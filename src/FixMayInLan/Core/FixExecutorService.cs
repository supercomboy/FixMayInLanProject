using FixMayInLan.Models;
using Microsoft.Win32;

namespace FixMayInLan.Core;

/// <summary>
/// Thực hiện tuần tự các fix đã được người dùng chọn.
/// </summary>
public sealed class FixExecutorService
{
    private const string PrintRegistryPath =
        @"SYSTEM\CurrentControlSet\Control\Print";

    private const string UserWindowsRegistryPath =
        @"Software\Microsoft\Windows NT\CurrentVersion\Windows";

    private readonly RegistryBackupService
        _backupService;
        private readonly IAppLogger _logger;

    private readonly SpoolerService
        _spoolerService;

    private readonly PrinterService
        _printerService;

    private string? _previousDefaultPrinter;

    private bool _restartSpoolerWhenUndo;

public FixExecutorService(
    RegistryBackupService backupService,
    SpoolerService spoolerService,
    PrinterService printerService,
    IAppLogger logger)
{
    _backupService = backupService;
    _spoolerService = spoolerService;
    _printerService = printerService;
    _logger = logger;
}

    public string? LastBackupFolder =>
        _backupService.CurrentSessionFolder;

    public async Task<IReadOnlyList<FixResult>>
        ExecuteAsync(
            PrinterInfo printer,
            IReadOnlyCollection<DiagnosticFinding>
                selectedFindings,
            IProgress<FixProgress>? progress = null,
            CancellationToken cancellationToken = default)
    {
        if (selectedFindings.Count == 0)
        {
            throw new ArgumentException(
                "Chưa chọn lỗi cần sửa.",
                nameof(selectedFindings));
        }

        if (selectedFindings.Any(
                finding => !finding.CanFixLocally))
        {
            throw new InvalidOperationException(
                "Danh sách chứa một mục không thể " +
                "xử lý tại máy hiện tại.");
        }

       string backupFolder =
    _backupService.StartSession();

_logger.Info(
    $"Đã tạo phiên backup: {backupFolder}");

        _previousDefaultPrinter =
            _printerService.GetDefaultPrinterName();

        _restartSpoolerWhenUndo = false;

        List<FixResult> results = new();

        try
        {
            int completed = 0;

            foreach (DiagnosticFinding finding
                     in selectedFindings)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                ReportProgress(
                    progress,
                    completed,
                    selectedFindings.Count,
                    $"Đang xử lý {finding.Code}...");

                switch (finding.Issue)
                {
                    case PrinterIssue.Error0000011B:
                        await ApplyError11BAsync(
                            printer,
                            cancellationToken);
                        break;

                    case PrinterIssue.Error00000709:
                        await ApplyError709Async(
                            printer,
                            cancellationToken);
                        break;

                    default:
                        throw new NotSupportedException(
                            $"Chưa hỗ trợ lỗi " +
                            $"{finding.Issue}.");
                }

                results.Add(
                    new FixResult
                    {
                        Issue = finding.Issue,
                        Succeeded = true,
                        Message =
                            $"Đã xử lý {finding.Code}."
                    });

                completed++;

                ReportProgress(
                    progress,
                    completed,
                    selectedFindings.Count,
                    $"Hoàn tất {finding.Code}.");
            }

            return results;
        }
        catch (Exception originalException)
        {
            // Nếu đã có snapshot, tự động rollback.
            if (_backupService.HasSnapshots)
            {
                try
                {
                    await UndoAsync(
                        CancellationToken.None);
                }
                catch (Exception rollbackException)
                {
                    throw new AggregateException(
                        "Sửa lỗi thất bại và quá trình " +
                        "hoàn tác cũng gặp lỗi.",
                        originalException,
                        rollbackException);
                }
            }

            throw;
        }
    }

    /// <summary>
    /// Hoàn tác phiên sửa gần nhất.
    /// </summary>
      public async Task UndoAsync(
        CancellationToken cancellationToken = default)
        
    {
        _logger.Warning(
    "Đang hoàn tác phiên thay đổi gần nhất.");
        _backupService.RestoreCurrentSession();

        if (!string.IsNullOrWhiteSpace(
                _previousDefaultPrinter) &&
            _printerService.PrinterExists(
                _previousDefaultPrinter))
        {
            await _printerService
                .SetDefaultPrinterAsync(
                    _previousDefaultPrinter,
                    cancellationToken);
        }

        if (_restartSpoolerWhenUndo)
        {
            await _spoolerService.RestartAsync(
                cancellationToken);
        }
        _logger.Success(
    "Đã hoàn tác phiên thay đổi.");
    }

    private async Task ApplyError11BAsync(
        PrinterInfo printer,
        CancellationToken cancellationToken)
    {
        if (printer.Role != PrinterRole.Host)
        {
            throw new InvalidOperationException(
                "0x0000011B chỉ được áp dụng tại " +
                "Printer Host đang chia sẻ máy in.");
        }
_logger.Warning(
    "Chuẩn bị áp dụng 0x0000011B trên Printer Host.");

_logger.Info(
    "Đang sao lưu RpcAuthnLevelPrivacyEnabled.");
        _backupService.BackupDwordValue(
            RegistryHive.LocalMachine,
            PrintRegistryPath,
            "RpcAuthnLevelPrivacyEnabled");

        using RegistryKey printKey =
            Registry.LocalMachine.CreateSubKey(
                PrintRegistryPath,
                writable: true)
            ?? throw new UnauthorizedAccessException(
                "Không thể mở Registry của " +
                "Print Spooler.");

        printKey.SetValue(
            "RpcAuthnLevelPrivacyEnabled",
            0,
            RegistryValueKind.DWord);
            _logger.Warning(
    "Đã đặt RpcAuthnLevelPrivacyEnabled=0. " +
    "Mức bảo vệ RPC đã giảm.");

        _restartSpoolerWhenUndo = true;

        await _spoolerService.RestartAsync(
            cancellationToken);
            _logger.Success(
    "Đã hoàn tất xử lý 0x0000011B.");
    }

    private async Task ApplyError709Async(
        PrinterInfo printer,
        CancellationToken cancellationToken)
    {
        if (!_printerService.PrinterExists(
                printer.Name))
        {
            throw new InvalidOperationException(
                $"Máy in '{printer.Name}' " +
                "không còn tồn tại.");
        }
        _logger.Info(
    $"Chuẩn bị đặt '{printer.Name}' " +
    "làm máy in mặc định.");

_logger.Info(
    "Đang sao lưu LegacyDefaultPrinterMode.");
        _backupService.BackupDwordValue(
            RegistryHive.CurrentUser,
            UserWindowsRegistryPath,
            "LegacyDefaultPrinterMode");

        using RegistryKey windowsKey =
            Registry.CurrentUser.CreateSubKey(
                UserWindowsRegistryPath,
                writable: true)
            ?? throw new UnauthorizedAccessException(
                "Không thể mở thiết lập máy in " +
                "của người dùng.");

        // Tắt cơ chế Windows tự thay đổi
        // máy in mặc định.
        windowsKey.SetValue(
            "LegacyDefaultPrinterMode",
            1,
            RegistryValueKind.DWord);

        await _printerService
            .SetDefaultPrinterAsync(
                printer.Name,
                cancellationToken);

        string? actualDefaultPrinter =
            _printerService.GetDefaultPrinterName();

        if (!string.Equals(
                actualDefaultPrinter,
                printer.Name,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Windows không giữ máy in vừa chọn " +
                "làm máy in mặc định.");
        }
        
    }

    private static void ReportProgress(
        IProgress<FixProgress>? progress,
        int completed,
        int total,
        string status)
    {
        int percent =
            total == 0
                ? 0
                : completed * 100 / total;

        progress?.Report(
            new FixProgress
            {
                Percent = percent,
                Status = status
            });
    }
}
using FixMayInLan.Models;
using Microsoft.Win32;

namespace FixMayInLan.Core;

/// <summary>
/// Chẩn đoán đúng hai nhóm lỗi:
/// 0x0000011B và 0x00000709.
/// </summary>
public sealed class DiagnosticsService
{
    private const string PrintRegistryPath =
        @"SYSTEM\CurrentControlSet\Control\Print";

    private const string RpcPrivacyValueName =
        "RpcAuthnLevelPrivacyEnabled";

    public Task<IReadOnlyList<DiagnosticFinding>>
        ScanAsync(
            PrinterInfo printer,
            CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<DiagnosticFinding>>(
            () =>
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                List<DiagnosticFinding> findings = new()
                {
                    DiagnoseError11B(printer),
                    DiagnoseError709(printer)
                };

                return findings;
            },
            cancellationToken);
    }

    private static DiagnosticFinding DiagnoseError11B(
        PrinterInfo printer)
    {
        // Nếu đây là máy Client, không được sửa
        // RpcAuthnLevelPrivacyEnabled tại máy này.
        if (printer.Role == PrinterRole.Client)
        {
            return new DiagnosticFinding
            {
                Issue =
                    PrinterIssue.Error0000011B,

                Code = "0x0000011B",

                Title =
                    "Không thể kết nối máy in chia sẻ",

                Description =
                    "Máy in được chọn đang hoạt động ở vai trò Client. " +
                    "Thiết lập RpcAuthnLevelPrivacyEnabled phải được " +
                    "kiểm tra trên máy đang chia sẻ máy in " +
                    "(Printer Host), không phải trên máy Client.",

                Severity =
                    FindingSeverity.Warning,

                CanFixLocally = false,
                IsRecommended = false,

                Preview =
                    "Không thay đổi Registry trên Client. " +
                    "Hãy chạy công cụ tại Printer Host."
            };
        }

        // Máy in trực tiếp không thuộc trường hợp
        // kết nối shared printer 0x0000011B.
        if (printer.Role != PrinterRole.Host)
        {
            return new DiagnosticFinding
            {
                Issue =
    PrinterIssue.Error0000011B,

Code = "0x0000011B",

Title = "Fix share LAN",

                Description =
                    "Máy in được chọn không phải máy in đang được " +
                    "chia sẻ từ máy tính hiện tại. Vì vậy không áp dụng " +
                    "thay đổi RPC dành cho Printer Host.",

                Severity =
                    FindingSeverity.Information,

                CanFixLocally = false,
                IsRecommended = false,

                Preview = "Không có thay đổi."
            };
        }

        // Chỉ kiểm tra Registry khi đúng vai trò Host.
        using RegistryKey? printKey =
            Registry.LocalMachine.OpenSubKey(
                PrintRegistryPath,
                writable: false);

        object? registryValue =
            printKey?.GetValue(
                RpcPrivacyValueName);

        bool compatibilityModeEnabled =
            registryValue is int value &&
            value == 0;

        if (compatibilityModeEnabled)
        {
            return new DiagnosticFinding
            {
                Issue =
                    PrinterIssue.Error0000011B,

                Code = "0x0000011B",

                Title =
                    "Chế độ tương thích RPC đã được bật",

                Description =
                    "RpcAuthnLevelPrivacyEnabled hiện đang bằng 0. " +
                    "Nếu Client vẫn báo 0x0000011B thì nguyên nhân " +
                    "có thể không nằm ở thiết lập RPC này.",

                Severity =
                    FindingSeverity.Information,

                CanFixLocally = false,
                IsRecommended = false,

                Preview =
                    "Không ghi lại Registry vì giá trị đã bằng 0."
            };
        }

        return new DiagnosticFinding
        {
            Issue =
                PrinterIssue.Error0000011B,

            Code = "0x0000011B",

            Title =
                "Có thể áp dụng chế độ tương thích RPC",

            Description =
                "Máy tính hiện tại đang đóng vai trò Printer Host. " +
                "Nếu máy Client hiển thị đúng lỗi 0x0000011B, " +
                "có thể sao lưu Registry rồi bật chế độ tương thích RPC.",

            Severity =
                FindingSeverity.Warning,

            // Có thể sửa nhưng không tự động tick
            // vì thay đổi này làm giảm bảo mật.
            CanFixLocally = true,
            IsRecommended = false,

            Preview =
                $@"Sao lưu rồi đặt:
HKLM\{PrintRegistryPath}\{RpcPrivacyValueName}
Kiểu: DWORD
Giá trị: 0

Sau đó khởi động lại Print Spooler.",

            SecurityWarning =
                "RpcAuthnLevelPrivacyEnabled=0 làm giảm yêu cầu " +
                "bảo mật RPC cho dịch vụ in. Chỉ dùng trên mạng LAN " +
                "tin cậy và nên hoàn tác khi không còn cần thiết."
        };
    }

    private static DiagnosticFinding DiagnoseError709(
        PrinterInfo printer)
    {
        if (printer.IsDefault)
        {
            return new DiagnosticFinding
            {
                Issue =
                    PrinterIssue.Error00000709,

                Code = "0x00000709",

                Title = "Fix lỗi mặc định",

                Description =
                    "Máy in được chọn hiện đã là máy in mặc định. " +
                    "Bạn vẫn có thể chạy lại thao tác nếu Windows " +
                    "tiếp tục báo lỗi 0x00000709.",

                Severity =
                    FindingSeverity.Information,

                CanFixLocally = true,
                IsRecommended = false,

                Preview =
                    "Sao lưu LegacyDefaultPrinterMode, " +
                    "gọi Windows SetDefaultPrinter và xác minh lại."
            };
        }

        return new DiagnosticFinding
        {
            Issue =
                PrinterIssue.Error00000709,

            Code = "0x00000709",

            Title =
                "Máy in chưa được đặt làm mặc định",

            Description =
                "Máy in được chọn chưa phải máy in mặc định. " +
                "Có thể tắt cơ chế Windows tự quản lý máy in mặc định " +
                "và đặt máy in này làm mặc định.",

            Severity =
                FindingSeverity.Warning,

            CanFixLocally = true,

            // Trường hợp này được tick sẵn.
            IsRecommended = true,

            Preview =
                @"Sao lưu:
HKCU\Software\Microsoft\Windows NT\CurrentVersion\Windows
Giá trị: LegacyDefaultPrinterMode

Sau đó đặt DWORD=1, gọi SetDefaultPrinter và xác minh lại."
        };
    }
}
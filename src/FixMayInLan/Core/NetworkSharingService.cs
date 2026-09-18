using Microsoft.Win32;

namespace FixMayInLan.Core;

public sealed class NetworkSharingService
{
    private const string LsaRegistryPath =
        @"SYSTEM\CurrentControlSet\Control\Lsa";

    private const string ServerRegistryPath =
        @"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters";

    private readonly PowerShellRunner
        _powerShellRunner;

    private readonly RegistryBackupService
        _backupService;

    private readonly ComputerNameService
        _computerNameService;

    private readonly IAppLogger _logger;

    public NetworkSharingService(
        PowerShellRunner powerShellRunner,
        RegistryBackupService backupService,
        ComputerNameService computerNameService,
        IAppLogger logger)
    {
        _powerShellRunner = powerShellRunner;
        _backupService = backupService;
        _computerNameService = computerNameService;
        _logger = logger;
    }

    public string? PasswordSharingBackupFolder =>
        _backupService.CurrentSessionFolder;

    public async Task ChangePublicToPrivateAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureNotDomainJoined();

        const string script = """
            $ErrorActionPreference = 'Stop'

            $profiles = Get-NetConnectionProfile |
                Where-Object {
                    $_.NetworkCategory -eq 'Public' -and
                    (
                        $_.IPv4Connectivity -ne 'Disconnected' -or
                        $_.IPv6Connectivity -ne 'Disconnected'
                    )
                }

            if (-not $profiles) {
                Write-Output 'NO_CHANGE'
                exit 0
            }

            $profiles |
                Set-NetConnectionProfile `
                    -NetworkCategory Private `
                    -ErrorAction Stop

            $names = $profiles |
                ForEach-Object { $_.InterfaceAlias }

            Write-Output ('CHANGED=' + ($names -join ', '))
            """;

        _logger.Info(
            "Đang kiểm tra network profile.");

        PowerShellResult result =
            await _powerShellRunner.RunAsync(
                script,
                cancellationToken);

        EnsurePowerShellSucceeded(
            result,
            "Không thể chuyển mạng sang Private.");

        if (result.Output.Contains(
                "NO_CHANGE",
                StringComparison.OrdinalIgnoreCase))
        {
            _logger.Info(
                "Không có profile Public đang hoạt động.");
        }
        else
        {
            _logger.Success(
                "Đã chuyển network profile sang Private: " +
                result.Output);
        }
    }

    public async Task<bool> HasActivePrivateNetworkAsync(
        CancellationToken cancellationToken = default)
    {
        const string script = """
            $ErrorActionPreference = 'Stop'

            $profiles = Get-NetConnectionProfile |
                Where-Object {
                    $_.IPv4Connectivity -ne 'Disconnected' -or
                    $_.IPv6Connectivity -ne 'Disconnected'
                }

            if (
                $profiles |
                Where-Object {
                    $_.NetworkCategory -eq 'Private'
                }
            ) {
                Write-Output 'PRIVATE'
            }
            else {
                Write-Output 'NOT_PRIVATE'
            }
            """;

        PowerShellResult result =
            await _powerShellRunner.RunAsync(
                script,
                cancellationToken);

        EnsurePowerShellSucceeded(
            result,
            "Không thể đọc network profile.");

        return string.Equals(
            result.Output.Trim(),
            "PRIVATE",
            StringComparison.OrdinalIgnoreCase);
    }

    public async Task EnableLanSharingAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureNotDomainJoined();

        bool hasPrivateNetwork =
            await HasActivePrivateNetworkAsync(
                cancellationToken);

        if (!hasPrivateNetwork)
        {
            throw new InvalidOperationException(
                "Không có mạng Private đang hoạt động. " +
                "Hãy chạy Public → Private trước.");
        }

        const string script = """
            $ErrorActionPreference = 'Stop'

            $services = @(
                @{ Name = 'FDResPub'; Startup = 'Automatic' },
                @{ Name = 'fdPHost'; Startup = 'Manual' },
                @{ Name = 'LanmanServer'; Startup = 'Automatic' }
            )

            foreach ($item in $services) {
                $service = Get-Service `
                    -Name $item.Name `
                    -ErrorAction SilentlyContinue

                if ($null -ne $service) {
                    Set-Service `
                        -Name $item.Name `
                        -StartupType $item.Startup `
                        -ErrorAction Stop

                    if ($service.Status -ne 'Running') {
                        Start-Service `
                            -Name $item.Name `
                            -ErrorAction Stop
                    }
                }
            }

            $rules = Get-NetFirewallRule |
                Where-Object {
                    $_.PolicyStoreSourceType -eq 'Local' -and
                    (
                        $_.Name -like 'NETDIS-*' -or
                        $_.Name -like 'FPS-*'
                    )
                }

            if (-not $rules) {
                throw 'Không tìm thấy firewall rule Network Discovery/File Sharing.'
            }

            $rules |
                Set-NetFirewallRule `
                    -Enabled True `
                    -Profile Private `
                    -ErrorAction Stop

            Write-Output (
                'ENABLED_RULES=' +
                @($rules).Count
            )
            """;

        _logger.Info(
            "Đang bật Network Discovery và File & Printer Sharing.");

        PowerShellResult result =
            await _powerShellRunner.RunAsync(
                script,
                cancellationToken);

        EnsurePowerShellSucceeded(
            result,
            "Không thể bật chia sẻ mạng LAN.");

        _logger.Success(
            "Đã bật chia sẻ mạng LAN cho profile Private. " +
            result.Output);
    }

    public async Task DisablePasswordProtectedSharingAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureNotDomainJoined();

        bool hasPrivateNetwork =
            await HasActivePrivateNetworkAsync(
                cancellationToken);

        if (!hasPrivateNetwork)
        {
            throw new InvalidOperationException(
                "Chức năng chỉ được phép chạy khi " +
                "có mạng Private đang hoạt động.");
        }

        string backupFolder =
            _backupService.StartSession();

        // Sao lưu tất cả trước khi thay đổi.
        _backupService.BackupDwordValue(
            RegistryHive.LocalMachine,
            LsaRegistryPath,
            "EveryoneIncludesAnonymous");

        _backupService.BackupDwordValue(
            RegistryHive.LocalMachine,
            LsaRegistryPath,
            "ForceGuest");

        _backupService.BackupDwordValue(
            RegistryHive.LocalMachine,
            ServerRegistryPath,
            "RestrictNullSessAccess");

        using RegistryKey lsaKey =
            Registry.LocalMachine.CreateSubKey(
                LsaRegistryPath,
                writable: true)
            ?? throw new UnauthorizedAccessException(
                "Không thể mở khóa Registry LSA.");

        using RegistryKey serverKey =
            Registry.LocalMachine.CreateSubKey(
                ServerRegistryPath,
                writable: true)
            ?? throw new UnauthorizedAccessException(
                "Không thể mở Registry LanmanServer.");

        lsaKey.SetValue(
            "EveryoneIncludesAnonymous",
            1,
            RegistryValueKind.DWord);

        lsaKey.SetValue(
            "ForceGuest",
            1,
            RegistryValueKind.DWord);

        serverKey.SetValue(
            "RestrictNullSessAccess",
            0,
            RegistryValueKind.DWord);

        _logger.Warning(
            "Password Protected Sharing: OFF. " +
            "Quyền truy cập ẩn danh/Guest đã được nới lỏng.");

        _logger.Info(
            $"Backup Registry: {backupFolder}");
    }

    public Task UndoPasswordProtectedSharingAsync()
    {
        return Task.Run(
            () =>
            {
                _backupService.RestoreCurrentSession();

                _logger.Success(
                    "Đã khôi phục thiết lập Password " +
                    "Protected Sharing từ backup.");
            });
    }

    private void EnsureNotDomainJoined()
    {
        if (_computerNameService.IsDomainJoined())
        {
            throw new InvalidOperationException(
                "Máy tính đang thuộc Domain. " +
                "Thiết lập này phải do quản trị viên " +
                "Domain hoặc Group Policy quản lý.");
        }
    }

    private static void EnsurePowerShellSucceeded(
        PowerShellResult result,
        string message)
    {
        if (result.Succeeded)
        {
            return;
        }

        string details =
            string.IsNullOrWhiteSpace(result.Error)
                ? result.Output
                : result.Error;

        throw new InvalidOperationException(
            message +
            Environment.NewLine +
            details);
    }
}
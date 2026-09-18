using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FixMayInLan.Models;

namespace FixMayInLan.Core;

public sealed partial class ClientPrinterService
{
    /// <summary>
    /// Đọc danh sách máy in được share từ máy chủ.
    /// </summary>
    public async Task<IReadOnlyList<SharedPrinterInfo>>
        GetSharedPrintersAsync(
            string serverInput,
            CancellationToken cancellationToken = default)
    {
        string serverName = NormalizeServerName(serverInput);
        string escapedServer = EscapePowerShellValue(serverName);

        string script = $$"""
            $ErrorActionPreference = 'Stop'
            [Console]::OutputEncoding = [System.Text.Encoding]::UTF8

            $printers = @(
                Get-Printer -ComputerName '{{escapedServer}}' -ErrorAction Stop |
                    Where-Object {
                        $_.Shared -eq $true -and
                        -not [string]::IsNullOrWhiteSpace($_.ShareName)
                    } |
                    Select-Object Name, ShareName, DriverName, PortName
            )

            if ($printers.Count -eq 0) {
                Write-Output '[]'
            }
            else {
                ConvertTo-Json -InputObject $printers -Compress -Depth 3
            }
            """;

        PowerShellResult result = await RunPowerShellAsync(
            script,
            cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                CreatePowerShellErrorMessage(
                    "Không thể đọc danh sách máy in từ máy chủ.",
                    result.Error));
        }

        return ParseSharedPrinters(
            result.Output,
            serverName);
    }

    /// <summary>
    /// Kết nối máy in share vào máy trạm.
    /// </summary>
    public async Task ConnectPrinterAsync(
        SharedPrinterInfo printer,
        bool setAsDefault,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(printer);

        string connectionName = printer.ConnectionName;

        if (string.IsNullOrWhiteSpace(connectionName))
        {
            throw new InvalidOperationException(
                "Đường dẫn kết nối máy in không hợp lệ.");
        }

        string escapedConnectionName =
            EscapePowerShellValue(connectionName);

        string script = $$"""
            $ErrorActionPreference = 'Stop'
            [Console]::OutputEncoding = [System.Text.Encoding]::UTF8

            $connectionName = '{{escapedConnectionName}}'

            $existingPrinter = Get-Printer -ErrorAction SilentlyContinue |
                Where-Object {
                    $_.Name -eq $connectionName
                } |
                Select-Object -First 1

            if ($null -eq $existingPrinter) {
                Add-Printer `
                    -ConnectionName $connectionName `
                    -ErrorAction Stop
            }

            Write-Output $connectionName
            """;

        PowerShellResult result = await RunPowerShellAsync(
            script,
            cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                CreatePowerShellErrorMessage(
                    $"Không thể kết nối máy in {connectionName}.",
                    result.Error));
        }

        if (setAsDefault)
        {
            await SetDefaultPrinterWithRetryAsync(
                connectionName,
                cancellationToken);
        }
    }

    /// <summary>
    /// Chuẩn hóa tên máy chủ do người dùng nhập.
    /// Chấp nhận: MAYCHU, \\MAYCHU, 192.168.1.10.
    /// </summary>
    public string NormalizeServerName(string serverInput)
    {
        if (string.IsNullOrWhiteSpace(serverInput))
        {
            throw new ArgumentException(
                "Vui lòng nhập tên hoặc địa chỉ IP của máy chủ.",
                nameof(serverInput));
        }

        string serverName = serverInput
            .Trim()
            .TrimStart('\\')
            .TrimEnd('\\');

        if (serverName.Length > 253)
        {
            throw new ArgumentException(
                "Tên máy chủ quá dài.",
                nameof(serverInput));
        }

        if (!ServerNamePattern().IsMatch(serverName))
        {
            throw new ArgumentException(
                "Tên hoặc địa chỉ IP máy chủ không hợp lệ.",
                nameof(serverInput));
        }

        return serverName;
    }

    private static IReadOnlyList<SharedPrinterInfo>
        ParseSharedPrinters(
            string json,
            string serverName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            using JsonDocument document =
                JsonDocument.Parse(json.Trim());

            List<SharedPrinterInfo> printers = [];

            if (document.RootElement.ValueKind ==
                JsonValueKind.Array)
            {
                foreach (JsonElement item in
                         document.RootElement.EnumerateArray())
                {
                    AddPrinterFromJson(
                        printers,
                        item,
                        serverName);
                }
            }
            else if (document.RootElement.ValueKind ==
                     JsonValueKind.Object)
            {
                AddPrinterFromJson(
                    printers,
                    document.RootElement,
                    serverName);
            }

            return printers
                .Where(printer =>
                    !string.IsNullOrWhiteSpace(
                        printer.ShareName))
                .OrderBy(printer => printer.ShareName)
                .ToList();
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "Dữ liệu máy in trả về từ máy chủ không hợp lệ.",
                exception);
        }
    }

    private static void AddPrinterFromJson(
        ICollection<SharedPrinterInfo> printers,
        JsonElement item,
        string serverName)
    {
        printers.Add(new SharedPrinterInfo
        {
            ServerName = serverName,
            Name = ReadJsonString(item, "Name"),
            ShareName = ReadJsonString(item, "ShareName"),
            DriverName = ReadJsonString(item, "DriverName"),
            PortName = ReadJsonString(item, "PortName")
        });
    }

    private static string ReadJsonString(
        JsonElement item,
        string propertyName)
    {
        if (!item.TryGetProperty(
                propertyName,
                out JsonElement property))
        {
            return string.Empty;
        }

        return property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : property.ToString();
    }

    private static async Task
        SetDefaultPrinterWithRetryAsync(
            string printerName,
            CancellationToken cancellationToken)
    {
        const int maximumAttempts = 10;

        for (int attempt = 1;
             attempt <= maximumAttempts;
             attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (SetDefaultPrinter(printerName))
            {
                return;
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(500),
                cancellationToken);
        }

        throw new InvalidOperationException(
            "Máy in đã được kết nối nhưng không thể đặt làm mặc định.");
    }

    private static async Task<PowerShellResult>
        RunPowerShellAsync(
            string script,
            CancellationToken cancellationToken)
    {
        string encodedCommand = Convert.ToBase64String(
            Encoding.Unicode.GetBytes(script));

        ProcessStartInfo startInfo = new()
        {
            FileName = "powershell.exe",
            Arguments =
                "-NoLogo -NoProfile -NonInteractive " +
                $"-EncodedCommand {encodedCommand}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using Process process = new()
        {
            StartInfo = startInfo
        };

        if (!process.Start())
        {
            throw new InvalidOperationException(
                "Không thể khởi chạy Windows PowerShell.");
        }

        Task<string> outputTask =
            process.StandardOutput.ReadToEndAsync(
                cancellationToken);

        Task<string> errorTask =
            process.StandardError.ReadToEndAsync(
                cancellationToken);

        try
        {
            await process.WaitForExitAsync(
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            TryStopProcess(process);
            throw;
        }

        string output = await outputTask;
        string error = await errorTask;

        return new PowerShellResult(
            process.ExitCode,
            output.Trim(),
            error.Trim());
    }

    private static void TryStopProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Không làm mất lỗi hủy thao tác ban đầu.
        }
    }

    private static string EscapePowerShellValue(string value)
    {
        // Chuỗi PowerShell đặt trong dấu nháy đơn.
        return value.Replace("'", "''");
    }

    private static string CreatePowerShellErrorMessage(
        string message,
        string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return message;
        }

        return $"{message}{Environment.NewLine}{error}";
    }

   [DllImport(
    "winspool.drv",
    EntryPoint = "SetDefaultPrinterW",
    CharSet = CharSet.Unicode,
    SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
private static extern bool SetDefaultPrinter(
    string printerName);

    [GeneratedRegex(
        @"^[A-Za-z0-9](?:[A-Za-z0-9.-]{0,251}[A-Za-z0-9])?$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ServerNamePattern();

    private sealed record PowerShellResult(
        int ExitCode,
        string Output,
        string Error);
}
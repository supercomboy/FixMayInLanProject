using System.ComponentModel;
using System.Management;
using System.Runtime.InteropServices;
using FixMayInLan.Models;

namespace FixMayInLan.Core;

/// <summary>
/// Đọc và thay đổi thông tin máy in của Windows.
/// </summary>
public sealed class PrinterService
{
    [DllImport(
        "winspool.drv",
        EntryPoint = "SetDefaultPrinterW",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool
        SetDefaultPrinterNative(
            string printerName);

    public Task<IReadOnlyList<PrinterInfo>>
        GetPrintersAsync(
            CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<PrinterInfo>>(
            () =>
            {
                List<PrinterInfo> printers = new();

                const string query = """
                    SELECT
                        Name,
                        PortName,
                        DriverName,
                        PrinterStatus,
                        Default,
                        Shared,
                        Network,
                        ShareName
                    FROM Win32_Printer
                    """;

                using ManagementObjectSearcher searcher =
                    new(query);

                using ManagementObjectCollection results =
                    searcher.Get();

                foreach (ManagementObject printer in results)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    PrinterInfo printerInfo = new()
                    {
                        Name = GetString(
                            printer,
                            "Name",
                            "Không rõ"),

                        PortName = GetString(
                            printer,
                            "PortName",
                            "—"),

                        DriverName = GetString(
                            printer,
                            "DriverName",
                            "—"),

                        Status = GetPrinterStatus(
                            printer["PrinterStatus"]),

                        IsDefault = GetBoolean(
                            printer,
                            "Default"),

                        IsShared = GetBoolean(
                            printer,
                            "Shared"),

                        IsNetwork = GetBoolean(
                            printer,
                            "Network"),

                        ShareName =
                            printer["ShareName"]?.ToString()
                    };

                    printers.Add(printerInfo);
                }

                return printers
                    .OrderByDescending(
                        printer => printer.IsDefault)
                    .ThenBy(
                        printer => printer.Name)
                    .ToList();
            },
            cancellationToken);
    }

    /// <summary>
    /// Đọc tên máy in mặc định hiện tại.
    /// </summary>
    public string? GetDefaultPrinterName()
    {
        const string query = """
            SELECT Name
            FROM Win32_Printer
            WHERE Default = TRUE
            """;

        using ManagementObjectSearcher searcher =
            new(query);

        using ManagementObjectCollection results =
            searcher.Get();

        foreach (ManagementObject printer in results)
        {
            return printer["Name"]?.ToString();
        }

        return null;
    }

    /// <summary>
    /// Kiểm tra máy in còn tồn tại trước khi sửa.
    /// </summary>
    public bool PrinterExists(
        string printerName)
    {
        const string query = """
            SELECT Name
            FROM Win32_Printer
            """;

        using ManagementObjectSearcher searcher =
            new(query);

        using ManagementObjectCollection results =
            searcher.Get();

        foreach (ManagementObject printer in results)
        {
            string? currentName =
                printer["Name"]?.ToString();

            if (string.Equals(
                    currentName,
                    printerName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Đặt máy in mặc định bằng Windows API.
    /// </summary>
    public Task SetDefaultPrinterAsync(
        string printerName,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () =>
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                if (!PrinterExists(printerName))
                {
                    throw new InvalidOperationException(
                        $"Không tìm thấy máy in " +
                        $"'{printerName}'.");
                }

                bool succeeded =
                    SetDefaultPrinterNative(
                        printerName);

                if (!succeeded)
                {
                    int errorCode =
                        Marshal.GetLastWin32Error();

                    throw new Win32Exception(
                        errorCode,
                        $"Không thể đặt '{printerName}' " +
                        "làm máy in mặc định.");
                }
            },
            cancellationToken);
    }

    private static string GetString(
        ManagementBaseObject managementObject,
        string propertyName,
        string fallback)
    {
        string? value =
            managementObject[propertyName]?.ToString();

        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value;
    }

    private static bool GetBoolean(
        ManagementBaseObject managementObject,
        string propertyName)
    {
        return managementObject[propertyName]
            is bool value && value;
    }

    private static string GetPrinterStatus(
        object? rawValue)
    {
        ushort status =
            rawValue is ushort value
                ? value
                : (ushort)0;

        return status switch
        {
            1 => "Khác",
            2 => "Không xác định",
            3 => "Sẵn sàng",
            4 => "Đang in",
            5 => "Đang khởi động",
            6 => "Đã dừng",
            7 => "Ngoại tuyến",
            _ => "Không xác định"
        };
    }
}
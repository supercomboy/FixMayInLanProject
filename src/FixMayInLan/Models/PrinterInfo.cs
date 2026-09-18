namespace FixMayInLan.Models;

/// <summary>
/// Vai trò của máy in đối với máy tính hiện tại.
/// </summary>
public enum PrinterRole
{
    Unknown,
    Host,
    Client,
    Direct
}

/// <summary>
/// Thông tin của một máy in được lấy từ Windows.
/// </summary>
public sealed class PrinterInfo
{
    public string Name { get; init; } = string.Empty;

    public string PortName { get; init; } = string.Empty;

    public string DriverName { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public bool IsDefault { get; init; }

    public bool IsShared { get; init; }

    public bool IsNetwork { get; init; }

    public string? ShareName { get; init; }

    /// <summary>
    /// Tự động xác định Host, Client hoặc máy in trực tiếp.
    /// </summary>
    public PrinterRole Role
    {
        get
        {
            // Máy in cục bộ đang được chia sẻ từ máy hiện tại.
            if (IsShared && !IsNetwork)
            {
                return PrinterRole.Host;
            }

            // Máy in được kết nối từ máy khác qua mạng.
            if (IsNetwork ||
                Name.StartsWith(
                    @"\\",
                    StringComparison.Ordinal))
            {
                return PrinterRole.Client;
            }

            // USB hoặc Standard TCP/IP được cài trực tiếp.
            return PrinterRole.Direct;
        }
    }

    /// <summary>
    /// Nội dung hiển thị trên giao diện.
    /// </summary>
    public string RoleDisplay
    {
        get
        {
            return Role switch
            {
                PrinterRole.Host => "Printer Host",
                PrinterRole.Client => "Client",
                PrinterRole.Direct => "Máy in trực tiếp",
                _ => "Chưa xác định"
            };
        }
    }

    public string DefaultDisplay =>
        IsDefault ? "Có" : string.Empty;

    public string SharedDisplay =>
        IsShared ? "Đang chia sẻ" : "Không";
}
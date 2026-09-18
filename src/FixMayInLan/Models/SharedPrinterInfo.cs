namespace FixMayInLan.Models;

public sealed class SharedPrinterInfo
{
    public string ServerName { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string ShareName { get; init; } = string.Empty;

    public string DriverName { get; init; } = string.Empty;

    public string PortName { get; init; } = string.Empty;

    /// <summary>
    /// Đường dẫn kết nối máy in, ví dụ:
    /// \\MAYCHU\Canon2900
    /// </summary>
    public string ConnectionName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ServerName) ||
                string.IsNullOrWhiteSpace(ShareName))
            {
                return string.Empty;
            }

            string server = ServerName.Trim().TrimStart('\\');

            return $@"\\{server}\{ShareName}";
        }
    }
}
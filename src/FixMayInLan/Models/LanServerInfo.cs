namespace FixMayInLan.Models;

public sealed class LanServerInfo
{
    public string HostName { get; init; } = string.Empty;

    public string IpAddress { get; init; } = string.Empty;

    public string DisplayName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(HostName) ||
                HostName.Equals(IpAddress, StringComparison.OrdinalIgnoreCase))
            {
                return IpAddress;
            }

            return $"{HostName}  •  {IpAddress}";
        }
    }

    /// <summary>
    /// Ưu tiên hostname; nếu không lấy được hostname thì dùng địa chỉ IP.
    /// </summary>
    public string ConnectionTarget =>
        string.IsNullOrWhiteSpace(HostName)
            ? IpAddress
            : HostName;
}
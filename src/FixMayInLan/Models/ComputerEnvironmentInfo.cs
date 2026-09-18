namespace FixMayInLan.Models;

/// <summary>
/// Thông tin môi trường Windows hiện tại.
/// </summary>
public sealed class ComputerEnvironmentInfo
{
    public string ComputerName { get; init; } =
        string.Empty;

    public string IPv4Address { get; init; } =
        string.Empty;

    public string WindowsName { get; init; } =
        string.Empty;

    public string WindowsVersion { get; init; } =
        string.Empty;

    public bool IsWindowsServer { get; init; }

    public bool IsDomainJoined { get; init; }

    public string? DomainName { get; init; }

    public string DomainDisplay
    {
        get
        {
            if (IsDomainJoined)
            {
                return string.IsNullOrWhiteSpace(
                    DomainName)
                    ? "Domain-joined"
                    : $"Domain: {DomainName}";
            }

            return "Workgroup";
        }
    }

   public string EnvironmentDisplay
{
    get
    {
        return
            $"{ComputerName}  •  " +
            $"{IPv4Address}  •  " +
            $"{WindowsName}";
    }
}
}
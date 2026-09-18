using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using FixMayInLan.Models;

namespace FixMayInLan.Core;

/// <summary>
/// Đọc thông tin hệ điều hành, domain và mạng.
/// </summary>
public sealed class SystemInfoService
{
    public Task<ComputerEnvironmentInfo>
        GetEnvironmentInfoAsync(
            CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () =>
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                OperatingSystemData operatingSystem =
                    ReadOperatingSystem();

                ComputerSystemData computerSystem =
                    ReadComputerSystem();

                string ipv4Address =
                    GetPrimaryIPv4Address();

                return new ComputerEnvironmentInfo
                {
                    ComputerName =
                        Environment.MachineName,

                    IPv4Address =
                        ipv4Address,

                    WindowsName =
                        operatingSystem.Caption,

                    WindowsVersion =
                        operatingSystem.Version,

                    IsWindowsServer =
                        operatingSystem.ProductType != 1,

                    IsDomainJoined =
                        computerSystem.PartOfDomain,

                    DomainName =
                        computerSystem.DomainName
                };
            },
            cancellationToken);
    }

    private static OperatingSystemData
        ReadOperatingSystem()
    {
        const string query = """
            SELECT
                Caption,
                Version,
                ProductType
            FROM Win32_OperatingSystem
            """;

        using ManagementObjectSearcher searcher =
            new(query);

        using ManagementObjectCollection results =
            searcher.Get();

        foreach (ManagementObject item in results)
        {
            string caption =
                item["Caption"]?.ToString()
                ?? "Windows";

            string version =
                item["Version"]?.ToString()
                ?? Environment.OSVersion
                    .Version.ToString();

            uint productType = 1;

            if (item["ProductType"] is not null)
            {
                productType =
                    Convert.ToUInt32(
                        item["ProductType"]);
            }

            return new OperatingSystemData(
                caption,
                version,
                productType);
        }

        return new OperatingSystemData(
            "Windows",
            Environment.OSVersion.Version.ToString(),
            1);
    }

    private static ComputerSystemData
        ReadComputerSystem()
    {
        const string query = """
            SELECT
                PartOfDomain,
                Domain
            FROM Win32_ComputerSystem
            """;

        using ManagementObjectSearcher searcher =
            new(query);

        using ManagementObjectCollection results =
            searcher.Get();

        foreach (ManagementObject item in results)
        {
            bool partOfDomain =
                item["PartOfDomain"]
                    is bool value &&
                value;

            string? domainName =
                item["Domain"]?.ToString();

            return new ComputerSystemData(
                partOfDomain,
                domainName);
        }

        return new ComputerSystemData(
            false,
            null);
    }

    private static string GetPrimaryIPv4Address()
    {
        try
        {
            IEnumerable<NetworkInterface>
                activeInterfaces =
                    NetworkInterface
                        .GetAllNetworkInterfaces()
                        .Where(networkInterface =>
                            networkInterface
                                .OperationalStatus ==
                            OperationalStatus.Up)
                        .Where(networkInterface =>
                            networkInterface
                                .NetworkInterfaceType !=
                            NetworkInterfaceType.Loopback)
                        .OrderByDescending(
                            networkInterface =>
                                networkInterface
                                    .GetIPProperties()
                                    .GatewayAddresses
                                    .Any());

            foreach (NetworkInterface networkInterface
                     in activeInterfaces)
            {
                UnicastIPAddressInformation?
                    addressInformation =
                        networkInterface
                            .GetIPProperties()
                            .UnicastAddresses
                            .FirstOrDefault(address =>
                                address.Address
                                    .AddressFamily ==
                                AddressFamily
                                    .InterNetwork &&
                                !IPAddress.IsLoopback(
                                    address.Address));

                if (addressInformation is null)
                {
                    continue;
                }

                string address =
                    addressInformation
                        .Address
                        .ToString();

                // Bỏ qua địa chỉ tự gán APIPA.
                if (address.StartsWith(
                        "169.254.",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                return address;
            }
        }
        catch
        {
            // Trả về thông báo bên dưới nếu
            // không thể đọc card mạng.
        }

        return "Không có IPv4";
    }

    private sealed record OperatingSystemData(
        string Caption,
        string Version,
        uint ProductType);

    private sealed record ComputerSystemData(
        bool PartOfDomain,
        string? DomainName);
}
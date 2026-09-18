using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using FixMayInLan.Models;

namespace FixMayInLan.Core;

public sealed class LanServerScannerService
{
    private const int SmbPort = 445;
    private const int MaximumConcurrentScans = 32;
    private const int ConnectionTimeoutMilliseconds = 500;

    /// <summary>
    /// Quét các máy đang mở cổng SMB 445 trong cùng dải mạng /24.
    /// Progress trả về giá trị từ 0 đến 100.
    /// </summary>
    public async Task<IReadOnlyList<LanServerInfo>> ScanAsync(
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        IPAddress localAddress = GetLocalIPv4Address()
            ?? throw new InvalidOperationException(
                "Không tìm thấy kết nối mạng IPv4 đang hoạt động.");

        byte[] localBytes = localAddress.GetAddressBytes();

        ConcurrentBag<LanServerInfo> servers = new();
        using SemaphoreSlim semaphore =
            new(MaximumConcurrentScans, MaximumConcurrentScans);

        int completedCount = 0;
        const int totalAddressCount = 254;

        IEnumerable<Task> scanTasks = Enumerable
            .Range(1, totalAddressCount)
            .Select(async lastOctet =>
            {
                await semaphore.WaitAsync(cancellationToken);

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    IPAddress targetAddress = new(
                    [
                        localBytes[0],
                        localBytes[1],
                        localBytes[2],
                        (byte)lastOctet
                    ]);

                    // Không quét chính máy đang chạy ứng dụng.
                    if (targetAddress.Equals(localAddress))
                    {
                        return;
                    }

                    bool smbAvailable = await IsPortOpenAsync(
                        targetAddress,
                        SmbPort,
                        ConnectionTimeoutMilliseconds,
                        cancellationToken);

                    if (!smbAvailable)
                    {
                        return;
                    }

                    string hostName = await ResolveHostNameAsync(
                        targetAddress,
                        cancellationToken);

                    servers.Add(new LanServerInfo
                    {
                        HostName = hostName,
                        IpAddress = targetAddress.ToString()
                    });
                }
                finally
                {
                    int completed = Interlocked.Increment(ref completedCount);
                    int percentage = completed * 100 / totalAddressCount;

                    progress?.Report(percentage);

                    semaphore.Release();
                }
            });

        await Task.WhenAll(scanTasks);

        return servers
            .OrderBy(server => GetSortableAddress(server.IpAddress))
            .ToList();
    }

    /// <summary>
    /// Trả về IPv4 của card mạng đang hoạt động và có Default Gateway.
    /// </summary>
    public IPAddress? GetLocalIPv4Address()
    {
        IEnumerable<NetworkInterface> networkInterfaces =
            NetworkInterface.GetAllNetworkInterfaces()
                .Where(networkInterface =>
                    networkInterface.OperationalStatus ==
                        OperationalStatus.Up &&
                    networkInterface.NetworkInterfaceType !=
                        NetworkInterfaceType.Loopback &&
                    networkInterface.NetworkInterfaceType !=
                        NetworkInterfaceType.Tunnel);

        foreach (NetworkInterface networkInterface in networkInterfaces)
        {
            IPInterfaceProperties properties =
                networkInterface.GetIPProperties();

            bool hasIPv4Gateway = properties.GatewayAddresses.Any(
                gateway =>
                    gateway.Address.AddressFamily ==
                        AddressFamily.InterNetwork &&
                    !gateway.Address.Equals(IPAddress.Any) &&
                    !gateway.Address.Equals(IPAddress.None));

            if (!hasIPv4Gateway)
            {
                continue;
            }

            IPAddress? address = properties.UnicastAddresses
                .Where(item =>
                    item.Address.AddressFamily ==
                        AddressFamily.InterNetwork)
                .Select(item => item.Address)
                .FirstOrDefault(IsPrivateIPv4Address);

            if (address is not null)
            {
                return address;
            }
        }

        return null;
    }

    private static async Task<bool> IsPortOpenAsync(
        IPAddress address,
        int port,
        int timeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        using TcpClient client = new(AddressFamily.InterNetwork);

        using CancellationTokenSource timeoutSource =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

        timeoutSource.CancelAfter(timeoutMilliseconds);

        try
        {
            await client.ConnectAsync(
                address,
                port,
                timeoutSource.Token);

            return client.Connected;
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            // Hết thời gian chờ kết nối.
            return false;
        }
        catch (SocketException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<string> ResolveHostNameAsync(
        IPAddress address,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            IPHostEntry hostEntry =
                await Dns.GetHostEntryAsync(address);

            string hostName = hostEntry.HostName.Trim();

            if (string.IsNullOrWhiteSpace(hostName))
            {
                return address.ToString();
            }

            // Nếu DNS trả về FQDN, chỉ hiển thị tên máy ở phía trước.
            int dotIndex = hostName.IndexOf('.');

            if (dotIndex > 0)
            {
                hostName = hostName[..dotIndex];
            }

            return hostName;
        }
        catch
        {
            return address.ToString();
        }
    }

    private static bool IsPrivateIPv4Address(IPAddress address)
    {
        byte[] bytes = address.GetAddressBytes();

        // 10.0.0.0/8
        if (bytes[0] == 10)
        {
            return true;
        }

        // 172.16.0.0/12
        if (bytes[0] == 172 &&
            bytes[1] >= 16 &&
            bytes[1] <= 31)
        {
            return true;
        }

        // 192.168.0.0/16
        return bytes[0] == 192 &&
               bytes[1] == 168;
    }

    private static uint GetSortableAddress(string address)
    {
        if (!IPAddress.TryParse(address, out IPAddress? parsedAddress))
        {
            return uint.MaxValue;
        }

        byte[] bytes = parsedAddress.GetAddressBytes();

        if (bytes.Length != 4)
        {
            return uint.MaxValue;
        }

        return ((uint)bytes[0] << 24) |
               ((uint)bytes[1] << 16) |
               ((uint)bytes[2] << 8) |
               bytes[3];
    }
}
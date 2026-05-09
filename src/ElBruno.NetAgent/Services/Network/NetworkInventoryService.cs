using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;

namespace ElBruno.NetAgent.Services.Network;

/// <summary>
/// Default implementation of INetworkInventoryService.
/// Enumerates System.Net.NetworkInformation.NetworkInterface and classifies each adapter.
/// </summary>
public class NetworkInventoryService : INetworkInventoryService
{
    private readonly ILogger<NetworkInventoryService> _logger;

    public NetworkInventoryService(ILogger<NetworkInventoryService> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<Core.Models.NetworkInterfaceInfo>> GetInterfacesAsync(CancellationToken cancellationToken = default)
    {
        var interfaces = NetworkInterface.GetAllNetworkInterfaces();
        var result = new List<Core.Models.NetworkInterfaceInfo>();

        foreach (var ni in interfaces)
        {
            var kind = ClassifyAdapter(ni);
            var info = new Core.Models.NetworkInterfaceInfo
            {
                Id = ni.Id ?? string.Empty,
                Name = ni.Name,
                Description = ni.Description,
                AdapterKind = kind,
                OperationalState = MapOperationalState(ni.OperationalStatus),
                MacAddress = GetMacAddress(ni),
                SpeedBytesPerSecond = ni.Speed
            };

            result.Add(info);

            _logger.LogDebug(
                "Detected interface: {Name} (Kind={Kind}, State={State})",
                info.Name, kind, info.OperationalState);
        }

        _logger.LogInformation(
            "Network inventory complete: {Count} interfaces detected",
            result.Count);

        return Task.FromResult<IReadOnlyList<Core.Models.NetworkInterfaceInfo>>(result);
    }

    private static Core.Enums.NetworkAdapterKind ClassifyAdapter(NetworkInterface networkInterface)
    {
        var description = networkInterface.Description?.ToLowerInvariant() ?? string.Empty;
        var name = networkInterface.Name?.ToLowerInvariant() ?? string.Empty;
        var type = networkInterface.NetworkInterfaceType;

        // Loopback
        if (type == NetworkInterfaceType.Loopback)
        {
            return Core.Enums.NetworkAdapterKind.Loopback;
        }

        // WiFi
        if (type == NetworkInterfaceType.Wireless80211)
        {
            return Core.Enums.NetworkAdapterKind.WiFi;
        }

        // Bluetooth (detected via description heuristic since the enum value may not be available)
        if (description.Contains("bluetooth", StringComparison.Ordinal))
        {
            return Core.Enums.NetworkAdapterKind.Bluetooth;
        }

        // Cellular
        if (type == NetworkInterfaceType.Ppp)
        {
            return Core.Enums.NetworkAdapterKind.Cellular;
        }

        // VPN
        if (type == NetworkInterfaceType.Tunnel ||
            description.Contains("vpn", StringComparison.Ordinal))
        {
            return Core.Enums.NetworkAdapterKind.Vpn;
        }

        // Virtual adapters (by description heuristics)
        if (IsVirtualAdapter(description, name))
        {
            return Core.Enums.NetworkAdapterKind.Virtual;
        }

        // USB tethering heuristic: Ethernet with Remote NDIS keywords
        if (type == NetworkInterfaceType.Ethernet &&
            (description.Contains("remote ndis", StringComparison.Ordinal) ||
             description.Contains("usb tether", StringComparison.OrdinalIgnoreCase)))
        {
            return Core.Enums.NetworkAdapterKind.UsbTethering;
        }

        // Ethernet
        if (type == NetworkInterfaceType.Ethernet ||
            type == NetworkInterfaceType.FastEthernetFx ||
            type == NetworkInterfaceType.GigabitEthernet)
        {
            return Core.Enums.NetworkAdapterKind.Ethernet;
        }

        return Core.Enums.NetworkAdapterKind.Unknown;
    }

    private static bool IsVirtualAdapter(string description, string name)
    {
        var virtualKeywords = new[] { "virtualbox", "hyper-v", "vmware", "vpci", "wsl" };
        return virtualKeywords.Any(kw => description.Contains(kw, StringComparison.Ordinal) || name.Contains(kw, StringComparison.Ordinal));
    }

    private static Core.Models.NetworkOperationalState MapOperationalState(OperationalStatus operationalStatus)
    {
        return operationalStatus switch
        {
            OperationalStatus.Up => Core.Models.NetworkOperationalState.Up,
            OperationalStatus.Down => Core.Models.NetworkOperationalState.Down,
            OperationalStatus.Testing => Core.Models.NetworkOperationalState.Testing,
            _ => Core.Models.NetworkOperationalState.Unknown
        };
    }

    private static string? GetMacAddress(NetworkInterface networkInterface)
    {
        var mac = networkInterface.GetPhysicalAddress();
        return mac?.ToString() ?? null;
    }
}

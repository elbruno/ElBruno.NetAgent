using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using ElBruno.NetAgent.Core.Models;
using ElBruno.NetAgent.Core.Enums;

namespace ElBruno.NetAgent.Core.Networking
{
    // Compatibility shim preserving the original test-facing API.
    // Delegates to the authoritative implementation when possible.
    public class NetworkInventoryService
    {
        private readonly IEnumerable<LightweightNetworkInterface>? _injected;

        public NetworkInventoryService()
        {
        }

        // Constructor used for tests to inject a deterministic set of interfaces
        public NetworkInventoryService(IEnumerable<LightweightNetworkInterface> injected)
        {
            _injected = injected;
        }

        public async Task<IReadOnlyList<NetworkInterfaceInfo>> GetInterfacesAsync(CancellationToken cancellationToken)
        {
            if (_injected != null)
            {
                var list = _injected.Select(s => new NetworkInterfaceInfo
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    Kind = Classify(s)
                }).ToList();

                return (IReadOnlyList<NetworkInterfaceInfo>)list;
            }

            // Delegate to authoritative implementation in Services.Network and map types
            var impl = new ElBruno.NetAgent.Services.Network.NetworkInventoryService(new NullLogger<ElBruno.NetAgent.Services.Network.NetworkInventoryService>());
            var coreList = await impl.GetInterfacesAsync(cancellationToken).ConfigureAwait(false);

            var mapped = coreList.Select(m => new NetworkInterfaceInfo
            {
                Id = m.Id,
                Name = m.Name,
                Description = m.Description,
                Kind = (NetworkAdapterKind)m.Kind
            }).ToList();

            return (IReadOnlyList<NetworkInterfaceInfo>)mapped;
        }

        public static NetworkAdapterKind Classify(LightweightNetworkInterface adapter)
        {
            if (adapter == null) return NetworkAdapterKind.Unknown;

            var desc = (adapter.Description ?? string.Empty).ToLowerInvariant();
            var name = (adapter.Name ?? string.Empty).ToLowerInvariant();

            // Loopback by type or typical name
            if (adapter.InterfaceType == NetworkInterfaceType.Loopback || name.Contains("loopback") || desc.Contains("loopback"))
                return NetworkAdapterKind.Loopback;

            // WiFi by type or description
            if (adapter.InterfaceType == NetworkInterfaceType.Wireless80211 || desc.Contains("wi-fi") || desc.Contains("wifi") || desc.Contains("wireless"))
                return NetworkAdapterKind.WiFi;

            // USB tethering heuristics
            if (desc.Contains("usb") || desc.Contains("rndis") || desc.Contains("tether"))
                return NetworkAdapterKind.UsbTethering;

            // Virtual adapters
            if (desc.Contains("virtual") || desc.Contains("hyper-v") || desc.Contains("vmware") || desc.Contains("virtualbox") || desc.Contains("tap") || desc.Contains("vpn"))
                return NetworkAdapterKind.Virtual;

            // Ethernet by type
            if (adapter.InterfaceType == NetworkInterfaceType.Ethernet || adapter.InterfaceType == NetworkInterfaceType.GigabitEthernet || adapter.InterfaceType == NetworkInterfaceType.FastEthernetFx || adapter.InterfaceType == NetworkInterfaceType.FastEthernetT)
                return NetworkAdapterKind.Ethernet;

            return NetworkAdapterKind.Unknown;
        }
    }
}

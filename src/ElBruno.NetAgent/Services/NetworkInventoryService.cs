using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace ElBruno.NetAgent.Core.Networking
{
    public interface INetworkInventoryService
    {
        Task<IReadOnlyList<NetworkInterfaceInfo>> GetInterfacesAsync(CancellationToken cancellationToken);
    }

    public class NetworkInventoryService : INetworkInventoryService
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

        public Task<IReadOnlyList<NetworkInterfaceInfo>> GetInterfacesAsync(CancellationToken cancellationToken)
        {
            IEnumerable<LightweightNetworkInterface> source;
            if (_injected != null)
            {
                source = _injected;
            }
            else
            {
                // Fallback to real system adapters when not injected. Keep simple and safe.
                source = NetworkInterface.GetAllNetworkInterfaces().Select(n => new LightweightNetworkInterface
                {
                    Id = n.Id,
                    Name = n.Name,
                    Description = n.Description ?? string.Empty,
                    InterfaceType = n.NetworkInterfaceType
                });
            }

            var list = source.Select(s => new NetworkInterfaceInfo
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Kind = Classify(s)
            }).ToList();

            return Task.FromResult((IReadOnlyList<NetworkInterfaceInfo>)list);
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
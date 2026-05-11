using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ElBruno.NetAgent.Core.Models;
using ElBruno.NetAgent.Core.Enums;

namespace ElBruno.NetAgent.Services.Network
{
    /// <summary>
    /// Implementation of INetworkInventoryService using System.Net.NetworkInformation APIs.
    /// </summary>
    public class NetworkInventoryService : ElBruno.NetAgent.Core.Services.INetworkInventoryService
    {
        private readonly ILogger<NetworkInventoryService> _logger;

        public NetworkInventoryService(ILogger<NetworkInventoryService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<IReadOnlyList<NetworkInterfaceInfo>> GetInterfacesAsync(CancellationToken cancellationToken)
        {
            var result = new List<NetworkInterfaceInfo>();

            var nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var ni in nics)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var ipProps = ni.GetIPProperties();
                var gateways = ipProps?.GatewayAddresses?
                    .Select(g => g?.Address?.ToString() ?? string.Empty)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList() ?? new List<string>();

                var isUp = ni.OperationalStatus == OperationalStatus.Up;
                var opState = ni.OperationalStatus == OperationalStatus.Up ? NetworkOperationalState.Up
                    : ni.OperationalStatus == OperationalStatus.Down ? NetworkOperationalState.Down : NetworkOperationalState.Unknown;

                var kind = ClassifyAdapter(ni, gateways, isUp);

                var info = new NetworkInterfaceInfo
                {
                    Id = ni.Id,
                    Name = ni.Name,
                    Description = ni.Description,
                    Kind = kind,
                    OperationalStatus = opState,
                    IsUp = isUp,
                    Speed = ni.Speed,
                    GatewayAddresses = gateways
                };

                result.Add(info);
            }

            _logger.LogInformation("Detected {Count} network interfaces.", result.Count);
            foreach (var i in result)
            {
                _logger.LogInformation("Interface: {Name} Id:{Id} Kind:{Kind} Status:{Status} Up:{IsUp} Speed:{Speed} Gateways:{Gateways}",
                    i.Name, i.Id, i.Kind, i.OperationalStatus, i.IsUp, i.Speed, string.Join(", ", i.GatewayAddresses));
            }

            return Task.FromResult((IReadOnlyList<NetworkInterfaceInfo>)result);
        }

        private NetworkAdapterKind ClassifyAdapter(NetworkInterface ni, List<string> gateways, bool isUp)
        {
            var desc = (ni.Description ?? string.Empty).ToLowerInvariant();
            var name = (ni.Name ?? string.Empty).ToLowerInvariant();
            var type = ni.NetworkInterfaceType;

            if (type == NetworkInterfaceType.Wireless80211)
                return NetworkAdapterKind.WiFi;

            if (type == NetworkInterfaceType.Loopback)
                return NetworkAdapterKind.Loopback;

            // VPN / Tunnel heuristics
            if (type == NetworkInterfaceType.Tunnel || desc.Contains("vpn") || name.Contains("vpn") || desc.Contains("tap") || desc.Contains("tun"))
                return NetworkAdapterKind.Vpn;

            // Bluetooth heuristic
            if (desc.Contains("bluetooth") || name.Contains("bluetooth"))
                return NetworkAdapterKind.Bluetooth;

            // Cellular heuristic
            if (desc.Contains("mobile") || desc.Contains("cellular") || desc.Contains("wwan"))
                return NetworkAdapterKind.Cellular;

            // Virtual adapters
            if (desc.Contains("virtual") || desc.Contains("hyper-v") || desc.Contains("vmware") || desc.Contains("virtualbox") || name.Contains("vethernet") || desc.Contains("vethernet"))
                return NetworkAdapterKind.Virtual;

            // Ethernet types
            if (type == NetworkInterfaceType.Ethernet || type == NetworkInterfaceType.Ethernet3Megabit ||
                type == NetworkInterfaceType.FastEthernetFx || type == NetworkInterfaceType.FastEthernetT ||
                type == NetworkInterfaceType.GigabitEthernet)
            {
                // USB tethering heuristic - description contains USB/RNDIS/android etc, interface up and has gateway
                var usbKeywords = new[] { "remote ndis", "rndis", "usb", "android", "pixel", "mobile" };
                if (isUp && gateways.Any() && usbKeywords.Any(k => desc.Contains(k) || name.Contains(k)))
                {
                    return NetworkAdapterKind.UsbTethering;
                }

                return NetworkAdapterKind.Ethernet;
            }

            return NetworkAdapterKind.Unknown;
        }
    }
}

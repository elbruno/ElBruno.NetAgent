using System.Net.NetworkInformation;

namespace ElBruno.NetAgent.Core.Networking
{
    public enum NetworkAdapterKind
    {
        Unknown,
        WiFi,
        Ethernet,
        UsbTethering,
        Virtual,
        Loopback
    }

    public class NetworkInterfaceInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public NetworkAdapterKind Kind { get; set; } = NetworkAdapterKind.Unknown;
    }

    /// <summary>
    /// Lightweight representation of an adapter used for injection and testing.
    /// </summary>
    public class LightweightNetworkInterface
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public NetworkInterfaceType InterfaceType { get; set; } = NetworkInterfaceType.Ethernet;
    }
}
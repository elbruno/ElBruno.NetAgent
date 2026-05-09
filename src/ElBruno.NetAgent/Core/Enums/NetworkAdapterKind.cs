using System;

namespace ElBruno.NetAgent.Core.Enums
{
    /// <summary>
    /// Enumeration of network adapter kinds used by the inventory service.
    /// </summary>
    public enum NetworkAdapterKind
    {
        Unknown,
        WiFi,
        Ethernet,
        UsbTethering,
        Virtual,
        Loopback,
        Bluetooth,
        Cellular,
        Vpn
    }
}

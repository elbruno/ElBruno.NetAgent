namespace ElBruno.NetAgent.Core.Enums;

/// <summary>
/// Classifies a network adapter by its type.
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

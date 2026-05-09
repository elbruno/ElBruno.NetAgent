using ElBruno.NetAgent.Core.Enums;
using ElBruno.NetAgent.Core.Models;

namespace ElBruno.NetAgent.Tests;

public class NetworkInterfaceInfoTests
{
    [Fact]
    public void IsCandidate_ReturnsFalse_WhenOperationalStateIsDown()
    {
        var info = new NetworkInterfaceInfo
        {
            Id = "test-1",
            Name = "Test Adapter",
            Description = "Test",
            AdapterKind = NetworkAdapterKind.Ethernet,
            OperationalState = NetworkOperationalState.Down
        };

        Assert.False(info.IsCandidate);
    }

    [Fact]
    public void IsCandidate_ReturnsFalse_WhenOperationalStateIsTesting()
    {
        var info = new NetworkInterfaceInfo
        {
            Id = "test-2",
            Name = "Test Adapter",
            Description = "Test",
            AdapterKind = NetworkAdapterKind.Ethernet,
            OperationalState = NetworkOperationalState.Testing
        };

        Assert.False(info.IsCandidate);
    }

    [Fact]
    public void IsCandidate_ReturnsFalse_WhenAdapterKindIsLoopback()
    {
        var info = new NetworkInterfaceInfo
        {
            Id = "test-3",
            Name = "Loopback",
            Description = "Loopback",
            AdapterKind = NetworkAdapterKind.Loopback,
            OperationalState = NetworkOperationalState.Up
        };

        Assert.False(info.IsCandidate);
    }

    [Fact]
    public void IsCandidate_ReturnsFalse_WhenAdapterKindIsVirtual()
    {
        var info = new NetworkInterfaceInfo
        {
            Id = "test-4",
            Name = "Virtual Adapter",
            Description = "Virtual",
            AdapterKind = NetworkAdapterKind.Virtual,
            OperationalState = NetworkOperationalState.Up
        };

        Assert.False(info.IsCandidate);
    }

    [Fact]
    public void IsCandidate_ReturnsTrue_WhenUpAndValidKind()
    {
        var info = new NetworkInterfaceInfo
        {
            Id = "test-5",
            Name = "Ethernet Adapter",
            Description = "Ethernet",
            AdapterKind = NetworkAdapterKind.Ethernet,
            OperationalState = NetworkOperationalState.Up
        };

        Assert.True(info.IsCandidate);
    }

    [Fact]
    public void IsCandidate_ReturnsTrue_WhenWiFiUp()
    {
        var info = new NetworkInterfaceInfo
        {
            Id = "test-6",
            Name = "Wi-Fi",
            Description = "Wi-Fi",
            AdapterKind = NetworkAdapterKind.WiFi,
            OperationalState = NetworkOperationalState.Up
        };

        Assert.True(info.IsCandidate);
    }

    [Fact]
    public void IsCandidate_ReturnsTrue_WhenUsbTetheringUp()
    {
        var info = new NetworkInterfaceInfo
        {
            Id = "test-7",
            Name = "USB Tethering",
            Description = "USB Tethering",
            AdapterKind = NetworkAdapterKind.UsbTethering,
            OperationalState = NetworkOperationalState.Up
        };

        Assert.True(info.IsCandidate);
    }

    [Fact]
    public void IsCandidate_ReturnsFalse_WhenBluetoothDown()
    {
        var info = new NetworkInterfaceInfo
        {
            Id = "test-8",
            Name = "Bluetooth",
            Description = "Bluetooth",
            AdapterKind = NetworkAdapterKind.Bluetooth,
            OperationalState = NetworkOperationalState.Down
        };

        Assert.False(info.IsCandidate);
    }

    [Fact]
    public void IsCandidate_ReturnsTrue_WhenCellularUp()
    {
        var info = new NetworkInterfaceInfo
        {
            Id = "test-9",
            Name = "Cellular",
            Description = "Cellular",
            AdapterKind = NetworkAdapterKind.Cellular,
            OperationalState = NetworkOperationalState.Up
        };

        Assert.True(info.IsCandidate);
    }

    [Fact]
    public void IsCandidate_ReturnsTrue_WhenVpnUp()
    {
        var info = new NetworkInterfaceInfo
        {
            Id = "test-10",
            Name = "VPN",
            Description = "VPN",
            AdapterKind = NetworkAdapterKind.Vpn,
            OperationalState = NetworkOperationalState.Up
        };

        Assert.True(info.IsCandidate);
    }

    [Fact]
    public void IsCandidate_ReturnsFalse_WhenUnknownDown()
    {
        var info = new NetworkInterfaceInfo
        {
            Id = "test-11",
            Name = "Unknown",
            Description = "Unknown",
            AdapterKind = NetworkAdapterKind.Unknown,
            OperationalState = NetworkOperationalState.Down
        };

        Assert.False(info.IsCandidate);
    }

    [Fact]
    public void IsCandidate_ReturnsTrue_WhenUnknownButUp()
    {
        var info = new NetworkInterfaceInfo
        {
            Id = "test-12",
            Name = "Unknown",
            Description = "Unknown",
            AdapterKind = NetworkAdapterKind.Unknown,
            OperationalState = NetworkOperationalState.Up
        };

        Assert.True(info.IsCandidate);
    }

    [Fact]
    public void NetworkInterfaceInfo_IdCannotBeNull()
    {
        var info = new NetworkInterfaceInfo();
        Assert.NotNull(info.Id);
        Assert.Empty(info.Id);
    }

    [Fact]
    public void NetworkInterfaceInfo_NameCannotBeNull()
    {
        var info = new NetworkInterfaceInfo();
        Assert.NotNull(info.Name);
        Assert.Empty(info.Name);
    }

    [Fact]
    public void NetworkInterfaceInfo_DescriptionCannotBeNull()
    {
        var info = new NetworkInterfaceInfo();
        Assert.NotNull(info.Description);
        Assert.Empty(info.Description);
    }

    [Fact]
    public void NetworkInterfaceInfo_MacAddressCanBeNull()
    {
        var info = new NetworkInterfaceInfo();
        Assert.Null(info.MacAddress);
    }

    [Fact]
    public void NetworkInterfaceInfo_DefaultAdapterKindIsUnknown()
    {
        var info = new NetworkInterfaceInfo();
        Assert.Equal(NetworkAdapterKind.Unknown, info.AdapterKind);
    }

    [Fact]
    public void NetworkInterfaceInfo_DefaultOperationalStateIsUnknown()
    {
        var info = new NetworkInterfaceInfo();
        Assert.Equal(NetworkOperationalState.Unknown, info.OperationalState);
    }

    [Fact]
    public void NetworkInterfaceInfo_DefaultSpeedIsZero()
    {
        var info = new NetworkInterfaceInfo();
        Assert.Equal(0L, info.SpeedBytesPerSecond);
    }
}

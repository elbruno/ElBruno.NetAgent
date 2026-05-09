using System.Threading.Tasks;
using System.Threading;
using Xunit;
using ElBruno.NetAgent.Core.Networking;
using System.Net.NetworkInformation;
using System.Linq;
using System.Collections.Generic;

namespace ElBruno.NetAgent.Tests
{
    public class NetworkInventoryTests
    {
        [Fact]
        public async Task Classification_Returns_WiFi()
        {
            var input = new LightweightNetworkInterface
            {
                Id = "1",
                Name = "Wi-Fi",
                Description = "Intel(R) Dual Band Wireless",
                InterfaceType = NetworkInterfaceType.Wireless80211
            };

            var kind = NetworkInventoryService.Classify(input);
            Assert.Equal(NetworkAdapterKind.WiFi, kind);
        }

        [Fact]
        public async Task Classification_Returns_Ethernet()
        {
            var input = new LightweightNetworkInterface
            {
                Id = "2",
                Name = "Ethernet",
                Description = "Intel(R) Ethernet Connection",
                InterfaceType = NetworkInterfaceType.Ethernet
            };

            var kind = NetworkInventoryService.Classify(input);
            Assert.Equal(NetworkAdapterKind.Ethernet, kind);
        }

        [Fact]
        public async Task Classification_Returns_UsbTethering()
        {
            var input = new LightweightNetworkInterface
            {
                Id = "3",
                Name = "USB Ethernet",
                Description = "Remote NDIS based Internet Sharing Device (USB)",
                InterfaceType = NetworkInterfaceType.Ethernet
            };

            var kind = NetworkInventoryService.Classify(input);
            Assert.Equal(NetworkAdapterKind.UsbTethering, kind);
        }

        [Fact]
        public async Task Classification_Returns_Virtual()
        {
            var input = new LightweightNetworkInterface
            {
                Id = "4",
                Name = "vEthernet (Default Switch)",
                Description = "Hyper-V Virtual Ethernet Adapter",
                InterfaceType = NetworkInterfaceType.Ethernet
            };

            var kind = NetworkInventoryService.Classify(input);
            Assert.Equal(NetworkAdapterKind.Virtual, kind);
        }

        [Fact]
        public async Task Classification_Returns_Loopback()
        {
            var input = new LightweightNetworkInterface
            {
                Id = "5",
                Name = "Loopback Pseudo-Interface 1",
                Description = "Software Loopback Interface",
                InterfaceType = NetworkInterfaceType.Loopback
            };

            var kind = NetworkInventoryService.Classify(input);
            Assert.Equal(NetworkAdapterKind.Loopback, kind);
        }

        [Fact]
        public async Task GetInterfacesAsync_Returns_NonEmpty_When_InjectionProvided()
        {
            var adapters = new[]
            {
                new LightweightNetworkInterface { Id = "1", Name = "Ethernet", Description = "Intel Ethernet", InterfaceType = NetworkInterfaceType.Ethernet }
            };

            var svc = new NetworkInventoryService(adapters);
            var list = await svc.GetInterfacesAsync(CancellationToken.None);

            Assert.NotNull(list);
            Assert.True(list.Any());
            Assert.Equal(1, list.Count);
            Assert.Equal(NetworkAdapterKind.Ethernet, list.First().Kind);
        }
    }
}

using ElBruno.NetAgent.Core.Enums;
using ElBruno.NetAgent.Services.Network;
using Microsoft.Extensions.Logging;
using System.Net.NetworkInformation;

namespace ElBruno.NetAgent.Tests;

public class NetworkAdapterClassificationTests
{
    private readonly NetworkInventoryService _service;

    public NetworkAdapterClassificationTests()
    {
        var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<NetworkInventoryService>();
        _service = new NetworkInventoryService(logger);
    }

    [Fact]
    public async Task GetInterfacesAsync_ReturnsAtLeastOneInterface()
    {
        var interfaces = await _service.GetInterfacesAsync();
        Assert.NotEmpty(interfaces);
    }

    [Fact]
    public async Task GetInterfacesAsync_AllInterfacesHaveNonNullId()
    {
        var interfaces = await _service.GetInterfacesAsync();
        foreach (var iface in interfaces)
        {
            Assert.NotNull(iface.Id);
            Assert.NotEmpty(iface.Id);
        }
    }

    [Fact]
    public async Task GetInterfacesAsync_AllInterfacesHaveNonNullName()
    {
        var interfaces = await _service.GetInterfacesAsync();
        foreach (var iface in interfaces)
        {
            Assert.NotNull(iface.Name);
            Assert.NotEmpty(iface.Name);
        }
    }

    [Fact]
    public async Task GetInterfacesAsync_CandidatesHaveUpState()
    {
        var interfaces = await _service.GetInterfacesAsync();
        var candidates = interfaces.Where(i => i.IsCandidate);
        foreach (var candidate in candidates)
        {
            Assert.Equal(Core.Models.NetworkOperationalState.Up, candidate.OperationalState);
            Assert.NotEqual(NetworkAdapterKind.Loopback, candidate.AdapterKind);
            Assert.NotEqual(NetworkAdapterKind.Virtual, candidate.AdapterKind);
        }
    }

    [Fact]
    public async Task GetInterfacesAsync_ActiveInterfaceIsCandidate()
    {
        var interfaces = await _service.GetInterfacesAsync();
        var upInterfaces = interfaces.Where(i => i.OperationalState == Core.Models.NetworkOperationalState.Up);
        Assert.NotEmpty(upInterfaces);

        foreach (var iface in upInterfaces)
        {
            if (iface.AdapterKind == NetworkAdapterKind.Loopback ||
                iface.AdapterKind == NetworkAdapterKind.Virtual)
            {
                Assert.False(iface.IsCandidate);
            }
            else
            {
                Assert.True(iface.IsCandidate);
            }
        }
    }
}

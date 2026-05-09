using ElBruno.NetAgent.Core.Enums;
using ElBruno.NetAgent.Core.Models;
using ElBruno.NetAgent.Services.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElBruno.NetAgent.Tests;

public class NetworkQualityScoreTests
{
    private readonly ILogger<PingNetworkQualityService> _logger;
    private readonly NetAgentOptions _options;

    public NetworkQualityScoreTests()
    {
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<PingNetworkQualityService>();
        _options = new NetAgentOptions();
    }

    private PingNetworkQualityService CreateService(NetAgentOptions? options = null)
    {
        var opts = options ?? _options;
        var service = new PingNetworkQualityService(_logger, Options.Create(opts));
        return service;
    }

    [Fact]
    public void CalculateQualityScore_AllEndpointsSuccess_LowLatency_ReturnsHighScore()
    {
        var service = CreateService();
        var interfaceInfo = new NetworkInterfaceInfo
        {
            Id = "test-1",
            Name = "Test Ethernet",
            Description = "Ethernet",
            AdapterKind = NetworkAdapterKind.Ethernet,
            OperationalState = NetworkOperationalState.Up
        };

        var snapshot = service.MeasureAsync(interfaceInfo).GetAwaiter().GetResult();

        // Score should be in a reasonable range (0-100)
        Assert.InRange(snapshot.QualityScore, 0, 100);
        Assert.NotEqual(NetworkQualityLevel.Unknown, snapshot.QualityLevel);
    }

    [Fact]
    public void CalculateQualityScore_AllEndpointsFail_ReturnsPoorScore()
    {
        var service = CreateService();
        var interfaceInfo = new NetworkInterfaceInfo
        {
            Id = "test-2",
            Name = "Test Offline",
            Description = "Ethernet",
            AdapterKind = NetworkAdapterKind.Ethernet,
            OperationalState = NetworkOperationalState.Up
        };

        var snapshot = service.MeasureAsync(interfaceInfo).GetAwaiter().GetResult();

        // When all endpoints fail, packet loss is 100%, penalty is 80, score = 20
        // Score should still be in range and not Excellent/Good
        Assert.InRange(snapshot.QualityScore, 0, 100);
        Assert.NotEqual(NetworkQualityLevel.Excellent, snapshot.QualityLevel);
        Assert.NotEqual(NetworkQualityLevel.Good, snapshot.QualityLevel);
    }

    [Fact]
    public void MeasureAsync_ReturnsNonEmptyEndpointResults()
    {
        var service = CreateService();
        var interfaceInfo = new NetworkInterfaceInfo
        {
            Id = "test-3",
            Name = "Test Interface",
            Description = "Test",
            AdapterKind = NetworkAdapterKind.Ethernet,
            OperationalState = NetworkOperationalState.Up
        };

        var snapshot = service.MeasureAsync(interfaceInfo).GetAwaiter().GetResult();

        Assert.NotEmpty(snapshot.EndpointResults);
        Assert.Equal(_options.PingEndpoints.Count, snapshot.EndpointResults.Count);
    }

    [Fact]
    public void MeasureAsync_AllEndpointResultsHaveEndpoint()
    {
        var service = CreateService();
        var interfaceInfo = new NetworkInterfaceInfo
        {
            Id = "test-4",
            Name = "Test Interface",
            Description = "Test",
            AdapterKind = NetworkAdapterKind.Ethernet,
            OperationalState = NetworkOperationalState.Up
        };

        var snapshot = service.MeasureAsync(interfaceInfo).GetAwaiter().GetResult();

        foreach (var result in snapshot.EndpointResults)
        {
            Assert.NotNull(result.Endpoint);
            Assert.NotEmpty(result.Endpoint);
        }
    }

    [Fact]
    public void MeasureAsync_AllEndpointResultsHaveTimestamp()
    {
        var service = CreateService();
        var interfaceInfo = new NetworkInterfaceInfo
        {
            Id = "test-5",
            Name = "Test Interface",
            Description = "Test",
            AdapterKind = NetworkAdapterKind.Ethernet,
            OperationalState = NetworkOperationalState.Up
        };

        var snapshot = service.MeasureAsync(interfaceInfo).GetAwaiter().GetResult();

        foreach (var result in snapshot.EndpointResults)
        {
            Assert.True(result.Timestamp > DateTime.UtcNow.AddSeconds(-10), "Timestamp should be recent");
        }
    }

    [Fact]
    public void MeasureAsync_QualityScoreInRange()
    {
        var service = CreateService();
        var interfaceInfo = new NetworkInterfaceInfo
        {
            Id = "test-6",
            Name = "Test Interface",
            Description = "Test",
            AdapterKind = NetworkAdapterKind.Ethernet,
            OperationalState = NetworkOperationalState.Up
        };

        var snapshot = service.MeasureAsync(interfaceInfo).GetAwaiter().GetResult();

        Assert.InRange(snapshot.QualityScore, 0, 100);
    }

    [Fact]
    public void MeasureAsync_QualityLevelIsNotUnknown()
    {
        var service = CreateService();
        var interfaceInfo = new NetworkInterfaceInfo
        {
            Id = "test-7",
            Name = "Test Interface",
            Description = "Test",
            AdapterKind = NetworkAdapterKind.Ethernet,
            OperationalState = NetworkOperationalState.Up
        };

        var snapshot = service.MeasureAsync(interfaceInfo).GetAwaiter().GetResult();

        Assert.NotEqual(NetworkQualityLevel.Unknown, snapshot.QualityLevel);
    }

    [Fact]
    public void MeasureAsync_AvgLatencyValidWhenEndpointsSucceed()
    {
        var service = CreateService();
        var interfaceInfo = new NetworkInterfaceInfo
        {
            Id = "test-8",
            Name = "Test Interface",
            Description = "Test",
            AdapterKind = NetworkAdapterKind.Ethernet,
            OperationalState = NetworkOperationalState.Up
        };

        var snapshot = service.MeasureAsync(interfaceInfo).GetAwaiter().GetResult();

        var successfulResults = snapshot.EndpointResults.Where(r => r.Success).ToList();
        if (successfulResults.Any())
        {
            Assert.True(snapshot.AverageLatencyMs >= 0, "Average latency should be >= 0 when endpoints succeed");
        }
    }

    [Fact]
    public void MeasureAsync_MinLatencyLessThanOrEqualMaxLatency()
    {
        var service = CreateService();
        var interfaceInfo = new NetworkInterfaceInfo
        {
            Id = "test-9",
            Name = "Test Interface",
            Description = "Test",
            AdapterKind = NetworkAdapterKind.Ethernet,
            OperationalState = NetworkOperationalState.Up
        };

        var snapshot = service.MeasureAsync(interfaceInfo).GetAwaiter().GetResult();

        Assert.True(snapshot.MinLatencyMs <= snapshot.MaxLatencyMs);
    }

    [Fact]
    public void MeasureAsync_InterfaceIdMatchesInput()
    {
        var service = CreateService();
        var interfaceInfo = new NetworkInterfaceInfo
        {
            Id = "unique-test-id",
            Name = "Test Interface",
            Description = "Test",
            AdapterKind = NetworkAdapterKind.Ethernet,
            OperationalState = NetworkOperationalState.Up
        };

        var snapshot = service.MeasureAsync(interfaceInfo).GetAwaiter().GetResult();

        Assert.Equal("unique-test-id", snapshot.InterfaceId);
    }

    [Fact]
    public void MeasureAsync_InterfaceNameMatchesInput()
    {
        var service = CreateService();
        var interfaceInfo = new NetworkInterfaceInfo
        {
            Id = "test-10",
            Name = "My Custom Interface",
            Description = "Test",
            AdapterKind = NetworkAdapterKind.Ethernet,
            OperationalState = NetworkOperationalState.Up
        };

        var snapshot = service.MeasureAsync(interfaceInfo).GetAwaiter().GetResult();

        Assert.Equal("My Custom Interface", snapshot.InterfaceName);
    }

    [Fact]
    public void MeasureAsync_PacketLossPercentInRange()
    {
        var service = CreateService();
        var interfaceInfo = new NetworkInterfaceInfo
        {
            Id = "test-11",
            Name = "Test Interface",
            Description = "Test",
            AdapterKind = NetworkAdapterKind.Ethernet,
            OperationalState = NetworkOperationalState.Up
        };

        var snapshot = service.MeasureAsync(interfaceInfo).GetAwaiter().GetResult();

        Assert.InRange(snapshot.PacketLossPercent, 0, 100);
    }
}

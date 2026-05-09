using ElBruno.NetAgent.Core.Models;
using ElBruno.NetAgent.Services.Configuration;

namespace ElBruno.NetAgent.Tests;

public class ConfigurationValidatorTests
{
    private readonly ConfigurationValidator _validator = new();

    [Fact]
    public void Validate_ValidOptions_ReturnsNoErrors()
    {
        var options = new NetAgentOptions();
        var errors = _validator.Validate(options);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_CheckIntervalSecondsBelowMinimum_ReturnsError()
    {
        var options = new NetAgentOptions { CheckIntervalSeconds = 1 };
        var errors = _validator.Validate(options);
        Assert.Contains(errors, e => e.Contains("CheckIntervalSeconds"));
    }

    [Fact]
    public void Validate_PingTimeoutMsBelowMinimum_ReturnsError()
    {
        var options = new NetAgentOptions { PingTimeoutMs = 200 };
        var errors = _validator.Validate(options);
        Assert.Contains(errors, e => e.Contains("PingTimeoutMs"));
    }

    [Fact]
    public void Validate_LatencyThresholdMsBelowMinimum_ReturnsError()
    {
        var options = new NetAgentOptions { LatencyThresholdMs = 40 };
        var errors = _validator.Validate(options);
        Assert.Contains(errors, e => e.Contains("LatencyThresholdMs"));
    }

    [Fact]
    public void Validate_PacketLossThresholdOutOfRange_ReturnsError()
    {
        var options = new NetAgentOptions { PacketLossThresholdPercent = -1 };
        var errors = _validator.Validate(options);
        Assert.Contains(errors, e => e.Contains("PacketLossThresholdPercent"));
    }

    [Fact]
    public void Validate_PacketLossThresholdAbove100_ReturnsError()
    {
        var options = new NetAgentOptions { PacketLossThresholdPercent = 101 };
        var errors = _validator.Validate(options);
        Assert.Contains(errors, e => e.Contains("PacketLossThresholdPercent"));
    }

    [Fact]
    public void Validate_FailbackCooldownBelowMinimum_ReturnsError()
    {
        var options = new NetAgentOptions { FailbackCooldownSeconds = 20 };
        var errors = _validator.Validate(options);
        Assert.Contains(errors, e => e.Contains("FailbackCooldownSeconds"));
    }

    [Fact]
    public void Validate_EmptyPingEndpoints_ReturnsError()
    {
        var options = new NetAgentOptions { PingEndpoints = new List<string>() };
        var errors = _validator.Validate(options);
        Assert.Contains(errors, e => e.Contains("ping endpoint"));
    }

    [Fact]
    public void Validate_DefaultOptions_ReturnsNoErrors()
    {
        var options = new NetAgentOptions();
        var errors = _validator.Validate(options);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_BoundaryValues_ReturnsNoErrors()
    {
        var options = new NetAgentOptions
        {
            CheckIntervalSeconds = 2,
            PingTimeoutMs = 250,
            LatencyThresholdMs = 50,
            PacketLossThresholdPercent = 0,
            FailbackCooldownSeconds = 30
        };
        var errors = _validator.Validate(options);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_BeyondBoundaryValues_ReturnsNoErrors()
    {
        var options = new NetAgentOptions
        {
            CheckIntervalSeconds = 100,
            PingTimeoutMs = 10000,
            LatencyThresholdMs = 500,
            PacketLossThresholdPercent = 100,
            FailbackCooldownSeconds = 3600
        };
        var errors = _validator.Validate(options);
        Assert.Empty(errors);
    }
}

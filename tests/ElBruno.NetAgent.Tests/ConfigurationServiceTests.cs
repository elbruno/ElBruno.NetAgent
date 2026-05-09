using ElBruno.NetAgent.Core.Models;
using ElBruno.NetAgent.Services.Configuration;
using Microsoft.Extensions.Logging;

namespace ElBruno.NetAgent.Tests;

public class ConfigurationServiceTests
{
    [Fact]
    public void LoadConfiguration_CreatesDefaultConfig_WhenFileDoesNotExist()
    {
        var logger = CreateNullLogger<ConfigurationService>();
        var service = new ConfigurationService(logger);

        // The service will write to %LOCALAPPDATA%\ElBruno.NetAgent\config.json
        // This is the expected behavior per CONFIGURATION.md
        var options = service.LoadConfiguration();

        Assert.NotNull(options);
        Assert.False(options.AutoModeEnabled);
        Assert.True(options.DryRunMode);
        Assert.Equal(5, options.CheckIntervalSeconds);
        Assert.Equal(1000, options.PingTimeoutMs);
        Assert.Equal(180, options.LatencyThresholdMs);
        Assert.Equal(20, options.PacketLossThresholdPercent);
        Assert.Equal(20, options.FailoverDurationSeconds);
        Assert.Equal(120, options.FailbackCooldownSeconds);
        Assert.Equal(3, options.MinimumChecksBeforeSwitch);
        Assert.Equal(20, options.MinimumScoreDeltaToSwitch);
        Assert.Equal("Information", options.LogLevel);
        Assert.True(options.ShowNotifications);
        Assert.True(options.StartMinimizedToTray);
        Assert.Contains("1.1.1.1", options.PingEndpoints);
        Assert.Contains("8.8.8.8", options.PingEndpoints);
        Assert.Contains("9.9.9.9", options.PingEndpoints);
        Assert.Contains("UsbTethering", options.PreferredAdapterKinds);
        Assert.Contains("Ethernet", options.PreferredAdapterKinds);
        Assert.Contains("WiFi", options.PreferredAdapterKinds);
        Assert.Contains("Loopback", options.IgnoredAdapterNameContains);
        Assert.Contains("VirtualBox", options.IgnoredAdapterNameContains);
        Assert.Contains("Hyper-V", options.IgnoredAdapterNameContains);
        Assert.Contains("VMware", options.IgnoredAdapterNameContains);
    }

    [Fact]
    public void ConfigFilePath_IsInLocalAppData()
    {
        var logger = CreateNullLogger<ConfigurationService>();
        var service = new ConfigurationService(logger);

        var expectedDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ElBruno.NetAgent");

        Assert.StartsWith(expectedDir, service.ConfigFilePath);
        Assert.EndsWith("config.json", service.ConfigFilePath);
    }

    [Fact]
    public void LoadConfiguration_ReturnsNonNullableOptions()
    {
        var logger = CreateNullLogger<ConfigurationService>();
        var service = new ConfigurationService(logger);

        var options = service.LoadConfiguration();

        Assert.NotNull(options.PingEndpoints);
        Assert.NotEmpty(options.PingEndpoints);
        Assert.NotNull(options.PreferredAdapterKinds);
        Assert.NotEmpty(options.PreferredAdapterKinds);
        Assert.NotNull(options.IgnoredAdapterNameContains);
        Assert.NotEmpty(options.IgnoredAdapterNameContains);
    }

    private static ILogger<T> CreateNullLogger<T>()
    {
        return LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<T>();
    }
}

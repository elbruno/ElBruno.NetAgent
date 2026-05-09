using ElBruno.NetAgent.Core.Enums;
using ElBruno.NetAgent.Core.Models;
using ElBruno.NetAgent.Infrastructure.Windows;
using ElBruno.NetAgent.Services.Audit;
using ElBruno.NetAgent.Services.Configuration;
using ElBruno.NetAgent.Services.Control;
using Microsoft.Extensions.Logging;

namespace ElBruno.NetAgent.Tests;

/// <summary>
/// Phase 10 guardrail tests: proves that the safety chain works end-to-end.
/// These tests verify that dry-run is the default, live mode is blocked by default,
/// and audit logs clearly mark dry-run actions.
/// </summary>
public class SafetyGuardrailTests
{
    private readonly ILogger<WindowsNetworkController> _logger;
    private readonly ILogger<ConfigurationService> _configLogger;

    public SafetyGuardrailTests()
    {
        var factory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = factory.CreateLogger<WindowsNetworkController>();
        _configLogger = factory.CreateLogger<ConfigurationService>();
    }

    private WindowsNetworkController CreateController(
        NetworkSwitchMode mode = NetworkSwitchMode.DryRun,
        bool liveModeAllowed = false)
    {
        var adminService = new MockAdminService();
        var auditLog = new MockAuditLogService();
        return new WindowsNetworkController(_logger, adminService, auditLog, mode, liveModeAllowed);
    }

    #region Default behavior tests

    [Fact]
    public void NetAgentOptions_DryRunModeDefaultsToTrue()
    {
        // Arrange & Act
        var options = new NetAgentOptions();

        // Assert
        Assert.True(options.DryRunMode, "DryRunMode must default to true");
    }

    [Fact]
    public void NetAgentOptions_LiveModeAllowedDefaultsToFalse()
    {
        // Arrange & Act
        var options = new NetAgentOptions();

        // Assert
        Assert.False(options.LiveModeAllowed, "LiveModeAllowed must default to false");
    }

    [Fact]
    public void NetAgentOptions_AutoModeEnabledDefaultsToFalse()
    {
        // Arrange & Act
        var options = new NetAgentOptions();

        // Assert
        Assert.False(options.AutoModeEnabled, "AutoModeEnabled must default to false");
    }

    [Fact]
    public async Task Controller_DryRunByDefault_ReturnsDryRun()
    {
        // Arrange — use the default constructor (no liveModeAllowed parameter)
        var controller = CreateController();

        // Act
        var result = await controller.PreferInterfaceAsync("eth-1");

        // Assert
        Assert.True(result.IsDryRun, "Controller must default to dry-run mode");
        Assert.True(result.Succeeded);
        Assert.Contains("Dry-run", result.Message);
    }

    [Fact]
    public async Task Controller_LiveModeBlockedByDefault_ForcesDryRun()
    {
        // Arrange — pass Live mode but liveModeAllowed = false (default)
        var controller = CreateController(NetworkSwitchMode.Live, liveModeAllowed: false);

        // Act
        var result = await controller.PreferInterfaceAsync("eth-1");

        // Assert — safety gate forces dry-run
        Assert.True(result.IsDryRun, "Safety gate must force dry-run when LiveModeAllowed is false");
        Assert.True(result.Succeeded);
        Assert.Contains("Dry-run", result.Message);
    }

    [Fact]
    public async Task Controller_LiveModeEnabledWithLiveParam_ReturnsNotImplemented()
    {
        // Arrange — live mode allowed AND Live parameter
        // This is the ONLY case where live mode is actually attempted
        var controller = CreateController(NetworkSwitchMode.Live, liveModeAllowed: true);

        // Act
        var result = await controller.PreferInterfaceAsync("eth-1");

        // Assert — live mode is not yet implemented, so it fails
        Assert.False(result.Succeeded, "Live mode is not yet implemented, so it should still fail");
        Assert.False(result.IsDryRun, "In live mode, IsDryRun should be false");
        Assert.Contains("not yet implemented", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Controller_LiveModeAllowedIsFalse_StillForcesDryRun()
    {
        // Arrange — live mode NOT allowed, even with Live parameter
        var controller = CreateController(NetworkSwitchMode.Live, liveModeAllowed: false);

        // Act
        var result = await controller.PreferInterfaceAsync("eth-1");

        // Assert — safety gate MUST force dry-run
        Assert.True(result.IsDryRun, "Safety gate must force dry-run when LiveModeAllowed is false");
        Assert.True(result.Succeeded);
        Assert.Contains("Dry-run", result.Message);
    }

    [Fact]
    public async Task Controller_LiveParamWithLiveNotAllowed_ForcesDryRun()
    {
        // Arrange — user passes Live mode, but liveModeAllowed is false
        var controller = CreateController(NetworkSwitchMode.Live, liveModeAllowed: false);

        // Act
        var result = await controller.PreferInterfaceAsync("eth-1");

        // Assert — the safety gate must force dry-run
        Assert.True(result.IsDryRun, "Safety gate must force dry-run when LiveModeAllowed is false");
        Assert.True(result.Succeeded);
        Assert.Contains("Dry-run", result.Message);
    }

    [Fact]
    public async Task RestoreAutomaticMetrics_DryRunByDefault_ReturnsDryRun()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.RestoreAutomaticMetricsAsync();

        // Assert
        Assert.True(result.IsDryRun, "Restore must also default to dry-run");
        Assert.True(result.Succeeded);
        Assert.Contains("Dry-run", result.Message);
    }

    [Fact]
    public async Task RestoreAutomaticMetrics_LiveBlockedByDefault_ForcesDryRun()
    {
        // Arrange — Live mode requested but liveModeAllowed = false (default)
        var controller = CreateController(NetworkSwitchMode.Live, liveModeAllowed: false);

        // Act
        var result = await controller.RestoreAutomaticMetricsAsync();

        // Assert — safety gate must force dry-run
        Assert.True(result.IsDryRun, "Safety gate must force dry-run when LiveModeAllowed is false");
        Assert.True(result.Succeeded);
        Assert.Contains("Dry-run", result.Message);
    }

    #endregion

    #region Configuration validator safety tests

    [Fact]
    public void Validator_DryRunModeFalse_ReturnsError()
    {
        // Arrange
        var validator = new ConfigurationValidator();
        var options = new NetAgentOptions { DryRunMode = false };

        // Act
        var errors = validator.Validate(options);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("DryRunMode"));
    }

    [Fact]
    public void Validator_AutoModeEnabledTrue_ReturnsError()
    {
        // Arrange
        var validator = new ConfigurationValidator();
        var options = new NetAgentOptions { AutoModeEnabled = true };

        // Act
        var errors = validator.Validate(options);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("AutoModeEnabled"));
    }

    [Fact]
    public void Validator_LiveModeAllowedTrue_ReturnsError()
    {
        // Arrange
        var validator = new ConfigurationValidator();
        var options = new NetAgentOptions { LiveModeAllowed = true };

        // Act
        var errors = validator.Validate(options);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("LiveModeAllowed"));
    }

    [Fact]
    public void Validator_AllUnsafeSettings_ReturnsAllErrors()
    {
        // Arrange
        var validator = new ConfigurationValidator();
        var options = new NetAgentOptions
        {
            DryRunMode = false,
            AutoModeEnabled = true,
            LiveModeAllowed = true
        };

        // Act
        var errors = validator.Validate(options);

        // Assert
        Assert.Equal(3, errors.Count);
        Assert.Contains(errors, e => e.Contains("DryRunMode"));
        Assert.Contains(errors, e => e.Contains("AutoModeEnabled"));
        Assert.Contains(errors, e => e.Contains("LiveModeAllowed"));
    }

    [Fact]
    public void Validator_DefaultOptions_PassesSafetyChecks()
    {
        // Arrange
        var validator = new ConfigurationValidator();
        var options = new NetAgentOptions(); // all defaults

        // Act
        var errors = validator.Validate(options);

        // Assert
        Assert.Empty(errors);
    }

    #endregion

    #region Audit log safety tests

    [Fact]
    public async Task AuditLog_DryRunEntry_MarksIsDryRunTrue()
    {
        // Arrange
        var auditLog = new MockAuditLogService();
        var controller = new WindowsNetworkController(_logger, new MockAdminService(), auditLog, NetworkSwitchMode.DryRun, liveModeAllowed: false);

        // Act
        await controller.PreferInterfaceAsync("eth-1");

        // Assert
        var entries = await auditLog.GetEntriesAsync();
        Assert.Single(entries);
        Assert.True(entries[0].IsDryRun, "Audit entries must clearly mark dry-run actions");
    }

    [Fact]
    public async Task AuditLog_DryRunEntry_ContainsDiagnostics()
    {
        // Arrange
        var auditLog = new MockAuditLogService();
        var controller = new WindowsNetworkController(_logger, new MockAdminService(), auditLog, NetworkSwitchMode.DryRun, liveModeAllowed: false);

        // Act
        await controller.PreferInterfaceAsync("wifi-1");

        // Assert
        var entries = await auditLog.GetEntriesAsync();
        Assert.Single(entries);
        Assert.NotNull(entries[0].Details);
        Assert.Contains("metric 50", entries[0].Details!);
        Assert.Contains("No real changes made", entries[0].Details!);
    }

    [Fact]
    public async Task AuditLog_DryRunEntry_ActionIsCorrect()
    {
        // Arrange
        var auditLog = new MockAuditLogService();
        var controller = new WindowsNetworkController(_logger, new MockAdminService(), auditLog, NetworkSwitchMode.DryRun, liveModeAllowed: false);

        // Act
        await controller.PreferInterfaceAsync("eth-2");

        // Assert
        var entries = await auditLog.GetEntriesAsync();
        Assert.Single(entries);
        Assert.Equal("PreferInterface", entries[0].Action);
        Assert.Equal("eth-2", entries[0].Target);
    }

    [Fact]
    public async Task AuditLog_RestoreEntry_MarksIsDryRunTrue()
    {
        // Arrange
        var auditLog = new MockAuditLogService();
        var controller = new WindowsNetworkController(_logger, new MockAdminService(), auditLog, NetworkSwitchMode.DryRun, liveModeAllowed: false);

        // Act
        await controller.RestoreAutomaticMetricsAsync();

        // Assert
        var entries = await auditLog.GetEntriesAsync();
        Assert.Single(entries);
        Assert.True(entries[0].IsDryRun);
        Assert.Equal("RestoreAutomaticMetrics", entries[0].Action);
    }

    #endregion

    #region End-to-end safety chain

    [Fact]
    public void SafetyChain_DefaultConfig_AllowsNoLiveExecution()
    {
        // Arrange — simulate loading default config with an isolated temp directory
        var tempDir = Path.Combine(Path.GetTempPath(), "ElBruno.NetAgent.Test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var configService = new ConfigurationService(_configLogger, tempDir);
            var options = configService.LoadConfiguration();

            // Assert — safety defaults
            Assert.True(options.DryRunMode);
            Assert.False(options.AutoModeEnabled);
            Assert.False(options.LiveModeAllowed);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task SafetyChain_ControllerRespectsLiveModeNotAllowed_ForcesDryRun()
    {
        // Arrange — controller built without live mode allowed
        var auditLog = new MockAuditLogService();
        var controller = new WindowsNetworkController(
            _logger,
            new MockAdminService(),
            auditLog,
            NetworkSwitchMode.Live, // user tries to request live
            liveModeAllowed: false  // but safety gate blocks it
        );

        // Act
        var result = await controller.PreferInterfaceAsync("eth-1");

        // Assert
        Assert.True(result.IsDryRun, "Safety gate must force dry-run");
        Assert.True(result.Succeeded);
        Assert.Equal("eth-1", result.InterfaceId);

        // Verify audit log shows dry-run
        var entries = await auditLog.GetEntriesAsync();
        Assert.Single(entries);
        Assert.True(entries[0].IsDryRun);
    }

    #endregion

    #region Mocks

    private class MockAdminService : IWindowsAdminService
    {
        public bool IsAdministrator => false;
    }

    private class MockAuditLogService : IAuditLogService
    {
        public List<AuditLogEntry> Entries { get; } = new();

        public Task AddEntryAsync(AuditLogEntry entry)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditLogEntry>> GetEntriesAsync()
        {
            return Task.FromResult<IReadOnlyList<AuditLogEntry>>(Entries.AsReadOnly());
        }

        public Task<IReadOnlyList<AuditLogEntry>> GetEntriesByActionAsync(string action)
        {
            return Task.FromResult<IReadOnlyList<AuditLogEntry>>(
                Entries.Where(e => e.Action.Equals(action, StringComparison.OrdinalIgnoreCase))
                    .ToList()
                    .AsReadOnly());
        }

        public Task ClearAsync()
        {
            Entries.Clear();
            return Task.CompletedTask;
        }
    }

    #endregion
}

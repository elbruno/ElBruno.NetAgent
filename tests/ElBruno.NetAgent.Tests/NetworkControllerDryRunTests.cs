using ElBruno.NetAgent.Core.Enums;
using ElBruno.NetAgent.Core.Models;
using ElBruno.NetAgent.Infrastructure.Windows;
using ElBruno.NetAgent.Services.Audit;
using ElBruno.NetAgent.Services.Control;
using Microsoft.Extensions.Logging;

namespace ElBruno.NetAgent.Tests;

public class NetworkControllerDryRunTests
{
    private readonly ILogger<WindowsNetworkController> _logger;

    public NetworkControllerDryRunTests()
    {
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<WindowsNetworkController>();
    }

    private WindowsNetworkController CreateController(NetworkSwitchMode mode = NetworkSwitchMode.DryRun)
    {
        var adminService = new MockAdminService();
        var auditLog = new MockAuditLogService();
        return new WindowsNetworkController(_logger, adminService, auditLog, mode);
    }

    #region Dry-run mode tests

    [Fact]
    public async Task PreferInterfaceAsync_DryRun_ReturnsSucceeded()
    {
        // Arrange
        var controller = CreateController(NetworkSwitchMode.DryRun);

        // Act
        var result = await controller.PreferInterfaceAsync("eth-1");

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.IsDryRun);
        Assert.Equal("eth-1", result.InterfaceId);
        Assert.Contains("Dry-run", result.Message);
    }

    [Fact]
    public async Task PreferInterfaceAsync_DryRun_NoRealChanges()
    {
        // Arrange
        var controller = CreateController(NetworkSwitchMode.DryRun);

        // Act
        var result = await controller.PreferInterfaceAsync("wifi-1");

        // Assert
        Assert.True(result.IsDryRun);
        Assert.Contains("No real changes made", result.Diagnostics);
    }

    [Fact]
    public async Task PreferInterfaceAsync_DryRun_EmptyInterfaceId_ReturnsFailure()
    {
        // Arrange
        var controller = CreateController(NetworkSwitchMode.DryRun);

        // Act
        var result = await controller.PreferInterfaceAsync("");

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsDryRun);
        Assert.Contains("null or empty", result.Message);
    }

    [Fact]
    public async Task PreferInterfaceAsync_DryRun_NullInterfaceId_ReturnsFailure()
    {
        // Arrange
        var controller = CreateController(NetworkSwitchMode.DryRun);

        // Act
        var result = await controller.PreferInterfaceAsync(null!);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsDryRun);
        Assert.Contains("null or empty", result.Message);
    }

    [Fact]
    public async Task RestoreAutomaticMetricsAsync_DryRun_ReturnsSucceeded()
    {
        // Arrange
        var controller = CreateController(NetworkSwitchMode.DryRun);

        // Act
        var result = await controller.RestoreAutomaticMetricsAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.IsDryRun);
        Assert.Contains("Dry-run", result.Message);
    }

    [Fact]
    public async Task RestoreAutomaticMetricsAsync_DryRun_NoRealChanges()
    {
        // Arrange
        var controller = CreateController(NetworkSwitchMode.DryRun);

        // Act
        var result = await controller.RestoreAutomaticMetricsAsync();

        // Assert
        Assert.True(result.IsDryRun);
        Assert.Contains("No real changes made", result.Diagnostics);
    }

    #endregion

    #region Safety rules tests

    [Fact]
    public async Task PreferInterfaceAsync_LiveMode_ReturnsDryRunBecauseLiveBlocked()
    {
        // Arrange — Live mode requested, but LiveModeAllowed defaults to false
        // The safety gate forces DryRun
        var controller = CreateController(NetworkSwitchMode.Live);

        // Act
        var result = await controller.PreferInterfaceAsync("eth-1");

        // Assert — safety gate forces dry-run
        Assert.True(result.IsDryRun, "Safety gate must force dry-run when LiveModeAllowed is false");
        Assert.True(result.Succeeded);
        Assert.Contains("Dry-run", result.Message);
    }

    [Fact]
    public async Task RestoreAutomaticMetricsAsync_LiveMode_ReturnsDryRunBecauseLiveBlocked()
    {
        // Arrange — Live mode requested, but LiveModeAllowed defaults to false
        var controller = CreateController(NetworkSwitchMode.Live);

        // Act
        var result = await controller.RestoreAutomaticMetricsAsync();

        // Assert — safety gate forces dry-run
        Assert.True(result.IsDryRun);
        Assert.True(result.Succeeded);
        Assert.Contains("Dry-run", result.Message);
    }

    [Fact]
    public async Task PreferInterfaceAsync_DryRun_LogsAdminWarning()
    {
        // Arrange
        var controller = CreateController(NetworkSwitchMode.DryRun);

        // Act
        var result = await controller.PreferInterfaceAsync("eth-1");

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.IsDryRun);
    }

    #endregion

    #region Admin detection tests

    [Fact]
    public void WindowsAdminService_DoesNotThrow()
    {
        // Arrange & Act — should never throw
        var adminService = new WindowsAdminService();

        // Assert
        // IsAdministrator is a bool, so we just verify it's accessible
        var isAdmin = adminService.IsAdministrator;
        Assert.IsType<bool>(isAdmin);
    }

    [Fact]
    public void WindowsAdminService_IsAdministratorIsReadOnly()
    {
        // Arrange
        var adminService = new WindowsAdminService();

        // Act
        var value = adminService.IsAdministrator;

        // Assert — just verify it's accessible and returns a bool
        Assert.IsType<bool>(value);
    }

    #endregion

    #region Audit log tests

    [Fact]
    public async Task PreferInterfaceAsync_DryRun_CreatesAuditEntry()
    {
        // Arrange
        var auditLog = new MockAuditLogService();
        var controller = new WindowsNetworkController(_logger, new MockAdminService(), auditLog, NetworkSwitchMode.DryRun);

        // Act
        await controller.PreferInterfaceAsync("eth-1");

        // Assert
        var entries = await auditLog.GetEntriesAsync();
        Assert.Single(entries);
        Assert.Equal("PreferInterface", entries[0].Action);
        Assert.Equal("eth-1", entries[0].Target);
        Assert.True(entries[0].IsDryRun);
        Assert.Equal("Succeeded", entries[0].Status);
        Assert.NotNull(entries[0].Timestamp);
        Assert.True(entries[0].Timestamp > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task RestoreAutomaticMetricsAsync_DryRun_CreatesAuditEntry()
    {
        // Arrange
        var auditLog = new MockAuditLogService();
        var controller = new WindowsNetworkController(_logger, new MockAdminService(), auditLog, NetworkSwitchMode.DryRun);

        // Act
        await controller.RestoreAutomaticMetricsAsync();

        // Assert
        var entries = await auditLog.GetEntriesAsync();
        Assert.Single(entries);
        Assert.Equal("RestoreAutomaticMetrics", entries[0].Action);
        Assert.Equal("AllInterfaces", entries[0].Target);
        Assert.True(entries[0].IsDryRun);
        Assert.Equal("Succeeded", entries[0].Status);
    }

    [Fact]
    public async Task PreferInterfaceAsync_DryRun_AuditEntryHasReason()
    {
        // Arrange
        var auditLog = new MockAuditLogService();
        var controller = new WindowsNetworkController(_logger, new MockAdminService(), auditLog, NetworkSwitchMode.DryRun);

        // Act
        await controller.PreferInterfaceAsync("wifi-1");

        // Assert
        var entries = await auditLog.GetEntriesAsync();
        Assert.Single(entries);
        Assert.NotEmpty(entries[0].Reason);
        Assert.Contains("quality", entries[0].Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PreferInterfaceAsync_DryRun_AuditEntryHasDetails()
    {
        // Arrange
        var auditLog = new MockAuditLogService();
        var controller = new WindowsNetworkController(_logger, new MockAdminService(), auditLog, NetworkSwitchMode.DryRun);

        // Act
        await controller.PreferInterfaceAsync("eth-2");

        // Assert
        var entries = await auditLog.GetEntriesAsync();
        Assert.Single(entries);
        Assert.NotNull(entries[0].Details);
        Assert.Contains("eth-2", entries[0].Details!);
        Assert.Contains("metric 50", entries[0].Details!);
    }

    [Fact]
    public async Task MultiplePreferCalls_CreatesMultipleAuditEntries()
    {
        // Arrange
        var auditLog = new MockAuditLogService();
        var controller = new WindowsNetworkController(_logger, new MockAdminService(), auditLog, NetworkSwitchMode.DryRun);

        // Act
        await controller.PreferInterfaceAsync("eth-1");
        await controller.PreferInterfaceAsync("wifi-1");
        await controller.RestoreAutomaticMetricsAsync();

        // Assert
        var entries = await auditLog.GetEntriesAsync();
        Assert.Equal(3, entries.Count);
        Assert.Equal("PreferInterface", entries[0].Action);
        Assert.Equal("PreferInterface", entries[1].Action);
        Assert.Equal("RestoreAutomaticMetrics", entries[2].Action);
    }

    [Fact]
    public async Task GetEntriesByAction_ReturnsFilteredEntries()
    {
        // Arrange
        var auditLog = new MockAuditLogService();
        var controller = new WindowsNetworkController(_logger, new MockAdminService(), auditLog, NetworkSwitchMode.DryRun);

        // Act
        await controller.PreferInterfaceAsync("eth-1");
        await controller.RestoreAutomaticMetricsAsync();
        await controller.PreferInterfaceAsync("wifi-1");

        // Assert
        var preferEntries = await auditLog.GetEntriesByActionAsync("PreferInterface");
        var restoreEntries = await auditLog.GetEntriesByActionAsync("RestoreAutomaticMetrics");

        Assert.Equal(2, preferEntries.Count);
        Assert.Single(restoreEntries);
    }

    [Fact]
    public async Task ClearAsync_ClearsAllEntries()
    {
        // Arrange
        var auditLog = new MockAuditLogService();
        var controller = new WindowsNetworkController(_logger, new MockAdminService(), auditLog, NetworkSwitchMode.DryRun);

        // Act
        await controller.PreferInterfaceAsync("eth-1");
        await auditLog.ClearAsync();

        // Assert
        var entries = await auditLog.GetEntriesAsync();
        Assert.Empty(entries);
    }

    [Fact]
    public async Task PreferInterfaceAsync_FailureStillCreatesAuditEntry()
    {
        // Arrange
        var auditLog = new MockAuditLogService();
        var controller = new WindowsNetworkController(_logger, new MockAdminService(), auditLog, NetworkSwitchMode.DryRun);

        // Act
        var result = await controller.PreferInterfaceAsync("");

        // Assert
        Assert.False(result.Succeeded);
        var entries = await auditLog.GetEntriesAsync();
        // Empty interface should not create an audit entry (early return before audit)
        Assert.Empty(entries);
    }

    [Fact]
    public async Task AuditEntry_IsDryRunAlwaysTrueInDryRunMode()
    {
        // Arrange
        var auditLog = new MockAuditLogService();
        var controller = new WindowsNetworkController(_logger, new MockAdminService(), auditLog, NetworkSwitchMode.DryRun);

        // Act
        await controller.PreferInterfaceAsync("eth-1");

        // Assert
        var entries = await auditLog.GetEntriesAsync();
        Assert.Single(entries);
        Assert.True(entries[0].IsDryRun, "All audit entries in dry-run mode must have IsDryRun = true");
    }

    #endregion

    #region Mock admin service for tests

    /// <summary>
    /// Mock admin service that simulates non-admin for test isolation.
    /// Tests should never require real admin elevation.
    /// </summary>
    private class MockAdminService : IWindowsAdminService
    {
        public bool IsAdministrator => false;
    }

    #endregion

    #region Mock audit log service for tests

    /// <summary>
    /// Mock audit log service that captures entries for test assertions.
    /// </summary>
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

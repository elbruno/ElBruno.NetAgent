using ElBruno.NetAgent.Core.Models;
using ElBruno.NetAgent.Services.Audit;
using ElBruno.NetAgent.UI.ViewModels;

namespace ElBruno.NetAgent.Tests;

public class DryRunStatusViewModelTests
{
    private MockAuditLogService CreateMockAuditLog(params AuditLogEntry[] entries)
    {
        return new MockAuditLogService(entries);
    }

    [Fact]
    public void Constructor_InitializesStatusTextToDryRunMode()
    {
        // Arrange
        var auditLog = CreateMockAuditLog();

        // Act
        var viewModel = new DryRunStatusViewModel(auditLog);

        // Assert
        Assert.Equal("DRY-RUN MODE", viewModel.StatusText);
    }

    [Fact]
    public void Constructor_InitializesLatestEntriesAsEmpty()
    {
        // Arrange
        var auditLog = CreateMockAuditLog();

        // Act
        var viewModel = new DryRunStatusViewModel(auditLog);

        // Assert
        Assert.NotNull(viewModel.LatestEntries);
        Assert.Empty(viewModel.LatestEntries);
    }

    [Fact]
    public void Constructor_LoadsAuditLogEntries()
    {
        // Arrange
        var entry1 = new AuditLogEntry
        {
            Timestamp = DateTime.UtcNow.AddSeconds(-2),
            Action = "PreferInterface",
            Target = "eth-1",
            Reason = "Test",
            IsDryRun = true,
            Status = "Succeeded"
        };
        var entry2 = new AuditLogEntry
        {
            Timestamp = DateTime.UtcNow.AddSeconds(-1),
            Action = "RestoreAutomaticMetrics",
            Target = "All",
            Reason = "Test",
            IsDryRun = true,
            Status = "Succeeded"
        };
        var auditLog = CreateMockAuditLog(entry1, entry2);

        // Act
        var viewModel = new DryRunStatusViewModel(auditLog);

        // Assert
        Assert.Equal(2, viewModel.LatestEntries.Count);
    }

    [Fact]
    public void UpdateStatus_UpdatesStatusText()
    {
        // Arrange
        var auditLog = CreateMockAuditLog();
        var viewModel = new DryRunStatusViewModel(auditLog);

        // Act
        viewModel.UpdateStatus("Running");

        // Assert
        Assert.Equal("Running", viewModel.StatusText);
    }

    [Fact]
    public void MaxEntries_DefaultIsTwenty()
    {
        // Arrange
        var auditLog = CreateMockAuditLog();

        // Act
        var viewModel = new DryRunStatusViewModel(auditLog);

        // Assert
        Assert.Equal(20, viewModel.MaxEntries);
    }

    [Fact]
    public void MaxEntries_CanBeSet()
    {
        // Arrange
        var auditLog = CreateMockAuditLog();
        var viewModel = new DryRunStatusViewModel(auditLog);

        // Act
        viewModel.MaxEntries = 50;

        // Assert
        Assert.Equal(50, viewModel.MaxEntries);
    }

    [Fact]
    public void RefreshCommand_RefreshesEntries()
    {
        // Arrange
        var auditLog = new MockAuditLogService();
        var viewModel = new DryRunStatusViewModel(auditLog);
        Assert.Empty(viewModel.LatestEntries);

        // Add entries after construction
        var entry = new AuditLogEntry
        {
            Timestamp = DateTime.UtcNow,
            Action = "PreferInterface",
            Target = "eth-1",
            Reason = "Test",
            IsDryRun = true,
            Status = "Succeeded"
        };
        auditLog.AddEntry(entry);

        // Act
        viewModel.RefreshCommand.Execute(null);

        // Assert
        Assert.Single(viewModel.LatestEntries);
        Assert.Equal("PreferInterface", viewModel.LatestEntries[0].Action);
    }

    [Fact]
    public void LatestEntries_IsOrderedByTimestampDescending()
    {
        // Arrange
        var entry1 = new AuditLogEntry
        {
            Timestamp = DateTime.UtcNow.AddSeconds(-3),
            Action = "Old",
            Target = "t",
            Reason = "r",
            IsDryRun = true,
            Status = "S"
        };
        var entry2 = new AuditLogEntry
        {
            Timestamp = DateTime.UtcNow.AddSeconds(-1),
            Action = "New",
            Target = "t",
            Reason = "r",
            IsDryRun = true,
            Status = "S"
        };
        var auditLog = CreateMockAuditLog(entry1, entry2);

        // Act
        var viewModel = new DryRunStatusViewModel(auditLog);

        // Assert
        Assert.Equal("New", viewModel.LatestEntries[0].Action);
        Assert.Equal("Old", viewModel.LatestEntries[1].Action);
    }

    [Fact]
    public void LatestEntries_RespectsMaxEntries()
    {
        // Arrange
        var entries = Enumerable.Range(1, 30)
            .Select(i => new AuditLogEntry
            {
                Timestamp = DateTime.UtcNow.AddSeconds(-i),
                Action = $"Action{i}",
                Target = "t",
                Reason = "r",
                IsDryRun = true,
                Status = "S"
            })
            .ToArray();
        var auditLog = CreateMockAuditLog(entries);

        // Act
        var viewModel = new DryRunStatusViewModel(auditLog) { MaxEntries = 10 };

        // Manually refresh to apply new MaxEntries
        viewModel.RefreshCommand.Execute(null);

        // Assert
        Assert.True(viewModel.LatestEntries.Count <= 10,
            $"Expected at most 10 entries, got {viewModel.LatestEntries.Count}");
    }

    [Fact]
    public void StatusText_RaisesPropertyChanged()
    {
        // Arrange
        var auditLog = CreateMockAuditLog();
        var viewModel = new DryRunStatusViewModel(auditLog);
        bool propertyChangedFired = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == "StatusText") propertyChangedFired = true;
        };

        // Act
        viewModel.UpdateStatus("New Status");

        // Assert
        Assert.True(propertyChangedFired, "PropertyChanged should have fired for StatusText");
    }

    /// <summary>
    /// Mock audit log service for testing without real dependencies.
    /// </summary>
    private class MockAuditLogService : IAuditLogService
    {
        private readonly List<AuditLogEntry> _entries = new();

        public MockAuditLogService(params AuditLogEntry[] entries)
        {
            _entries.AddRange(entries);
        }

        public Task AddEntryAsync(AuditLogEntry entry)
        {
            _entries.Add(entry);
            return Task.CompletedTask;
        }

        public void AddEntry(AuditLogEntry entry)
        {
            _entries.Add(entry);
        }

        public Task<IReadOnlyList<AuditLogEntry>> GetEntriesAsync()
        {
            return Task.FromResult<IReadOnlyList<AuditLogEntry>>(_entries.AsReadOnly());
        }

        public Task<IReadOnlyList<AuditLogEntry>> GetEntriesByActionAsync(string action)
        {
            return Task.FromResult<IReadOnlyList<AuditLogEntry>>(
                _entries.Where(e => e.Action.Equals(action, StringComparison.OrdinalIgnoreCase))
                    .ToList()
                    .AsReadOnly());
        }

        public Task ClearAsync()
        {
            _entries.Clear();
            return Task.CompletedTask;
        }
    }
}

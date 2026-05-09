using ElBruno.NetAgent.Core.Models;
using ElBruno.NetAgent.Services.Audit;

namespace ElBruno.NetAgent.Tests;

public class InMemoryAuditLogServiceTests
{
    [Fact]
    public async Task AddEntryAsync_AddsEntryToList()
    {
        // Arrange
        var service = new InMemoryAuditLogService();
        var entry = new AuditLogEntry
        {
            Timestamp = DateTime.UtcNow,
            Action = "PreferInterface",
            Target = "eth-1",
            Reason = "Test reason",
            IsDryRun = true,
            Status = "Succeeded"
        };

        // Act
        await service.AddEntryAsync(entry);

        // Assert
        var entries = await service.GetEntriesAsync();
        Assert.Single(entries);
        Assert.Equal("PreferInterface", entries[0].Action);
    }

    [Fact]
    public async Task AddEntryAsync_NullEntry_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new InMemoryAuditLogService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.AddEntryAsync(null!));
    }

    [Fact]
    public async Task GetEntriesAsync_ReturnsEntriesInOrder()
    {
        // Arrange
        var service = new InMemoryAuditLogService();
        var entry1 = new AuditLogEntry { Timestamp = DateTime.UtcNow.AddSeconds(-2), Action = "A", Target = "t1", Reason = "r", Status = "S" };
        var entry2 = new AuditLogEntry { Timestamp = DateTime.UtcNow.AddSeconds(-1), Action = "B", Target = "t2", Reason = "r", Status = "S" };

        // Act
        await service.AddEntryAsync(entry1);
        await service.AddEntryAsync(entry2);
        var entries = await service.GetEntriesAsync();

        // Assert
        Assert.Equal(2, entries.Count);
    }

    [Fact]
    public async Task GetEntriesByAction_ReturnsMatchingEntries()
    {
        // Arrange
        var service = new InMemoryAuditLogService();
        var entry1 = new AuditLogEntry { Timestamp = DateTime.UtcNow, Action = "PreferInterface", Target = "eth-1", Reason = "r", Status = "S" };
        var entry2 = new AuditLogEntry { Timestamp = DateTime.UtcNow, Action = "RestoreAutomaticMetrics", Target = "All", Reason = "r", Status = "S" };

        // Act
        await service.AddEntryAsync(entry1);
        await service.AddEntryAsync(entry2);
        var preferEntries = await service.GetEntriesByActionAsync("PreferInterface");

        // Assert
        Assert.Single(preferEntries);
        Assert.Equal("PreferInterface", preferEntries[0].Action);
    }

    [Fact]
    public async Task GetEntriesByAction_CaseInsensitive()
    {
        // Arrange
        var service = new InMemoryAuditLogService();
        var entry = new AuditLogEntry { Timestamp = DateTime.UtcNow, Action = "PreferInterface", Target = "eth-1", Reason = "r", Status = "S" };

        // Act
        await service.AddEntryAsync(entry);
        var entries = await service.GetEntriesByActionAsync("preferinterface");

        // Assert
        Assert.Single(entries);
    }

    [Fact]
    public async Task GetEntriesByAction_EmptyAction_ThrowsArgumentException()
    {
        // Arrange
        var service = new InMemoryAuditLogService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetEntriesByActionAsync(""));
    }

    [Fact]
    public async Task ClearAsync_RemovesAllEntries()
    {
        // Arrange
        var service = new InMemoryAuditLogService();
        var entry = new AuditLogEntry { Timestamp = DateTime.UtcNow, Action = "A", Target = "t", Reason = "r", Status = "S" };

        // Act
        await service.AddEntryAsync(entry);
        await service.ClearAsync();
        var entries = await service.GetEntriesAsync();

        // Assert
        Assert.Empty(entries);
    }

    [Fact]
    public async Task AuditEntry_PropertiesAreSettable()
    {
        // Arrange & Act
        var entry = new AuditLogEntry
        {
            Timestamp = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Action = "TestAction",
            Target = "TestTarget",
            Reason = "TestReason",
            IsDryRun = true,
            Status = "TestStatus",
            Details = "TestDetails"
        };

        // Assert
        Assert.Equal(new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc), entry.Timestamp);
        Assert.Equal("TestAction", entry.Action);
        Assert.Equal("TestTarget", entry.Target);
        Assert.Equal("TestReason", entry.Reason);
        Assert.True(entry.IsDryRun);
        Assert.Equal("TestStatus", entry.Status);
        Assert.Equal("TestDetails", entry.Details);
    }
}

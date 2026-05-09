using ElBruno.NetAgent.Core.Models;

namespace ElBruno.NetAgent.Services.Audit;

/// <summary>
/// In-memory implementation of IAuditLogService.
/// Entries are stored in a thread-safe list and are lost when the service is disposed.
/// </summary>
public class InMemoryAuditLogService : IAuditLogService
{
    private readonly List<AuditLogEntry> _entries = new();
    private readonly object _lock = new();

    public async Task AddEntryAsync(AuditLogEntry entry)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));

        lock (_lock)
        {
            _entries.Add(entry);
        }

        return;
    }

    public async Task<IReadOnlyList<AuditLogEntry>> GetEntriesAsync()
    {
        lock (_lock)
        {
            return _entries.AsReadOnly();
        }
    }

    public async Task<IReadOnlyList<AuditLogEntry>> GetEntriesByActionAsync(string action)
    {
        if (string.IsNullOrWhiteSpace(action)) throw new ArgumentException("Action cannot be null or empty.", nameof(action));

        lock (_lock)
        {
            return _entries
                .Where(e => e.Action.Equals(action, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly();
        }
    }

    public async Task ClearAsync()
    {
        lock (_lock)
        {
            _entries.Clear();
        }

        return;
    }
}

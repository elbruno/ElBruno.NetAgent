using ElBruno.NetAgent.Core.Models;

namespace ElBruno.NetAgent.Services.Audit;

/// <summary>
/// Service responsible for recording and retrieving audit log entries.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Adds a new audit log entry.
    /// </summary>
    Task AddEntryAsync(AuditLogEntry entry);

    /// <summary>
    /// Returns all audit log entries, ordered by timestamp descending.
    /// </summary>
    Task<IReadOnlyList<AuditLogEntry>> GetEntriesAsync();

    /// <summary>
    /// Returns all audit log entries for a specific action.
    /// </summary>
    Task<IReadOnlyList<AuditLogEntry>> GetEntriesByActionAsync(string action);

    /// <summary>
    /// Clears all audit log entries.
    /// </summary>
    Task ClearAsync();
}

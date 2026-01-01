using Nis2Shield.AspNetCore.Logging;

namespace Nis2Shield.AspNetCore.Siem;

/// <summary>
/// Interface for SIEM connectors.
/// </summary>
public interface ISiemConnector
{
    /// <summary>
    /// Sends a single log entry to the SIEM.
    /// </summary>
    Task SendAsync(ForensicLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends multiple log entries to the SIEM in bulk.
    /// </summary>
    Task SendBatchAsync(IEnumerable<ForensicLogEntry> entries, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to the SIEM.
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}

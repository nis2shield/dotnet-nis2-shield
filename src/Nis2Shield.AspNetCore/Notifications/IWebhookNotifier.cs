namespace Nis2Shield.AspNetCore.Notifications;

/// <summary>
/// Interface for webhook notification services.
/// </summary>
public interface IWebhookNotifier
{
    /// <summary>
    /// Sends a notification for a security event.
    /// </summary>
    /// <param name="eventType">Type of event (e.g., "rate_limit_exceeded", "tor_node_blocked").</param>
    /// <param name="message">Event message.</param>
    /// <param name="details">Additional event details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifyAsync(string eventType, string message, Dictionary<string, object>? details = null, CancellationToken cancellationToken = default);
}

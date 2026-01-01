using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nis2Shield.AspNetCore.Configuration;

namespace Nis2Shield.AspNetCore.Notifications;

/// <summary>
/// Webhook notifier that sends alerts to Slack, Microsoft Teams, Discord, or generic HTTP endpoints.
/// Notifications are sent asynchronously to avoid blocking the request pipeline.
/// </summary>
public class WebhookNotifier : IWebhookNotifier
{
    private readonly HttpClient _httpClient;
    private readonly WebhookOptions _options;
    private readonly ILogger<WebhookNotifier> _logger;

    public WebhookNotifier(
        HttpClient httpClient,
        IOptions<Nis2Options> options,
        ILogger<WebhookNotifier> logger)
    {
        _httpClient = httpClient;
        _options = options.Value.Webhooks;
        _logger = logger;
    }

    public async Task NotifyAsync(
        string eventType, 
        string message, 
        Dictionary<string, object>? details = null, 
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || _options.Targets.Count == 0)
            return;

        var tasks = _options.Targets
            .Where(t => t.Events.Contains(eventType) || t.Events.Contains("*"))
            .Select(target => SendToTargetAsync(target, eventType, message, details, cancellationToken));

        // Fire and forget - don't await to avoid blocking the request
        _ = Task.WhenAll(tasks).ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                _logger.LogError(t.Exception, "One or more webhook notifications failed");
            }
        }, cancellationToken);
    }

    private async Task SendToTargetAsync(
        WebhookTarget target,
        string eventType,
        string message,
        Dictionary<string, object>? details,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload = FormatPayload(target.Provider, eventType, message, details);
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(target.Url, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Webhook notification to {TargetName} failed: {StatusCode}",
                    target.Name, response.StatusCode);
            }
            else
            {
                _logger.LogDebug("Webhook notification sent to {TargetName}", target.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send webhook notification to {TargetName}", target.Name);
        }
    }

    private static string FormatPayload(
        WebhookProvider provider,
        string eventType,
        string message,
        Dictionary<string, object>? details)
    {
        return provider switch
        {
            WebhookProvider.Slack => FormatSlackPayload(eventType, message, details),
            WebhookProvider.MicrosoftTeams => FormatTeamsPayload(eventType, message, details),
            WebhookProvider.Discord => FormatDiscordPayload(eventType, message, details),
            _ => FormatGenericPayload(eventType, message, details)
        };
    }

    private static string FormatSlackPayload(string eventType, string message, Dictionary<string, object>? details)
    {
        var emoji = GetEventEmoji(eventType);
        var color = GetEventColor(eventType);

        var payload = new
        {
            attachments = new[]
            {
                new
                {
                    color,
                    blocks = new object[]
                    {
                        new
                        {
                            type = "header",
                            text = new { type = "plain_text", text = $"{emoji} NIS2 Shield Alert", emoji = true }
                        },
                        new
                        {
                            type = "section",
                            text = new { type = "mrkdwn", text = $"*Event:* `{eventType}`\n*Message:* {message}" }
                        },
                        new
                        {
                            type = "context",
                            elements = new[]
                            {
                                new { type = "mrkdwn", text = $"⏰ {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC" }
                            }
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(payload);
    }

    private static string FormatTeamsPayload(string eventType, string message, Dictionary<string, object>? details)
    {
        var emoji = GetEventEmoji(eventType);
        var color = GetEventColor(eventType);

        var payload = new
        {
            @type = "MessageCard",
            themeColor = color.TrimStart('#'),
            summary = $"NIS2 Shield Alert: {eventType}",
            sections = new[]
            {
                new
                {
                    activityTitle = $"{emoji} NIS2 Shield Alert",
                    facts = new[]
                    {
                        new { name = "Event", value = eventType },
                        new { name = "Message", value = message },
                        new { name = "Timestamp", value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC" }
                    },
                    markdown = true
                }
            }
        };

        return JsonSerializer.Serialize(payload);
    }

    private static string FormatDiscordPayload(string eventType, string message, Dictionary<string, object>? details)
    {
        var emoji = GetEventEmoji(eventType);
        var colorInt = GetEventColorInt(eventType);

        var payload = new
        {
            embeds = new[]
            {
                new
                {
                    title = $"{emoji} NIS2 Shield Alert",
                    color = colorInt,
                    fields = new[]
                    {
                        new { name = "Event", value = $"`{eventType}`", inline = true },
                        new { name = "Message", value = message, inline = false }
                    },
                    footer = new { text = $"NIS2 Shield • {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC" }
                }
            }
        };

        return JsonSerializer.Serialize(payload);
    }

    private static string FormatGenericPayload(string eventType, string message, Dictionary<string, object>? details)
    {
        var payload = new
        {
            event_type = eventType,
            message,
            timestamp = DateTime.UtcNow,
            source = "nis2-shield",
            details
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });
    }

    private static string GetEventEmoji(string eventType) => eventType switch
    {
        "rate_limit_exceeded" => "🚦",
        "tor_node_blocked" => "🧅",
        "session_hijack_detected" => "🔓",
        "mfa_required" => "🔐",
        _ => "🛡️"
    };

    private static string GetEventColor(string eventType) => eventType switch
    {
        "rate_limit_exceeded" => "#FFA500", // Orange
        "tor_node_blocked" => "#FF0000",    // Red
        "session_hijack_detected" => "#DC143C", // Crimson
        "mfa_required" => "#FFD700",        // Gold
        _ => "#4169E1"                      // Royal Blue
    };

    private static int GetEventColorInt(string eventType) => eventType switch
    {
        "rate_limit_exceeded" => 16753920,  // Orange
        "tor_node_blocked" => 16711680,     // Red
        "session_hijack_detected" => 14423100, // Crimson
        "mfa_required" => 16766720,         // Gold
        _ => 4286945                        // Royal Blue
    };
}

using System.ComponentModel.DataAnnotations;

namespace Nis2Shield.AspNetCore.Configuration;

/// <summary>
/// Configuration options for NIS2 Shield Middleware.
/// Binds to "Nis2" section in appsettings.json.
/// </summary>
public class Nis2Options
{
    public const string SectionName = "Nis2";

    /// <summary>
    /// Master switch to enable/disable the shield.
    /// Default: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Key for HMAC-SHA256 log integrity signing.
    /// </summary>
    [Required]
    public string IntegrityKey { get; set; } = string.Empty;

    /// <summary>
    /// Base64 encoded AES key for PII encryption.
    /// </summary>
    public string EncryptionKey { get; set; } = string.Empty;

    public LoggingOptions Logging { get; set; } = new();
    public ActiveDefenseOptions ActiveDefense { get; set; } = new();
    public SessionGuardOptions SessionGuard { get; set; } = new();
    public SiemOptions Siem { get; set; } = new();
    public WebhookOptions Webhooks { get; set; } = new();
}

public class LoggingOptions
{
    public bool Enabled { get; set; } = true;
    public bool AnonymizeIp { get; set; } = true;
    public bool EncryptPii { get; set; } = true;
    public List<string> PiiFields { get; set; } = new() { "email", "user_id", "credit_card" };
}

public class ActiveDefenseOptions
{
    public bool RateLimitEnabled { get; set; } = true;
    public int RateLimitThreshold { get; set; } = 100;
    public int RateLimitWindowSeconds { get; set; } = 60;
    public bool BlockTorExitNodes { get; set; } = true;
}

public class SessionGuardOptions
{
    /// <summary>
    /// Enable session hijacking detection.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Subnet tolerance for IP changes (CIDR notation).
    /// Default: 24 means /24 subnet tolerance (last octet can change).
    /// </summary>
    public int SubnetTolerance { get; set; } = 24;

    /// <summary>
    /// Allow User-Agent changes within the same session.
    /// Set to false for stricter security.
    /// </summary>
    public bool AllowUserAgentChange { get; set; } = false;

    /// <summary>
    /// Session timeout in minutes for cleanup.
    /// </summary>
    public int SessionTimeoutMinutes { get; set; } = 30;
}

public class SiemOptions
{
    /// <summary>
    /// Enable SIEM integration.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// SIEM provider type.
    /// </summary>
    public SiemProvider Provider { get; set; } = SiemProvider.None;

    /// <summary>
    /// SIEM endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// API key or token for authentication.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Index name (for Elasticsearch) or source type (for Splunk).
    /// </summary>
    public string IndexName { get; set; } = "nis2-shield";

    /// <summary>
    /// Batch size for bulk operations.
    /// </summary>
    public int BatchSize { get; set; } = 100;
}

public enum SiemProvider
{
    None,
    Elasticsearch,
    Splunk,
    Datadog
}

public class WebhookOptions
{
    /// <summary>
    /// Enable webhook notifications.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// List of webhook targets.
    /// </summary>
    public List<WebhookTarget> Targets { get; set; } = new();
}

public class WebhookTarget
{
    /// <summary>
    /// Display name for the target.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Webhook URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Provider type for formatting.
    /// </summary>
    public WebhookProvider Provider { get; set; } = WebhookProvider.Generic;

    /// <summary>
    /// Events to send to this webhook.
    /// </summary>
    public List<string> Events { get; set; } = new() { "rate_limit_exceeded", "tor_node_blocked", "session_hijack_detected" };
}

public enum WebhookProvider
{
    Generic,
    Slack,
    MicrosoftTeams,
    Discord
}


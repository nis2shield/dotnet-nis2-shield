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

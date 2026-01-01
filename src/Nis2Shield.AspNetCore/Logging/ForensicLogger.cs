using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Nis2Shield.AspNetCore.Configuration;

namespace Nis2Shield.AspNetCore.Logging;

/// <summary>
/// Structured forensic log entry with HMAC-SHA256 integrity.
/// Compatible with Django/Spring NIS2 Shield log format.
/// </summary>
public class ForensicLogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Level { get; set; } = "INFO";
    public string Event { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public long DurationMs { get; set; }
    public Dictionary<string, object>? Extra { get; set; }

    /// <summary>
    /// HMAC-SHA256 signature of the log payload.
    /// </summary>
    public string? Hmac { get; set; }
}

/// <summary>
/// Service for creating signed, structured logs.
/// </summary>
public class ForensicLogger
{
    private readonly Nis2Options _options;
    private readonly byte[] _keyBytes;

    public ForensicLogger(IOptions<Nis2Options> options)
    {
        _options = options.Value;
        _keyBytes = Encoding.UTF8.GetBytes(_options.IntegrityKey);
    }

    /// <summary>
    /// Creates a log entry and signs it with HMAC-SHA256.
    /// </summary>
    public ForensicLogEntry CreateSignedEntry(
        string eventName,
        string path,
        string method,
        int statusCode,
        long durationMs,
        string? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        Dictionary<string, object>? extra = null)
    {
        var entry = new ForensicLogEntry
        {
            Event = eventName,
            Path = path,
            Method = method,
            StatusCode = statusCode,
            DurationMs = durationMs,
            UserId = _options.Logging.EncryptPii ? AnonymizeField(userId) : userId,
            IpAddress = _options.Logging.AnonymizeIp ? AnonymizeIp(ipAddress) : ipAddress,
            UserAgent = userAgent,
            Extra = extra
        };

        // Sign the entry (exclude the Hmac field itself)
        entry.Hmac = ComputeHmac(entry);

        return entry;
    }

    /// <summary>
    /// Serializes entry to JSON string.
    /// </summary>
    public string Serialize(ForensicLogEntry entry)
    {
        return JsonSerializer.Serialize(entry, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        });
    }

    private string ComputeHmac(ForensicLogEntry entry)
    {
        // Create a copy without Hmac for signing
        var payload = JsonSerializer.Serialize(new
        {
            entry.Timestamp,
            entry.Level,
            entry.Event,
            entry.Path,
            entry.Method,
            entry.StatusCode,
            entry.UserId,
            entry.IpAddress,
            entry.UserAgent,
            entry.DurationMs,
            entry.Extra
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

        using var hmac = new HMACSHA256(_keyBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static string? AnonymizeIp(string? ip)
    {
        if (string.IsNullOrEmpty(ip)) return ip;
        var parts = ip.Split('.');
        if (parts.Length == 4)
        {
            return $"{parts[0]}.{parts[1]}.{parts[2]}.0";
        }
        return ip;
    }

    private static string? AnonymizeField(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return $"***{value.Length}";
    }
}

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Nis2Shield.AspNetCore.Configuration;

namespace Nis2Shield.AspNetCore.Logging;

/// <summary>
/// Structured forensic log entry with HMAC-SHA256 integrity.
/// Matches NIS2-JSON-SCHEMA v1.0.
/// </summary>
public class ForensicLogEntry
{
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
    public string Level { get; set; } = "INFO";
    public string Component { get; set; } = "NIS2-SHIELD-DOTNET";
    public string EventId { get; set; } = "HTTP_ACCESS";
    public RequestInfo Request { get; set; }
    public ResponseInfo Response { get; set; }
    public UserInfo? User { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public string? IntegrityHash { get; set; }
}

public class RequestInfo 
{
    public string Method { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}

public class ResponseInfo
{
    public int Status { get; set; }
    public long DurationMs { get; set; }
}

public class UserInfo
{
    public string? Id { get; set; }
    public string? Email { get; set; }
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
            EventId = eventName.ToUpper().Replace(" ", "_"), // Normalize event ID
            Request = new RequestInfo 
            {
                Method = method,
                Url = path,
                Ip = _options.Logging.AnonymizeIp ? AnonymizeIp(ipAddress) : (ipAddress ?? "unknown"),
                UserAgent = userAgent ?? "unknown"
            },
            Response = new ResponseInfo
            {
                Status = statusCode,
                DurationMs = durationMs
            },
            User = !string.IsNullOrEmpty(userId) ? new UserInfo 
            { 
                Id = _options.Logging.EncryptPii ? AnonymizeField(userId) : userId 
            } : null,
            Metadata = extra
        };
        
        // Map status code to level
        entry.Level = statusCode >= 500 ? "ERROR" : statusCode >= 400 ? "WARN" : "INFO";

        // Sign the entry (exclude the Hmac field itself)
        entry.IntegrityHash = ComputeHmac(entry);

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
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }

    private string ComputeHmac(ForensicLogEntry entry)
    {
        // Serialization for signing (must match schema structure exactly)
        var options = new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        
        // Serialize object without IntegrityHash (it is null at this point)
        var payload = JsonSerializer.Serialize(entry, options);

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

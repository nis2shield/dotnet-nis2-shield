using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nis2Shield.AspNetCore.Configuration;

namespace Nis2Shield.AspNetCore.ActiveDefense;

/// <summary>
/// Detects session hijacking by monitoring IP and User-Agent changes.
/// Uses fingerprinting to detect suspicious session behavior.
/// </summary>
public class SessionGuard
{
    private readonly Nis2Options _options;
    private readonly ILogger<SessionGuard> _logger;
    private readonly ConcurrentDictionary<string, SessionFingerprint> _sessions = new();

    public SessionGuard(IOptions<Nis2Options> options, ILogger<SessionGuard> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Validates the current request against the stored session fingerprint.
    /// Returns true if the session is valid, false if hijacking is suspected.
    /// </summary>
    public SessionValidationResult ValidateSession(HttpContext context)
    {
        if (!_options.SessionGuard.Enabled)
            return SessionValidationResult.Valid();

        var sessionId = GetSessionId(context);
        if (string.IsNullOrEmpty(sessionId))
            return SessionValidationResult.Valid(); // No session = nothing to protect

        var currentIp = GetClientIp(context);
        var currentUserAgent = context.Request.Headers.UserAgent.ToString();

        // Check if we have a stored fingerprint for this session
        if (_sessions.TryGetValue(sessionId, out var storedFingerprint))
        {
            // Validate IP subnet
            if (!IsIpInSameSubnet(currentIp, storedFingerprint.IpAddress, _options.SessionGuard.SubnetTolerance))
            {
                _logger.LogWarning(
                    "Session hijacking suspected: IP changed from {OldIp} to {NewIp} for session {SessionId}",
                    storedFingerprint.IpAddress, currentIp, sessionId);
                
                return SessionValidationResult.Suspicious("ip_changed", storedFingerprint.IpAddress, currentIp);
            }

            // Validate User-Agent if strict mode
            if (!_options.SessionGuard.AllowUserAgentChange && 
                !string.Equals(currentUserAgent, storedFingerprint.UserAgent, StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "Session hijacking suspected: User-Agent changed for session {SessionId}",
                    sessionId);
                
                return SessionValidationResult.Suspicious("user_agent_changed", storedFingerprint.UserAgent, currentUserAgent);
            }

            // Update last seen
            storedFingerprint.LastSeen = DateTime.UtcNow;
        }
        else
        {
            // First request with this session, store fingerprint
            _sessions[sessionId] = new SessionFingerprint
            {
                SessionId = sessionId,
                IpAddress = currentIp,
                UserAgent = currentUserAgent,
                CreatedAt = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow
            };
        }

        return SessionValidationResult.Valid();
    }

    /// <summary>
    /// Invalidates a session, removing it from tracking.
    /// </summary>
    public void InvalidateSession(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
        _logger.LogInformation("Session invalidated: {SessionId}", sessionId);
    }

    /// <summary>
    /// Cleans up expired sessions older than the specified timeout.
    /// </summary>
    public void CleanupExpiredSessions(TimeSpan sessionTimeout)
    {
        var cutoff = DateTime.UtcNow - sessionTimeout;
        var expiredSessions = _sessions
            .Where(kvp => kvp.Value.LastSeen < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var sessionId in expiredSessions)
        {
            _sessions.TryRemove(sessionId, out _);
        }

        if (expiredSessions.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired sessions", expiredSessions.Count);
        }
    }

    private static string? GetSessionId(HttpContext context)
    {
        // Try common session cookie names
        if (context.Request.Cookies.TryGetValue(".AspNetCore.Session", out var aspNetSession))
            return HashSessionId(aspNetSession);
        
        if (context.Request.Cookies.TryGetValue("ASP.NET_SessionId", out var aspSession))
            return HashSessionId(aspSession);

        // Check for Authorization header (JWT token)
        var auth = context.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(auth) && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return HashSessionId(auth[7..]); // Hash the token

        return null;
    }

    private static string HashSessionId(string sessionId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sessionId));
        return Convert.ToHexString(bytes)[..16]; // First 16 chars of hash
    }

    private static string GetClientIp(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static bool IsIpInSameSubnet(string ip1, string ip2, int subnetBits)
    {
        if (ip1 == ip2) return true;
        if (ip1 == "unknown" || ip2 == "unknown") return true; // Can't validate

        try
        {
            var parts1 = ip1.Split('.').Select(byte.Parse).ToArray();
            var parts2 = ip2.Split('.').Select(byte.Parse).ToArray();

            if (parts1.Length != 4 || parts2.Length != 4)
                return true; // IPv6 or invalid, skip validation

            // Calculate how many octets must match
            var fullOctets = subnetBits / 8;
            var remainingBits = subnetBits % 8;

            for (int i = 0; i < fullOctets && i < 4; i++)
            {
                if (parts1[i] != parts2[i])
                    return false;
            }

            if (remainingBits > 0 && fullOctets < 4)
            {
                var mask = (byte)(0xFF << (8 - remainingBits));
                if ((parts1[fullOctets] & mask) != (parts2[fullOctets] & mask))
                    return false;
            }

            return true;
        }
        catch
        {
            return true; // On error, allow
        }
    }

    private class SessionFingerprint
    {
        public required string SessionId { get; init; }
        public required string IpAddress { get; init; }
        public required string UserAgent { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime LastSeen { get; set; }
    }
}

/// <summary>
/// Result of session validation.
/// </summary>
public class SessionValidationResult
{
    public bool IsValid { get; private init; }
    public string? Reason { get; private init; }
    public string? OldValue { get; private init; }
    public string? NewValue { get; private init; }

    public static SessionValidationResult Valid() => new() { IsValid = true };
    
    public static SessionValidationResult Suspicious(string reason, string oldValue, string newValue) => new()
    {
        IsValid = false,
        Reason = reason,
        OldValue = oldValue,
        NewValue = newValue
    };
}

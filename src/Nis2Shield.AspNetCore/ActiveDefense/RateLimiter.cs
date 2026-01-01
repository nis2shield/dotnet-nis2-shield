using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nis2Shield.AspNetCore.Configuration;

namespace Nis2Shield.AspNetCore.ActiveDefense;

/// <summary>
/// In-memory sliding window rate limiter.
/// Limits requests per IP address within a configurable time window.
/// </summary>
public class RateLimiter
{
    private readonly Nis2Options _options;
    private readonly ILogger<RateLimiter> _logger;
    private readonly ConcurrentDictionary<string, SlidingWindow> _windows = new();

    public RateLimiter(IOptions<Nis2Options> options, ILogger<RateLimiter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Checks if the request should be allowed based on rate limiting rules.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>True if allowed, false if rate limited.</returns>
    public bool IsAllowed(HttpContext context)
    {
        if (!_options.ActiveDefense.RateLimitEnabled)
            return true;

        var clientIp = GetClientIp(context);
        var window = _windows.GetOrAdd(clientIp, _ => new SlidingWindow(
            _options.ActiveDefense.RateLimitThreshold,
            TimeSpan.FromSeconds(_options.ActiveDefense.RateLimitWindowSeconds)
        ));

        var allowed = window.TryAcquire();
        
        if (!allowed)
        {
            _logger.LogWarning("Rate limit exceeded for IP: {ClientIp}", clientIp);
        }

        return allowed;
    }

    private static string GetClientIp(HttpContext context)
    {
        // Check for forwarded header first (behind proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private class SlidingWindow
    {
        private readonly int _limit;
        private readonly TimeSpan _windowSize;
        private readonly Queue<DateTime> _timestamps = new();
        private readonly object _lock = new();

        public SlidingWindow(int limit, TimeSpan windowSize)
        {
            _limit = limit;
            _windowSize = windowSize;
        }

        public bool TryAcquire()
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                var windowStart = now - _windowSize;

                // Remove expired timestamps
                while (_timestamps.Count > 0 && _timestamps.Peek() < windowStart)
                {
                    _timestamps.Dequeue();
                }

                if (_timestamps.Count >= _limit)
                {
                    return false;
                }

                _timestamps.Enqueue(now);
                return true;
            }
        }
    }
}

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nis2Shield.AspNetCore.ActiveDefense;
using Nis2Shield.AspNetCore.Configuration;
using Nis2Shield.AspNetCore.Logging;

namespace Nis2Shield.AspNetCore;

/// <summary>
/// Core middleware for NIS2 Shield.
/// Handles forensic logging, rate limiting, and active defense.
/// </summary>
public class Nis2Middleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<Nis2Middleware> _logger;
    private readonly Nis2Options _options;
    private readonly ForensicLogger _forensicLogger;
    private readonly RateLimiter _rateLimiter;
    private readonly TorBlocker _torBlocker;

    public Nis2Middleware(
        RequestDelegate next,
        ILogger<Nis2Middleware> logger,
        IOptions<Nis2Options> options,
        ForensicLogger forensicLogger,
        RateLimiter rateLimiter,
        TorBlocker torBlocker)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
        _forensicLogger = forensicLogger;
        _rateLimiter = rateLimiter;
        _torBlocker = torBlocker;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var clientIp = GetClientIp(context);

        // 1. Active Defense: Rate Limiting
        if (_options.ActiveDefense.RateLimitEnabled && !_rateLimiter.IsAllowed(context))
        {
            stopwatch.Stop();
            await HandleRateLimitExceeded(context, clientIp, stopwatch.ElapsedMilliseconds);
            return;
        }

        // 2. Active Defense: Tor Exit Node Blocking
        if (_options.ActiveDefense.BlockTorExitNodes && await _torBlocker.IsTorExitNodeAsync(clientIp))
        {
            stopwatch.Stop();
            await HandleTorBlocked(context, clientIp, stopwatch.ElapsedMilliseconds);
            return;
        }

        // 3. Execute Pipeline
        await _next(context);
        stopwatch.Stop();

        // 4. Forensic Logging
        if (_options.Logging.Enabled)
        {
            LogRequest(context, clientIp, stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task HandleRateLimitExceeded(HttpContext context, string clientIp, long durationMs)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.Headers["Retry-After"] = _options.ActiveDefense.RateLimitWindowSeconds.ToString();
        await context.Response.WriteAsJsonAsync(new { error = "Rate limit exceeded", retry_after = _options.ActiveDefense.RateLimitWindowSeconds });

        var entry = _forensicLogger.CreateSignedEntry(
            "rate_limit_exceeded",
            context.Request.Path,
            context.Request.Method,
            429,
            durationMs,
            context.User?.Identity?.Name,
            clientIp,
            context.Request.Headers.UserAgent.ToString());

        _logger.LogWarning("{LogEntry}", _forensicLogger.Serialize(entry));
    }

    private async Task HandleTorBlocked(HttpContext context, string clientIp, long durationMs)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new { error = "Access denied", reason = "tor_exit_node" });

        var entry = _forensicLogger.CreateSignedEntry(
            "tor_node_blocked",
            context.Request.Path,
            context.Request.Method,
            403,
            durationMs,
            context.User?.Identity?.Name,
            clientIp,
            context.Request.Headers.UserAgent.ToString());

        _logger.LogWarning("{LogEntry}", _forensicLogger.Serialize(entry));
    }

    private void LogRequest(HttpContext context, string clientIp, long durationMs)
    {
        var entry = _forensicLogger.CreateSignedEntry(
            "request_completed",
            context.Request.Path,
            context.Request.Method,
            context.Response.StatusCode,
            durationMs,
            context.User?.Identity?.Name,
            clientIp,
            context.Request.Headers.UserAgent.ToString());

        _logger.LogInformation("{LogEntry}", _forensicLogger.Serialize(entry));
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
}

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nis2Shield.AspNetCore.Configuration;

namespace Nis2Shield.AspNetCore;

/// <summary>
/// Core middleware for NIS2 Shield.
/// Handles logging, rate limiting, and active defense.
/// </summary>
public class Nis2Middleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<Nis2Middleware> _logger;
    private readonly Nis2Options _options;

    public Nis2Middleware(RequestDelegate next, ILogger<Nis2Middleware> logger, IOptions<Nis2Options> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        // 1. Active Defense (Placeholder for Step 2)
        // Check Rate Limit
        // Check Tor Exit Nodes

        // 2. Forensic Logging
        if (_options.Logging.Enabled)
        {
            // TODO: Capture Request/Response for HMAC signing
            // For MVP, we just passthrough with a log
            _logger.LogInformation("NIS2 Shield Protection Active for Request: {Path}", context.Request.Path);
        }

        await _next(context);
    }
}

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nis2Shield.AspNetCore.Configuration;
using Nis2Shield.AspNetCore.Logging;

namespace Nis2Shield.AspNetCore.Siem;

/// <summary>
/// Datadog SIEM connector using the Logs API.
/// </summary>
public class DatadogConnector : ISiemConnector
{
    private readonly HttpClient _httpClient;
    private readonly SiemOptions _options;
    private readonly ILogger<DatadogConnector> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public DatadogConnector(
        HttpClient httpClient,
        IOptions<Nis2Options> options,
        ILogger<DatadogConnector> logger)
    {
        _httpClient = httpClient;
        _options = options.Value.Siem;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        // Default to US endpoint if not specified
        var endpoint = string.IsNullOrEmpty(_options.Endpoint) 
            ? "https://http-intake.logs.datadoghq.com" 
            : _options.Endpoint;
        
        _httpClient.BaseAddress = new Uri(endpoint.TrimEnd('/') + "/");
        
        if (!string.IsNullOrEmpty(_options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("DD-API-KEY", _options.ApiKey);
        }
    }

    public async Task SendAsync(ForensicLogEntry entry, CancellationToken cancellationToken = default)
    {
        await SendBatchAsync(new[] { entry }, cancellationToken);
    }

    public async Task SendBatchAsync(IEnumerable<ForensicLogEntry> entries, CancellationToken cancellationToken = default)
    {
        var ddLogs = entries.Select(entry => new
        {
            ddsource = "nis2-shield",
            ddtags = $"env:{_options.IndexName}",
            hostname = Environment.MachineName,
            service = "nis2-shield",
            message = JsonSerializer.Serialize(entry, _jsonOptions),
            status = entry.Level.ToLowerInvariant()
        }).ToList();

        var json = JsonSerializer.Serialize(ddLogs, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("api/v2/logs", content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Datadog Logs API request failed: {StatusCode} - {Error}", 
                    response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send logs to Datadog");
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Send a test log
            var testLog = new[]
            {
                new
                {
                    ddsource = "nis2-shield",
                    ddtags = $"env:{_options.IndexName}",
                    hostname = Environment.MachineName,
                    service = "nis2-shield",
                    message = "NIS2 Shield connection test",
                    status = "info"
                }
            };

            var json = JsonSerializer.Serialize(testLog, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/v2/logs", content, cancellationToken);
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Datadog connection test failed");
            return false;
        }
    }
}

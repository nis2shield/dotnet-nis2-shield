using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nis2Shield.AspNetCore.Configuration;
using Nis2Shield.AspNetCore.Logging;

namespace Nis2Shield.AspNetCore.Siem;

/// <summary>
/// Splunk SIEM connector using HTTP Event Collector (HEC).
/// </summary>
public class SplunkConnector : ISiemConnector
{
    private readonly HttpClient _httpClient;
    private readonly SiemOptions _options;
    private readonly ILogger<SplunkConnector> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public SplunkConnector(
        HttpClient httpClient,
        IOptions<Nis2Options> options,
        ILogger<SplunkConnector> logger)
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
        _httpClient.BaseAddress = new Uri(_options.Endpoint.TrimEnd('/') + "/");
        
        if (!string.IsNullOrEmpty(_options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Splunk", _options.ApiKey);
        }
    }

    public async Task SendAsync(ForensicLogEntry entry, CancellationToken cancellationToken = default)
    {
        var hecEvent = new
        {
            time = new DateTimeOffset(DateTime.Parse(entry.Timestamp)).ToUnixTimeSeconds(),
            sourcetype = _options.IndexName,
            source = "nis2-shield",
            @event = entry
        };

        var json = JsonSerializer.Serialize(hecEvent, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("services/collector/event", content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Splunk HEC request failed: {StatusCode} - {Error}", 
                    response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send log to Splunk");
        }
    }

    public async Task SendBatchAsync(IEnumerable<ForensicLogEntry> entries, CancellationToken cancellationToken = default)
    {
        // Splunk HEC accepts multiple events in a single request (newline-separated JSON)
        var bulkRequest = new StringBuilder();

        foreach (var entry in entries)
        {
            var hecEvent = new
            {
                time = new DateTimeOffset(DateTime.Parse(entry.Timestamp)).ToUnixTimeSeconds(),
                sourcetype = _options.IndexName,
                source = "nis2-shield",
                @event = entry
            };
            bulkRequest.AppendLine(JsonSerializer.Serialize(hecEvent, _jsonOptions));
        }

        var content = new StringContent(bulkRequest.ToString(), Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("services/collector/event", content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Splunk HEC batch request failed: {StatusCode} - {Error}", 
                    response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send batch logs to Splunk");
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Send a test event
            var testEvent = new
            {
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                sourcetype = _options.IndexName,
                source = "nis2-shield",
                @event = new { test = true, message = "NIS2 Shield connection test" }
            };

            var json = JsonSerializer.Serialize(testEvent, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("services/collector/event", content, cancellationToken);
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Splunk connection test failed");
            return false;
        }
    }
}

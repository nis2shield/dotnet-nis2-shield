using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nis2Shield.AspNetCore.Configuration;
using Nis2Shield.AspNetCore.Logging;

namespace Nis2Shield.AspNetCore.Siem;

/// <summary>
/// Elasticsearch SIEM connector using the Bulk API.
/// </summary>
public class ElasticsearchConnector : ISiemConnector
{
    private readonly HttpClient _httpClient;
    private readonly SiemOptions _options;
    private readonly ILogger<ElasticsearchConnector> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ElasticsearchConnector(
        HttpClient httpClient,
        IOptions<Nis2Options> options,
        ILogger<ElasticsearchConnector> logger)
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
                new System.Net.Http.Headers.AuthenticationHeaderValue("ApiKey", _options.ApiKey);
        }
    }

    public async Task SendAsync(ForensicLogEntry entry, CancellationToken cancellationToken = default)
    {
        await SendBatchAsync(new[] { entry }, cancellationToken);
    }

    public async Task SendBatchAsync(IEnumerable<ForensicLogEntry> entries, CancellationToken cancellationToken = default)
    {
        var bulkRequest = new StringBuilder();
        var indexName = $"{_options.IndexName}-{DateTime.UtcNow:yyyy.MM.dd}";

        foreach (var entry in entries)
        {
            // Bulk API format: action + newline + document + newline
            bulkRequest.AppendLine(JsonSerializer.Serialize(new { index = new { _index = indexName } }));
            bulkRequest.AppendLine(JsonSerializer.Serialize(entry, _jsonOptions));
        }

        var content = new StringContent(bulkRequest.ToString(), Encoding.UTF8, "application/x-ndjson");
        
        try
        {
            var response = await _httpClient.PostAsync("_bulk", content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Elasticsearch bulk request failed: {StatusCode} - {Error}", 
                    response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send logs to Elasticsearch");
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("_cluster/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Elasticsearch connection test failed");
            return false;
        }
    }
}

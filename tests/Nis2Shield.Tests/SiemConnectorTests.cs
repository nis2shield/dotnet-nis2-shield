using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Nis2Shield.AspNetCore.Configuration;
using Nis2Shield.AspNetCore.Logging;
using Nis2Shield.AspNetCore.Siem;
using Xunit;

namespace Nis2Shield.Tests;

public class SiemConnectorTests
{
    private ForensicLogEntry CreateSampleEntry()
    {
        return new ForensicLogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = "INFO",
            Event = "test_event",
            Path = "/api/test",
            Method = "GET",
            StatusCode = 200,
            UserId = "user123",
            IpAddress = "192.168.1.100",
            UserAgent = "Mozilla/5.0",
            DurationMs = 42,
            Hmac = "abc123"
        };
    }

    private (T connector, Mock<HttpMessageHandler> mockHandler) CreateConnector<T>(
        string endpoint,
        string apiKey,
        SiemProvider provider) where T : class, ISiemConnector
    {
        var options = Options.Create(new Nis2Options
        {
            Siem = new SiemOptions
            {
                Enabled = true,
                Provider = provider,
                Endpoint = endpoint,
                ApiKey = apiKey,
                IndexName = "nis2-shield-test",
                BatchSize = 100
            }
        });

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(mockHandler.Object);

        ISiemConnector connector = provider switch
        {
            SiemProvider.Elasticsearch => new ElasticsearchConnector(
                httpClient,
                options,
                Mock.Of<ILogger<ElasticsearchConnector>>()),
            SiemProvider.Splunk => new SplunkConnector(
                httpClient,
                options,
                Mock.Of<ILogger<SplunkConnector>>()),
            SiemProvider.Datadog => new DatadogConnector(
                httpClient,
                options,
                Mock.Of<ILogger<DatadogConnector>>()),
            _ => throw new ArgumentException("Unknown provider")
        };

        return ((T)connector, mockHandler);
    }

    #region Elasticsearch Tests

    [Fact]
    public async Task ElasticsearchConnector_SendAsync_ShouldPostToEndpoint()
    {
        // Arrange
        var (connector, mockHandler) = CreateConnector<ElasticsearchConnector>(
            "https://elasticsearch.example.com",
            "elastic-api-key",
            SiemProvider.Elasticsearch);
        var entry = CreateSampleEntry();

        // Act
        await connector.SendAsync(entry);

        // Assert
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri!.ToString().Contains("/_bulk")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ElasticsearchConnector_SendBatchAsync_ShouldSendMultipleEntries()
    {
        // Arrange
        var (connector, mockHandler) = CreateConnector<ElasticsearchConnector>(
            "https://elasticsearch.example.com",
            "elastic-api-key",
            SiemProvider.Elasticsearch);
        var entries = new[] { CreateSampleEntry(), CreateSampleEntry(), CreateSampleEntry() };

        // Act
        await connector.SendBatchAsync(entries);

        // Assert
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    #endregion

    #region Splunk Tests

    [Fact]
    public async Task SplunkConnector_SendAsync_ShouldPostToHecEndpoint()
    {
        // Arrange
        var (connector, mockHandler) = CreateConnector<SplunkConnector>(
            "https://splunk.example.com:8088",
            "splunk-hec-token",
            SiemProvider.Splunk);
        var entry = CreateSampleEntry();

        // Act
        await connector.SendAsync(entry);

        // Assert
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.Headers.Authorization != null &&
                req.Headers.Authorization.Scheme == "Splunk"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SplunkConnector_SendBatchAsync_ShouldSendNDJSON()
    {
        // Arrange
        var (connector, mockHandler) = CreateConnector<SplunkConnector>(
            "https://splunk.example.com:8088",
            "splunk-hec-token",
            SiemProvider.Splunk);
        var entries = new[] { CreateSampleEntry(), CreateSampleEntry() };

        // Act
        await connector.SendBatchAsync(entries);

        // Assert
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    #endregion

    #region Datadog Tests

    [Fact]
    public async Task DatadogConnector_SendAsync_ShouldPostToLogsApi()
    {
        // Arrange
        var (connector, mockHandler) = CreateConnector<DatadogConnector>(
            "https://http-intake.logs.datadoghq.com",
            "datadog-api-key",
            SiemProvider.Datadog);
        var entry = CreateSampleEntry();

        // Act
        await connector.SendAsync(entry);

        // Assert
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.Headers.Contains("DD-API-KEY")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task DatadogConnector_TestConnectionAsync_ShouldReturnTrue()
    {
        // Arrange
        var (connector, mockHandler) = CreateConnector<DatadogConnector>(
            "https://http-intake.logs.datadoghq.com",
            "datadog-api-key",
            SiemProvider.Datadog);

        // Act
        var result = await connector.TestConnectionAsync();

        // Assert
        Assert.True(result);
    }

    #endregion
}

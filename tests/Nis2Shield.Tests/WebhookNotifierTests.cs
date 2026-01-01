using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Nis2Shield.AspNetCore.Configuration;
using Nis2Shield.AspNetCore.Notifications;
using Xunit;

namespace Nis2Shield.Tests;

public class WebhookNotifierTests
{
    private (WebhookNotifier notifier, Mock<HttpMessageHandler> mockHandler) CreateNotifier(WebhookOptions? options = null)
    {
        var nis2Options = Options.Create(new Nis2Options
        {
            Webhooks = options ?? new WebhookOptions
            {
                Enabled = true,
                Targets = new List<WebhookTarget>
                {
                    new()
                    {
                        Name = "Test Slack",
                        Url = "https://hooks.slack.com/test",
                        Provider = WebhookProvider.Slack,
                        Events = new List<string> { "rate_limit_exceeded", "*" }
                    }
                }
            }
        });

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(mockHandler.Object);
        var logger = Mock.Of<ILogger<WebhookNotifier>>();

        return (new WebhookNotifier(httpClient, nis2Options, logger), mockHandler);
    }

    [Fact]
    public async Task NotifyAsync_WhenDisabled_ShouldNotSendRequest()
    {
        // Arrange
        var (notifier, mockHandler) = CreateNotifier(new WebhookOptions { Enabled = false });

        // Act
        await notifier.NotifyAsync("rate_limit_exceeded", "Test message");
        await Task.Delay(100); // Allow fire-and-forget to execute

        // Assert
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task NotifyAsync_WhenEnabled_ShouldSendRequest()
    {
        // Arrange
        var (notifier, mockHandler) = CreateNotifier();

        // Act
        await notifier.NotifyAsync("rate_limit_exceeded", "Rate limit exceeded for IP 192.168.1.1");
        await Task.Delay(500); // Allow fire-and-forget to execute

        // Assert
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.AtLeastOnce(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task NotifyAsync_WithWildcardEvent_ShouldMatchAllEvents()
    {
        // Arrange
        var options = new WebhookOptions
        {
            Enabled = true,
            Targets = new List<WebhookTarget>
            {
                new()
                {
                    Name = "Wildcard Target",
                    Url = "https://example.com/webhook",
                    Provider = WebhookProvider.Generic,
                    Events = new List<string> { "*" } // matches all events
                }
            }
        };
        var (notifier, mockHandler) = CreateNotifier(options);

        // Act - send a custom event
        await notifier.NotifyAsync("custom_security_event", "Something happened");
        await Task.Delay(500);

        // Assert
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.AtLeastOnce(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task NotifyAsync_EventNotInTargetList_ShouldNotSendRequest()
    {
        // Arrange
        var options = new WebhookOptions
        {
            Enabled = true,
            Targets = new List<WebhookTarget>
            {
                new()
                {
                    Name = "Specific Event Target",
                    Url = "https://example.com/webhook",
                    Provider = WebhookProvider.Slack,
                    Events = new List<string> { "rate_limit_exceeded" } // only this event
                }
            }
        };
        var (notifier, mockHandler) = CreateNotifier(options);

        // Act - send a different event
        await notifier.NotifyAsync("tor_node_blocked", "Tor blocked");
        await Task.Delay(100);

        // Assert - should not send because event doesn't match
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }
}

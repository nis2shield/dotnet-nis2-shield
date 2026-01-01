using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Nis2Shield.AspNetCore.ActiveDefense;
using Nis2Shield.AspNetCore.Configuration;
using Xunit;

namespace Nis2Shield.Tests;

public class SessionGuardTests
{
    private SessionGuard CreateSessionGuard(SessionGuardOptions? options = null)
    {
        var nis2Options = Options.Create(new Nis2Options
        {
            SessionGuard = options ?? new SessionGuardOptions
            {
                Enabled = true,
                SubnetTolerance = 24,
                AllowUserAgentChange = false,
                SessionTimeoutMinutes = 30
            }
        });
        var logger = Mock.Of<ILogger<SessionGuard>>();
        return new SessionGuard(nis2Options, logger);
    }

    private HttpContext CreateHttpContext(string ip, string userAgent, string? sessionCookie = null)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ip);
        context.Request.Headers["User-Agent"] = userAgent;
        
        if (sessionCookie != null)
        {
            context.Request.Headers["Cookie"] = $".AspNetCore.Session={sessionCookie}";
        }

        return context;
    }

    [Fact]
    public void ValidateSession_NoSession_ShouldReturnValid()
    {
        // Arrange
        var guard = CreateSessionGuard();
        var context = CreateHttpContext("192.168.1.100", "Mozilla/5.0");
        // No session cookie

        // Act
        var result = guard.ValidateSession(context);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateSession_WhenDisabled_ShouldAlwaysReturnValid()
    {
        // Arrange
        var guard = CreateSessionGuard(new SessionGuardOptions { Enabled = false });
        var context = CreateHttpContext("192.168.1.100", "Mozilla/5.0", "session-123");

        // Act
        var result = guard.ValidateSession(context);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CleanupExpiredSessions_ShouldNotThrow()
    {
        // Arrange
        var guard = CreateSessionGuard();

        // Act & Assert - should not throw
        guard.CleanupExpiredSessions(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void InvalidateSession_ShouldNotThrow()
    {
        // Arrange
        var guard = CreateSessionGuard();

        // Act & Assert - should not throw even for non-existent session
        guard.InvalidateSession("non-existent-session");
    }

    [Fact]
    public void SessionValidationResult_Valid_ShouldHaveCorrectProperties()
    {
        // Act
        var result = SessionValidationResult.Valid();

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void SessionValidationResult_Suspicious_ShouldHaveCorrectProperties()
    {
        // Act
        var result = SessionValidationResult.Suspicious("ip_changed", "192.168.1.1", "10.0.0.1");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("ip_changed", result.Reason);
        Assert.Equal("192.168.1.1", result.OldValue);
        Assert.Equal("10.0.0.1", result.NewValue);
    }
}

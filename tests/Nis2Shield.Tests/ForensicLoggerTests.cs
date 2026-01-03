using Microsoft.Extensions.Options;
using Nis2Shield.AspNetCore.Configuration;
using Nis2Shield.AspNetCore.Logging;
using Xunit;

namespace Nis2Shield.Tests;

public class ForensicLoggerTests
{
    [Fact]
    public void CreateSignedEntry_ShouldGenerateValidHmac()
    {
        // Arrange
        var options = Options.Create(new Nis2Options
        {
            IntegrityKey = "test-secret-key-12345",
            Logging = new LoggingOptions
            {
                Enabled = true,
                AnonymizeIp = true,
                EncryptPii = true
            }
        });
        var logger = new ForensicLogger(options);

        // Act
        var entry = logger.CreateSignedEntry(
            eventName: "request",
            path: "/api/users",
            method: "GET",
            statusCode: 200,
            durationMs: 42,
            userId: "user123",
            ipAddress: "192.168.1.100",
            userAgent: "Mozilla/5.0"
        );

        // Assert
        Assert.NotNull(entry.IntegrityHash);
        Assert.Equal(64, entry.IntegrityHash.Length); // SHA256 = 64 hex chars
        Assert.Equal("192.168.1.0", entry.Request.Ip); // Anonymized
        Assert.Equal("***7", entry.User?.Id); // PII masked
    }

    [Fact]
    public void Serialize_ShouldOutputSnakeCaseJson()
    {
        // Arrange
        var options = Options.Create(new Nis2Options
        {
            IntegrityKey = "test-secret",
            Logging = new LoggingOptions { AnonymizeIp = false, EncryptPii = false }
        });
        var logger = new ForensicLogger(options);
        var entry = logger.CreateSignedEntry(
            eventName: "test",
            path: "/test",
            method: "POST",
            statusCode: 201,
            durationMs: 10
        );

        // Act
        var json = logger.Serialize(entry);

        // Assert
        Assert.Contains("\"event_id\":\"TEST\"", json);
        Assert.Contains("\"status\":201", json);
        Assert.Contains("\"duration_ms\":10", json);
    }
}

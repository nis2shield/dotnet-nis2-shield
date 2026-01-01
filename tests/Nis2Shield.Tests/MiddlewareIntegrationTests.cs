using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nis2Shield.AspNetCore;
using Nis2Shield.AspNetCore.Configuration;
using Xunit;

namespace Nis2Shield.Tests;

public class MiddlewareIntegrationTests
{
    [Fact]
    public async Task Middleware_ShouldPassthrough_WhenEnabled()
    {
        // Arrange
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddNis2Shield(options =>
                        {
                            options.Enabled = true;
                            options.IntegrityKey = "test-key-12345";
                        });
                    })
                    .Configure(app =>
                    {
                        app.UseNis2Shield();
                        app.Run(async context =>
                        {
                            await context.Response.WriteAsync("Hello NIS2!");
                        });
                    });
            })
            .StartAsync();

        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Hello NIS2!", content);
    }

    [Fact]
    public async Task Middleware_ShouldPassthrough_WhenDisabled()
    {
        // Arrange
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddNis2Shield(options =>
                        {
                            options.Enabled = false;
                        });
                    })
                    .Configure(app =>
                    {
                        app.UseNis2Shield();
                        app.Run(async context =>
                        {
                            await context.Response.WriteAsync("Shield Disabled");
                        });
                    });
            })
            .StartAsync();

        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Shield Disabled", content);
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Nis2Shield.AspNetCore.Configuration;
using Nis2Shield.AspNetCore.Logging;
using Nis2Shield.AspNetCore.ActiveDefense;
using Nis2Shield.AspNetCore.Siem;
using Nis2Shield.AspNetCore.Notifications;

namespace Nis2Shield.AspNetCore;

/// <summary>
/// Extension methods for setting up NIS2 Shield in an <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers NIS2 Shield services in the DI container using settings from IConfiguration.
    /// This is the recommended way to configure NIS2 Shield.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration root to bind options from (usually the "Nis2" section).</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // In Program.cs
    /// var builder = WebApplication.CreateBuilder(args);
    /// 
    /// // register services settings from "Nis2" section in appsettings.json
    /// builder.Services.AddNis2Shield(builder.Configuration);
    /// 
    /// var app = builder.Build();
    /// 
    /// // Enable middleware
    /// app.UseNis2Shield();
    /// </code>
    /// </example>
    public static IServiceCollection AddNis2Shield(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Bind Options
        services.Configure<Nis2Options>(configuration.GetSection(Nis2Options.SectionName));

        // 2. Register Core Services
        RegisterCoreServices(services);

        // 3. Register SIEM Connector based on configuration
        var siemOptions = configuration.GetSection($"{Nis2Options.SectionName}:Siem").Get<SiemOptions>();
        if (siemOptions?.Enabled == true)
        {
            RegisterSiemConnector(services, siemOptions.Provider);
        }

        return services;
    }

    /// <summary>
    /// Registers NIS2 Shield services with delegate configuration.
    /// Use this for programmatic configuration or testing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Delegate to configure <see cref="Nis2Options"/>.</param>
    /// <example>
    /// <code>
    /// builder.Services.AddNis2Shield(options => 
    /// {
    ///     options.IntegrityKey = "secret-key";
    ///     options.Logging.Enabled = true;
    ///     options.Logging.AnonymizeIp = true;
    ///     
    ///     // Configure Rate Limiting
    ///     options.ActiveDefense.RateLimitEnabled = true;
    ///     options.ActiveDefense.RateLimitThreshold = 100;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddNis2Shield(this IServiceCollection services, Action<Nis2Options> configureOptions)
    {
        services.Configure(configureOptions);
        RegisterCoreServices(services);
        return services;
    }

    private static void RegisterCoreServices(IServiceCollection services)
    {
        // Forensic Logging
        services.AddSingleton<ForensicLogger>();

        // Active Defense
        services.AddSingleton<RateLimiter>();
        services.AddSingleton<TorBlocker>();
        services.AddSingleton<SessionGuard>();

        // Notifications
        services.AddHttpClient<IWebhookNotifier, WebhookNotifier>();

        // Middleware
        services.AddTransient<Nis2Middleware>();
    }

    private static void RegisterSiemConnector(IServiceCollection services, SiemProvider provider)
    {
        switch (provider)
        {
            case SiemProvider.Elasticsearch:
                services.AddHttpClient<ISiemConnector, ElasticsearchConnector>();
                break;
            case SiemProvider.Splunk:
                services.AddHttpClient<ISiemConnector, SplunkConnector>();
                break;
            case SiemProvider.Datadog:
                services.AddHttpClient<ISiemConnector, DatadogConnector>();
                break;
        }
    }
}



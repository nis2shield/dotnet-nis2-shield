using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Nis2Shield.AspNetCore.Configuration;
using Nis2Shield.AspNetCore.Logging;
using Nis2Shield.AspNetCore.ActiveDefense;

namespace Nis2Shield.AspNetCore;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers NIS2 Shield services in the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration root to bind options from.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNis2Shield(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Bind Options
        services.Configure<Nis2Options>(configuration.GetSection(Nis2Options.SectionName));

        // 2. Register Core Services
        services.AddSingleton<ForensicLogger>();
        services.AddSingleton<RateLimiter>();
        services.AddSingleton<TorBlocker>();
        services.AddTransient<Nis2Middleware>();

        return services;
    }

    /// <summary>
    /// Registers NIS2 Shield services with delegate configuration.
    /// </summary>
    public static IServiceCollection AddNis2Shield(this IServiceCollection services, Action<Nis2Options> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddSingleton<ForensicLogger>();
        services.AddSingleton<RateLimiter>();
        services.AddSingleton<TorBlocker>();
        services.AddTransient<Nis2Middleware>();
        return services;
    }
}

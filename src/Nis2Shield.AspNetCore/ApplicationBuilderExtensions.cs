using Microsoft.AspNetCore.Builder;

namespace Nis2Shield.AspNetCore;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds NIS2 Shield Middleware to the pipeline.
    /// Should be called early in the pipeline, before Authentication.
    /// </summary>
    public static IApplicationBuilder UseNis2Shield(this IApplicationBuilder app)
    {
        return app.UseMiddleware<Nis2Middleware>();
    }
}

using Microsoft.AspNetCore.Cors.Infrastructure;

namespace TaskBoard.Api.Extensions;

internal static class FrontendCorsExtensions
{
    public const string PolicyName = "Frontend";

    private const string AllowedOriginsKey = "Cors:AllowedOrigins";

    /// <summary>
    /// Allows only the configured frontend origins (for example the Angular dev server) to call
    /// the API. No origins are allowed when none are configured.
    /// </summary>
    public static IServiceCollection AddFrontendCors(this IServiceCollection services)
    {
        services.AddCors();

        services.AddOptions<CorsOptions>()
            .Configure<IConfiguration>((options, configuration) =>
            {
                var origins = configuration.GetSection(AllowedOriginsKey).Get<string[]>() ?? [];

                options.AddPolicy(PolicyName, policy => policy
                    .WithOrigins(origins)
                    .WithMethods(
                        HttpMethods.Get,
                        HttpMethods.Post,
                        HttpMethods.Put,
                        HttpMethods.Patch,
                        HttpMethods.Delete)
                    .WithHeaders("Content-Type"));
            });

        return services;
    }
}

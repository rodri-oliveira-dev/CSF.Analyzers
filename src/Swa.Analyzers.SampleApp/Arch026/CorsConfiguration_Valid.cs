using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Swa.Analyzers.SampleApp.Arch026;

public static class CorsConfigurationValid
{
    public static void Configure(CorsPolicyBuilder policy)
    {
        policy.WithOrigins("https://app.example.com")
            .AllowCredentials();

        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    }
}

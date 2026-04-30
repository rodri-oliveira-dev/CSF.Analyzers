using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Swa.Analyzers.SampleApp.Arch026;

public static class CorsConfigurationInvalid
{
    public static void Configure(CorsPolicyBuilder policy)
    {
        // ARCH026: wildcard origins must not be combined with credentials.
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowCredentials();

        // ARCH026: the same unsafe combination is reported in the opposite order.
        policy.AllowCredentials()
            .AllowAnyOrigin();
    }
}

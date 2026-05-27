using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Swa.Analyzers.SampleApp.Arch026;

public static class CorsConfigurationInvalid
{
    public static void Configure(CorsPolicyBuilder policy)
    {
        // ARCH026: origens wildcard não devem ser combinadas com credenciais.
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowCredentials();

        // ARCH026: a mesma combinação insegura é reportada na ordem oposta.
        policy.AllowCredentials()
            .AllowAnyOrigin();
    }
}

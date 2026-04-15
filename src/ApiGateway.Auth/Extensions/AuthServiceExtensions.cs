using ApiGateway.Auth.Jwt;
using ApiGateway.Auth.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApiGateway.Auth.Extensions
{
    public static class AuthServiceExtensions
    {
        public static IServiceCollection AddGatewayAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;

            services
                .AddAuthentication("Bearer")
                .AddJwtBearer("Bearer", options =>
                {
                    options.Authority               = jwtOptions.Authority;
                    //options.Audience                = jwtOptions.Audience;
                    options.MetadataAddress         = jwtOptions.MetadataAddress;
                    options.RequireHttpsMetadata    = jwtOptions.RequireHttpsMetadata;

                    options.TokenValidationParameters = new()
                    {
                        ValidateIssuer              = true,
                        ValidIssuer                 = jwtOptions.ValidIssuer,
                        ValidateAudience            = false,
                        ValidateLifetime            = true,
                        ValidateIssuerSigningKey    = true,
                        ClockSkew                   = TimeSpan.FromSeconds(30)
                        /*
                        ValidateIssuer              = confere se o ISS do token bate com o authority configurado (no appsettingsd), preotegendo contra tokens de outros sistemas
                        ValidateAudience            = false,
                        ValidateLifetime            = confere o campo EXP, ent se o token expirou, é rejeitado. sem isso, tokens velhos iam funcionar pra sempre :O
                        ValidateIssuerSigningKey    = confere a assinatura com a chave pública do keycloak, garantindo autencididade do token!,
                        ClockSkew                   = TimeSpan.FromSeconds(30) 
                        */
                    };
                });

            services.AddAuthorization();

            return services;
        }

        public static IApplicationBuilder UseGatewayAuthentication(this IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<AuthenticationMiddleware>();

            return app;
        }
    }
}

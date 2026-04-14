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
                    options.Audience                = jwtOptions.Audience;
                    options.RequireHttpsMetadata    = jwtOptions.RequireHttpsMetadata;

                    options.TokenValidationParameters = new()
                    {
                        ValidateIssuer              = true,
                        ValidateAudience            = false,
                        ValidateLifetime            = true,
                        ValidateIssuerSigningKey    = true,
                        ClockSkew                   = TimeSpan.FromSeconds(30)
                    };
                });

            services.AddAuthorization();

            return services
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

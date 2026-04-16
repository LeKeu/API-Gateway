using ApiGateway.Auth.Authorization;
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

                    /*
                    tive que adicionar o metadata e o validissuer (tanto aqui, quanto nojwt options quanto no appsettings) pq:
                    ao fazer o teste 1 (de chamar o http://localhost:5000/api/produtos enviando access token, que deveria dar 200) dava 401. 
                    isso pq o postman roda no windos w acessa o localhost para buscar o token, ent o token vem com o iss de lá
                    só que, quando bate no gateway e faz a checagem desse iss, não funciona. pq, dentro do docker, o keycloak tá como http://keycloak:8080
                    então ele busca as chaves públicas como http://keycloak:8080, e não como o iss que chegou

                    aí essa modificação que eu fiz separou em duas responsabilidades:
                    "MetadataAddress": "http://keycloak:8080/..."  -> onde buscar as chaves (dentro do Docker)
                    "ValidIssuer":     "http://localhost:8080/..."  -> o que aceitar no iss do token (como o Postman vê)
                    */

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

            /*
            o YARP suporta vincular uma política de autorizaçãi diretamente a uma rota no appsetitngs
            o fluxo ficaria assim dessa forma:
            REquest com token -> auth valida a assinatura -> asp.net verifica política de rota -> YARP roteia

            tipo: SE o token é válido mas o usuário não tem a role exigida pela rota, retorna 403 (forbidden. 401 é não idnetificado)

            por isso aqui eu to setando os roles
            */

            services.AddAuthorization(options =>
            {
                options.AddPolicy(GatewayPolicies.Reader, policy =>
                    policy
                        .RequireAuthenticatedUser()
                        .RequireRole("reader", "admin"));

                options.AddPolicy(GatewayPolicies.Admin, policy =>
                    policy
                        .RequireAuthenticatedUser()
                        .RequireRole("admin"));

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

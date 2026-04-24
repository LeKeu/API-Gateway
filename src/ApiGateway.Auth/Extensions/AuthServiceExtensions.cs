using ApiGateway.Auth.Authorization;
using ApiGateway.Auth.Jwt;
using ApiGateway.Auth.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Text.Json;

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

                    /*
                    SITUAÇÃO DE ERRO QUE ENCONTREI!
                    mesmo um usuário com o role correto, retornava 403 no postman T-T
                    a situação é que o keycloak coloca os roles aninhados dentro de um campo realm_access.roles

                    quando o .net faz "requirerole("reader")", ele procura por um claim do tipo ClamTypes.Role e 
                    não sabe que precisa mergulhar dentro de realm_access pra pegar os roles
                    então o token pode até chegar válido, com um usuário com o role correto, mas o .net não consegue validar assim
                    
                    CORREÇÃO: mapear as roles do keycloak! eu digo pro jwtbearer extrair as roles de dentro de realm_access.roles
                    com o evento OnTokenValidate, que acontece logo depois da validação do token
                    ASSIM:
                    */

                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = context =>
                        {
                            var identity = context.Principal?.Identity as ClaimsIdentity;
                            if (identity is null) return Task.CompletedTask;

                            //aqui eu to pegando o claim que vem junto com a string json
                            var realmAccess = context.Principal?.FindFirst("realm_access")?.Value;

                            if(realmAccess is null) return Task.CompletedTask;

                            //to desserializzando o json e pegando os roles
                            var parsed = JsonDocument.Parse(realmAccess);
                            if(!parsed.RootElement.TryGetProperty("roles", out var rolesElement))
                                return Task.CompletedTask;

                            foreach (var role in rolesElement.EnumerateArray())
                            {
                                var roleValue = role.GetString();
                                if (roleValue is not null)
                                    identity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
                            }

                            return Task.CompletedTask;
                        }
                    };

                    /*
                    +- como funciona o fluxo:
                    - Ontokenvalidate roda -> adiciona claimtype roles = "reader"/"qlqr um" na identidade
                    userauthorizathion checa a política da rota, que fala "ah, eu require esses roles: ..."
                    asp.net vai procurar esses roles em claims na identidade
                    e vai encontrar pq a gente adicionou no início
                    aprova
                    segue pro yarp
                    */
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

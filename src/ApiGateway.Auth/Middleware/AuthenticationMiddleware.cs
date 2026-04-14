using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ApiGateway.Auth.Middleware
{
    public class AuthenticationMiddleware
    {
        // Request → [RateLimiting] → [Auth] → [Cache] → YARP → Serviço
        /*
        _next é o próximo middleware na fila. quando eu chamo await _next(context), 
        é como se eu tivesse falando"pasosu nessa verificação, pode seguir pro próximo"
        se eu não chamar o _next o request para aqui 
        ( no caso de token inválido, ele faz um 401 e não chama o next, ent o yarp nunca vê esse request)
        */

        private readonly RequestDelegate _next;
        private readonly ILogger<AuthenticationMiddleware> _logger;

        public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            /*
            quando eu registro um middleware (app.usemiddleware<...>()) o framework usa reflection 
            pra enocntrar um método chamado exatamente InvokeAsync ou Invoke, que receb um httpcontext
            dúvida trouxa -> e como o httpcontext chega aqui?
            bom, ele é criaod pelo asp.net no momento que chega uma requisição http, 
            e contém tudo sobre aquele request específico (headers, body, path, query string, e também a resposta qeu vou montar)
            aí o asp.net injeta ele no invoke/invokeasync que ele achou
            */
            var result = await context.AuthenticateAsync();

            if (!result.Succeeded)
            {
                _logger.LogWarning($"Request Rejeitado! Token inválido ou ausente. Path: {context.Request.Path}");

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                await context.Response.WriteAsJsonAsync(new
                {
                    error = "unauthorized",
                    message = "Token ausente ou inválido."
                });
                return;
            }

            var username = result.Principal?.FindFirst("preferred_username")?.Value;
            if (username is not null)
                context.Request.Headers["X-User"] = username;

            var roles = result.Principal?.FindAll(
                r => r.Type == ClaimTypes.Role)
                .Select(r => r.Value) ?? [];

            context.Request.Headers["X-Roles"] = string.Join(",", roles);

            await _next(context);
        }
    }
}

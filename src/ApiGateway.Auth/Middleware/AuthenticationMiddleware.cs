using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ApiGateway.Auth.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthenticationMiddleware> _logger;

        public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
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
                r => r.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                .Select(r => r.Value) ?? [];

            context.Request.Headers["X-Roles"] = string.Join(",", roles);

            await _next(context);
        }
    }
}

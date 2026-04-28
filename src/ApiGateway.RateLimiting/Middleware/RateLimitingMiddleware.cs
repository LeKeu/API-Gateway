using ApiGateway.Core.Abstractions;
using ApiGateway.RateLimiting.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiGateway.RateLimiting.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IRateLimiterStore _store;
        private readonly RateLimitOptions _options;
        private readonly ILogger<RateLimitingMiddleware> _logger;

        public RateLimitingMiddleware(RequestDelegate next, IRateLimiterStore store, IOptions<RateLimitOptions> options, ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _store = store;
            _options = options.Value;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            //aqui eu to pegando o ip e path (/produto, /pedido) do request par montar a chave
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var path = context.Request.Path.Value ?? "/";

            var key = $"ratelimit:{clientIp}:{path}";
            var window = TimeSpan.FromSeconds(_options.WindowSeconds);

            var count = await _store.IncrementAsync(key, window);

            //aqui eu to adicionando header informativo na resposta
            context.Response.Headers["X-RateLimit-Limit"] = _options.RequestsPerWindow.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, _options.RequestsPerWindow - count).ToString();

            if(count > _options.RequestsPerWindow)
            {
                _logger.LogWarning($"Rate limit excedido. Cliente: {clientIp}, Path: {path}, Count: {count}");

                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = _options.WindowSeconds.ToString();

                await context.Response.WriteAsJsonAsync(new
                {
                    error = "too_many_requests",
                    message = $"Limite de {_options.RequestsPerWindow} requests por {_options.WindowSeconds}s excedido.",
                    retryAfterSeconds = _options.WindowSeconds
                });
                return;
            }
        }
    }
}

using Microsoft.AspNetCore.Http;

namespace ApiGateway.RateLimiting.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
    }
}

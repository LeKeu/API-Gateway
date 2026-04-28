using ApiGateway.Core.Abstractions;
using ApiGateway.RateLimiting.Middleware;
using ApiGateway.RateLimiting.Options;
using ApiGateway.RateLimiting.Redis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace ApiGateway.RateLimiting.Extensions;

public static class RateLimitingServiceExtensions
{
    public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RateLimitOptions>(
            configuration.GetSection(RateLimitOptions.SectionName));

        var redisConnection = configuration.GetConnectionString("Redis")!;

        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnection));

        services.AddSingleton<IRateLimiterStore, RedisRateLimiterStore>();

        return services;
    }

    public static IApplicationBuilder UseGatewayRateLimiting(
        this IApplicationBuilder app)
    {
        app.UseMiddleware<RateLimitingMiddleware>();
        return app;
    }
}
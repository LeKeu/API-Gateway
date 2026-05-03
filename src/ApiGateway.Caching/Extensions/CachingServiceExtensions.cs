using ApiGateway.Caching.Middleware;
using ApiGateway.Caching.Options;
using ApiGateway.Caching.Redis;
using ApiGateway.Core.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace ApiGateway.Caching.Extensions
{
    public static class CachingServiceExtensions
    {
        public static IServiceCollection AddGatewayCaching(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

            /*
            aqui, como eu expliquei em outra classe, eu vou tá reutilizando o IConnectionMultiplexer
            e se, por algum motivo, ainda não tiver injetado, eu injeto ele
            */
            if(!services.Any(x => x.ServiceType == typeof(IConnectionMultiplexer)))
            {
                var redisConn = configuration.GetConnectionString("Redis")!;
                services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConn));
            }

            services.AddSingleton<ICacheProvider, RedisCacheProvider>();

            return services;
        }

        public static IApplicationBuilder UseGatewayCaching(this IApplicationBuilder app)
        {
            app.UseMiddleware<ResponseCacheMiddleware>();
            return app;
        }
    }
}

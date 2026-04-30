
namespace ApiGateway.Caching.Options
{
    public class CacheOptions
    {
        public const string SectionName = "Cache";

        //as rotas que vou cachear e as ttls de cada
        //rotas q não estou no appsettings não vão ser cacheadass!
        public Dictionary<string, RouteCacheOptions> Routes { get; set; } = new();
    }

    public class RouteCacheOptions()
    {
        public int TtlSeconds { get; set; } = 60;
}

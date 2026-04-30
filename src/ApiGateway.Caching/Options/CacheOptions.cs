
namespace ApiGateway.Caching.Options
{
    public class CacheOptions
    {
        public const string SectionName = "Cache";

        //as rotas que vou cachear e as ttls de cada
        public Dictionary<string, RouteCacheOptions> Routes { get; set; } = new();
    }

    public class RouteCacheOptions()
    {
        public int TtlSeconds { get; set; } = 60;
}

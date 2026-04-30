using ApiGateway.Core.Abstractions;
using StackExchange.Redis;

namespace ApiGateway.Caching.Redis
{
    public class RedisCacheProvider : ICacheProvider
    {
        private readonly IDatabase _db;

        public RedisCacheProvider(IConnectionMultiplexer redis)// não to criando uma segunda ocnexão (pq tbm uso no rate limiying), e sim reusando já que tá registradfo como singleton
        {
            _db = redis.GetDatabase();
        }

        public async Task<string?> GetAsync(string key)
        {
            var value = await _db.StringGetAsync(key);

            //o rediso tem uma conversão implícita para string? (afirmação ein)
            //ent ele vai retornar null se a chave não existe, que seria o cache miss!
            return value.HasValue ? value.ToString() : null;
        }

        public async Task SetAsync(string key, string value, TimeSpan ttl)
        {
            //aqui eu to setando o cache q que quero, com o valor na chave específica pra ser apagado em ttl segundos
            await _db.StringSetAsync(key, value, ttl);
        }
    }
}

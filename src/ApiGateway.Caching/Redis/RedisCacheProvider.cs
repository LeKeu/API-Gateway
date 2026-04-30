using ApiGateway.Core.Abstractions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ApiGateway.Caching.Redis
{
    public class RedisCacheProvider : ICacheProvider
    {
        private readonly IDatabase _db;
        private readonly ILogger<RedisCacheProvider> _logger;

        public RedisCacheProvider(IConnectionMultiplexer redis, ILogger<RedisCacheProvider> logger)// não to criando uma segunda ocnexão (pq tbm uso no rate limiying), e sim reusando já que tá registradfo como singleton
        {
            _db = redis.GetDatabase();
            _logger = logger;
        }
        /*
        AGORA UM QUESTIONAMENTO QUE ME FEZ PENSAR!! 
        o IConnectionMultiplexer está injetado no projeto do ratelimit. esse projeto não tem referencia ao caching, e o caching não tem referencia dele
        então, pq não precisa injetar ele dnv aqui??
        pois pois caros amigos, cara dev do futurop, o projeto principal é o HOST!
        então a ID vive nele, que chama tanto o ratelimit quanto o caching.
        quando o ratelimit injeta o IConnectionMultiplexer como singleton, essa inj fica no container do HOST
        quando essa classe é instanciada e oede um IConnectionMultiplexer, o container já tem registrado
        os projetos ratelimiting e cahing tecnicamente não se conhecem diretamente mas ambos falam com o HOST, que é o central
        Se o caching fosse registrado antes do rateLimiting no program, o IConnectionMultiplexer ainda não estaria no container e a inj não daria certo 
        A ordem dos Add* no program importa!!
        */

        public async Task<string?> GetAsync(string key)
        {
            try
            {
                var value = await _db.StringGetAsync(key);

                //o rediso tem uma conversão implícita para string? (afirmação ein)
                //ent ele vai retornar null se a chave não existe, que seria o cache miss!
                return value.HasValue ? value.ToString() : null;
                /*
                OUTRO QUESTIONAMENTO! PQ HASVALUE E NÃO IS NULL??
                a var value é um RedisValue, que é uma struct (um tipo valor, não referência)
                structs em c# nunca são null por padrão, ent quando eu declaro redisvalue value, ela smepre vai existir na memória
                com algum valor, mesmo que o redis n tenha encontrado a chave
                ent, uma struct nunca pode ser null pq sempre tem um espaço na mempória!!
                */
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Cache indisponível ao ler a chave{key}. Seguindo sem chave.");
                return null;
            }

            
        }

        public async Task SetAsync(string key, string value, TimeSpan ttl)
        {
            try
            {
                //aqui eu to setando o cache q que quero, com o valor na chave específica pra ser apagado em ttl segundos
                await _db.StringSetAsync(key, value, ttl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Cache indisponível ao ler a chave{key}. Seguindo sem chave.");
            }
        }

        /*
        OUTRO QUESTIONAMENTO - O QUE ACONTECE SE O REDIS CAIR?? (feito antes do try catch!!)
        bom, depende tanto do que o código faz com o retorno quanto o que o retorno... retorna
        nesse caso, ele lança uma exceção que, sem tratamento, pode quebrar o gateway todo
        ent o correto seria aplicar esse cache aside mas com uma degradação graciosa: se o cache falhar, trata como cache miss e segue pro serviço normalmente
        o usuário ainda vai receber o que ele quer, só que vai ser sem o cache
        */
    }
}

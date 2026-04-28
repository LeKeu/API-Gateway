using ApiGateway.Core.Abstractions;
using StackExchange.Redis;

namespace ApiGateway.RateLimiting.Redis
{
    public class RedisRateLimiterStore : IRateLimiterStore
    {
        /*
        o IRateLimiterStore  é uma abstração que diz como armazenar e recuperar os ocntadores, sem dizer onde
        o RedisRateLimitStore é uma implementação concreta que usa redis. eu poderia ter outra implementação que faz tudo 
        pela mem[oria, sem precisar do redis.
        o meu middleware não sabe o que ele tá usando, só que ele chama increment async e recebe um número
        */

        private readonly IDatabase _db;

        /*
        o rate limit é um porteiro que conta quantas vexes um cliente bateu na porta, basicamente
        se bateu demais num intervalo de tempo, ele barra com 429 too many requests
        */
        public RedisRateLimiterStore(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
            /*
            o que é e como funciona o iconnectionmultiplier?
            o redis é um servidor externo, pra falar co ele eu preciso de uma ocnexão, que é essa interface
            "multiplexer" significa que ele usa uma única conexão TCP pra váqios operadores simultâneos,
            em vez de abrir e fechar uma conexão por request (por isso SINGLETON no DI dele)
            */
        }

        /*
        Cada KEY no redis representa um cliente numa janela de tempo. no primeiro request, o contador vai para 1 e o TTL é definido
        nas seguintes, só incrementa. quando o ttl expira, a chave some e a janela reinicia
        */
        public async Task<long> IncrementAsync(string key, TimeSpan window)
        {
            //aqui eu to incrementando o contador e definindo o ttl pela primeira vez
            var count = await _db.StringIncrementAsync(key);
            /*
            o que é o TTL? -> TIME TO LIVE!
            é o tempo de vida de uma chave no redis. quando ele chega em 0, o redis deleta a chave automaticamente, ent eu não preciso fazer nada

            Request 1 de 192.168.1.1 em /api/produtos
              → chave "ratelimit:192.168.1.1:/api/produtos" não existe no Redis
              → StringIncrementAsync cria a chave com valor 1
              → count == 1 → define TTL de 60 segundos
              → retorna 1 → abaixo do limite → deixa passar

            Requests 2 a 10
              → chave existe → só incrementa
              → retorna 2, 3, 4... → abaixo do limite → deixa passar

            Request 11
              → chave existe → incrementa → retorna 11
              → 11 > 10 → retorna 429

            60 segundos depois do request 1
              → Redis deleta a chave automaticamente (TTL expirou)

            Request 12 (após expiração)
              → chave não existe mais → recomeça do 1
              → deixa passar
            */

            if (count == 1)
            {
                //só tá definindo a expiração no primeiro request da janela
                await _db.KeyExpireAsync(key, window);
            }

            return count;
        }
    }
}

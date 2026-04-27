namespace ApiGateway.Core.Abstractions
{
    public interface IRateLimitStore
    {
        /*
        aqui ele responde essa pergunta: quantas vezes esse cliente já bateu aqui nessa janela de tempo?
         key -> eu to identificando um cliente numa rota específica, como se fosse "ratelimit:192.168.1.1:/api/produtos". IP + path juntos
        (o mesmo ip pode fazer x requests em /produtos e mais x requests em /pedidos
        window -> vai tá definindo por quanto tempo aquele contador existe. se eu passar 60 segundos, ele vive por esse tempo e some. 
        quando ele some, o cliente pode começar tudo dnv
        */
        Task<long> IncrementAsync(string key, TimeSpan window);
    }
}

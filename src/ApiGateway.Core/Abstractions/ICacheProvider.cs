namespace ApiGateway.Core.Abstractions
{
    public interface ICacheProvider
    {//um pra ler, outro pra escrever
        Task<string?> GetAsync(string key);
        //tá como string? pq pode voltar nulo, que seria o cache miss
        Task SetAsync(string key, string value, TimeSpan ttl);
        // valor tá como string pq vou guardar em json
    }
}

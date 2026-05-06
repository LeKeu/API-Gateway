namespace ApiGateway.Core.Abstractions
{
    public interface ICacheProvider
    {//um pra ler, outro pra escrever
        Task<byte[]?> GetAsync(string key);
        //tá como string? pq pode voltar nulo, que seria o cache miss
        /*
        IMPORTANTE!! mudei de string? para byte[]? pois o retorno, quando havia cache hit, no get estava com caracteres especiais
        devido a COMPRESSION!! o postman tá enviando o header com accepy-encoding: gzip, deflate, br. o yarp repassa isso pro wiremock,
        que comprime a resposta. aí quando eu pego de volta, eu to pegando os bytes COMPRIMIDOS!! e como eu envio sem o header, o postman não sabe que
        ele precisa descomprimir e exibir os bytes cru
        */
        Task SetAsync(string key, byte[] value, TimeSpan ttl);
        // valor tá como string pq vou guardar em json
    }
}

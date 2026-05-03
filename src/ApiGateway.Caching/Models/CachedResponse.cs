namespace ApiGateway.Caching.Models
{
    public record CachedResponse(
        byte[] Body,
        string ContentType,
        string? ContentEncoding);
    /*
    TIVE QUE ADICIONAR ESSE RECORD
    pois estava com aquele problema de compression que eu já expliquei!!
    */
}

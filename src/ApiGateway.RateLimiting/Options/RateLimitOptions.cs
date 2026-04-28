
namespace ApiGateway.RateLimiting.Options
{
    public class RateLimitOptions
    {
        /*
        aqui eu to colocando as configurações do rate limit no appsettings
        em vez de ler strings do json espalhados pelo código, eu mapeio tudo pra essa classe aquii
        */
        public const string SectionName = "RateLimit";

        public int RequestsPerWindow { get; set; } = 10;
        public int WindowSeconds { get; set; } = 60;
    }
}

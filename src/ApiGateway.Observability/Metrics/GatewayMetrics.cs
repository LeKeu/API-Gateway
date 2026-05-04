using System.Diagnostics.Metrics;

namespace ApiGateway.Observability.Metrics
{
    public sealed class GatewayMetrics : IDisposable
    {
        public const string MeterName = "ApiGateway";
        private readonly Meter _meter;

        //contando o total de requests recebidos
        public readonly Counter<long> RequestsTotal;

        //medidno a duiração de cada request em milissegundos
        public readonly Histogram<double> RequestDuration;

        //contando quantas vezes o ratelimit foi excedido
        public readonly Counter<long> RateLimitsHits;

        //contando quantas vezes teve hits e misses do cache
        public readonly Counter<long> CacheHits;
        public readonly Counter<long> CacheMisses;

        //contando os erros de authenticação
        public readonly Counter<long> AuthFailures;

        public GatewayMetrics()
        {
            /*
            o counter só sobe, nunca desce!!
            o hoistigram distribui o valores em buckets. estudar mais sobre! 
            */
            _meter = new Meter(MeterName, "1.0.0");

            RequestsTotal = _meter.CreateCounter<long>(
            name: "gateway_requests_total",
            unit: "{requests}",
            description: "Total de requests recebidos pela gateway");

            RequestDuration = _meter.CreateHistogram<double>(
                name: "gateway_request_duration_ms",
                unit: "ms",
                description: "Duração dos requests em milissegundos");

            RateLimitsHits = _meter.CreateCounter<long>(
                name: "gateway_rate_limit_hits_total",
                unit: "{hits}",
                description: "Total de requests barrados por rate limiting");

            CacheHits = _meter.CreateCounter<long>(
                name: "gateway_cache_hits_total",
                unit: "{hits}",
                description: "Total de cache hits");

            CacheMisses = _meter.CreateCounter<long>(
                name: "gateway_cache_misses_total",
                unit: "{misses}",
                description: "Total de cache misses");

            AuthFailures = _meter.CreateCounter<long>(
                name: "gateway_auth_failures_total",
                unit: "{failures}",
                description: "Total de falhas de autenticação");
        }

        public void Dispose() => _meter.Dispose();

    }
}

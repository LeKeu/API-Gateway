using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace ApiGateway.Observability.Metrics
{
    public class MetricsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly GatewayMetrics _metrics;

        public MetricsMiddleware(RequestDelegate next, GatewayMetrics metrics)
        {
            _next = next;
            _metrics = metrics;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "/";
            var method = context.Request.Method;

            //iniuciando o cronometro para checagem da duração!
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await _next(context); // deixando o gateway rodar como deveria e depois retornar no finally
            }
            finally
            {
                stopwatch.Stop();

                var statusCode = context.Response.StatusCode.ToString();
                var duration = stopwatch.Elapsed.TotalMilliseconds;

                /*
                essas tags permitem filtrar no grafanna 
                */
                var tags = new TagList
                {
                    {"method", method },
                    {"path", path},
                    {"status_code", statusCode},
                };

                _metrics.RequestsTotal.Add(1, tags);
                _metrics.RequestDuration.Record(duration, tags);

                // auqi praqueles retornos específicos
                if (context.Response.StatusCode == 429)
                    _metrics.RateLimitsHits.Add(1, new TagList { { "path", path } });

                if (context.Response.StatusCode == 401)
                    _metrics.AuthFailures.Add(1, new TagList { { "path", path } });
            }
        }
    }
}

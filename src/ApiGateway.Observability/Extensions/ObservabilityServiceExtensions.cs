using ApiGateway.Observability.Metrics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace ApiGateway.Observability.Extensions
{
    public static class ObservabilityServiceExtensions
    {
        public static IServiceCollection AddGatewayObservability(this IServiceCollection services)
        {
            services.AddSingleton<GatewayMetrics>();

            services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics
                    .SetResourceBuilder(
                        ResourceBuilder.CreateDefault()
                            .AddService("ApiGateway"))

                    //instrumentação automática do ASP.NET Core, mede todas as requisições HTTP automaticamente
                    .AddAspNetCoreInstrumentation()

                    //instrumentação automática de chamadas HTTP de saída,ede as chamadas do YARP para o WireMock
                    .AddHttpClientInstrumentation()

                    //registra o Meter customizado da gateway
                    .AddMeter(GatewayMetrics.MeterName)

                    //exporta as métricas no formato Prometheus,disponível em GET /metrics
                    .AddPrometheusExporter();
                });

            return services;
        }

        public static IApplicationBuilder UseGatewayObservability(this IApplicationBuilder app)
        {
            app.UseMiddleware<MetricsMiddleware>();
            app.UseOpenTelemetryPrometheusScrapingEndpoint(); //~expondo o endpoint /metrics pro prometheurs raspr

            return app;
        }
    }
}

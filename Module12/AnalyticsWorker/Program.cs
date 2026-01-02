using AnalyticsWorker.Consumers;
using Confluent.Kafka.Extensions.OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        services.AddHostedService<AnalyticsConsumer>();

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName: "AnalyticsWorker"))
            .WithTracing(b => b
                .AddHttpClientInstrumentation()
                .AddConfluentKafkaInstrumentation()
                .AddZipkinExporter(o =>
                {
                    o.Endpoint = new Uri("http://zipkin:9411/api/v2/spans");
                }))
            .WithMetrics(m => m
                .AddHttpClientInstrumentation()
                .AddPrometheusExporter());
    })
    .Build();

await host.RunAsync();
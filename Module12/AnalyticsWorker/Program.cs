using AnalyticsWorker.Consumers;
using Confluent.Kafka.Extensions.OpenTelemetry;
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
                }));
    })
    .Build();

await host.RunAsync();
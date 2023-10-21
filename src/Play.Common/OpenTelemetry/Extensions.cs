using System;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Play.Common.MassTransit;
using Play.Common.Settings;

namespace Play.Common.OpenTelemetry;

public static class Extensions
{
    public static IServiceCollection AddTracing(this IServiceCollection services, IConfiguration config)
    {
        services.AddOpenTelemetry().WithTracing(builder =>
            {
                var serviceSettings = config.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();

                //this means that we want to collect information from a anything that's immediate directly by our microservice. In this case, this is the trading
                //microservice
                builder.AddSource(serviceSettings.ServiceName)

                // Because masstransit already has a bunch of in instrumentation build in that can tell give us information about messages that have been received.
                // The message have been sent, published, and a bunch of other information that's already been immediate by mass transit. We want to make sure that
                // we also collect that information. And for that we have to use identified for mass transit
                .AddSource("MassTransit")

                // define or give a name to the trading microservice as a resource within the long tail of traces that are going to be showing up and to do that.
                // We want to use the method called SetResourceBuilder.
                .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        // we identify our service in the long list of choices that are going to be showing up.
                        .AddService(serviceName: serviceSettings.ServiceName))

                //Add the additional instrumentation that we want to be collecting here. This allows us to track Http calls that come from our microservice to 
                // the outside, basically any Http requests that come from microservice to the outside.
                .AddHttpClientInstrumentation()

                // this allows us to track any inbound request into our controllers right into our microservice via APIs.
                .AddAspNetCoreInstrumentation()

                //where we want to export this tracing information. so that we're going to be using the console exporter right now.
                .AddConsoleExporter()

                //For Jaeger
                // .AddJaegerExporter(options =>
                // {
                //     var jaegerSettings = config.GetSection(nameof(JaegerSettings)).Get<JaegerSettings>();

                //     options.AgentHost = jaegerSettings.Host;
                //     options.AgentPort = jaegerSettings.Port;
                // });
                .AddOtlpExporter(options => {
                    var jaegerSettings = config.GetSection(nameof(JaegerSettings)).Get<JaegerSettings>();
                    options.Endpoint = new Uri($"http://{jaegerSettings.Host}:{jaegerSettings.Port}");
                });
            });
            
            // if consumer has some issues, it will report to Jaeger traces.
            services.AddConsumeObserver<ConsumeObserver>();

        return services;
    }

    public static IServiceCollection AddMetrics(this IServiceCollection services, IConfiguration config)
    {
        /*
            * add this for microservice to export the metrics into Prometheus, which is our tool or server that is going to be
            * collecting that information so that we can see later on in a very nice way.
            */
            services.AddOpenTelemetry().WithMetrics(builder => 
            {
                var settings = config.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();

                // the name if this Meter should be matched the name of the Meter you specify in PurchaseStateMachine, which is
                // "Meter meter = new(settings.ServiceName);" in constructor
                builder.AddMeter(settings.ServiceName)
                    .AddMeter("MassTransit")
                    //capture the metrics of HttpClient and and AspNetCore 
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    
                    //tell OpenTelemetry that we want to export these metrics into a Prometheus.
                    .AddPrometheusExporter();
            });

        return services;
    }
}
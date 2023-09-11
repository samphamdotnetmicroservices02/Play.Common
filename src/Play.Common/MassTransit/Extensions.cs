using System;
using System.Reflection;
using GreenPipes;
using GreenPipes.Configurators;
using MassTransit;
using MassTransit.Definition;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Play.Common.Settings;

namespace Play.Common.MassTransit;

public static class Extensions
{
    public static IServiceCollection AddMassTransitWithRabbitMq(
        this IServiceCollection services,
        Action<IRetryConfigurator> configureRetries = null)
    {
        services
            .AddMassTransit(configure =>
            {
                configure.AddConsumers(Assembly.GetEntryAssembly());
                configure.UsingPlayEconomyRabbitMq(configureRetries);
            });

        /*
        * this is the service that actually starts the RabbitMq bus, so that messages can be published to the different exchanges
        *   and queues in RabbitMq
        */
        services.AddMassTransitHostedService();

        return services;
    }

    public static void UsingPlayEconomyRabbitMq(
        this IServiceCollectionBusConfigurator configure,
        Action<IRetryConfigurator> configureRetries = null)
    {
        configure.UsingRabbitMq((context, configurator) =>
        {
            var configuration = context.GetService<IConfiguration>();
            ServiceSettings serviceSettings = configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
            var rabbitMqSettings = configuration.GetSection(nameof(RabbitMQSettings)).Get<RabbitMQSettings>();
            configurator.Host(rabbitMqSettings.Host);

            //this line is going to help us define or modify a little bit how queues are created in RabbitMq
            configurator.ConfigureEndpoints(context,
            new KebabCaseEndpointNameFormatter(
                serviceSettings.ServiceName, //prefix consumer that we are using queues
                false // will not include the full namespace of our classes in those queues
                ));

            if (configureRetries == null)
            {
                configureRetries = (retryConfigurator) =>
                {
                    /*
                    * anytime a message is not able to be consumed by consumer, it will be retried three times, and we'd have 5 seconds delay
                    */
                    retryConfigurator.Interval(3, TimeSpan.FromSeconds(5));
                };
            }

            configurator.UseMessageRetry(configureRetries);
        });
    }
}
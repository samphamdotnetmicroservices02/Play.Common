using System;
using System.Reflection;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Play.Common.Settings;

namespace Play.Common.MassTransit;

public static class Extensions
{
    private const string RabbitMq = "RABBITMQ";
    private const string ServiceBus = "SERVICEBUS";

    public static IServiceCollection AddMassTransitWithMessageBroker(
        this IServiceCollection services,
        IConfiguration config,
        Action<IRetryConfigurator> configureRetries = null)
    {
        ServiceSettings serviceSettings = config.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
        switch (serviceSettings.MessageBroker?.ToUpper())
        {
            case ServiceBus:
                services.AddMassTransitWithServiceBus(configureRetries);
                break;
            case RabbitMq:
            default:
                services.AddMassTransitWithRabbitMq(configureRetries);
                break;
        }

        return services;
    }

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

        return services;
    }

    public static IServiceCollection AddMassTransitWithServiceBus(
        this IServiceCollection services,
        Action<IRetryConfigurator> configureRetries = null)
    {
        services
            .AddMassTransit(configure =>
            {
                configure.AddConsumers(Assembly.GetEntryAssembly());
                configure.UsingPlayEconomyAzureServiceBus(configureRetries);
            });

        return services;
    }

    public static void UsingPlayEconomyMessageBroker(
        this IBusRegistrationConfigurator configure,
        IConfiguration config,
        Action<IRetryConfigurator> configureRetries = null)
    {
        ServiceSettings serviceSettings = config.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
        switch (serviceSettings.MessageBroker?.ToUpper())
        {
            case ServiceBus:
                configure.UsingPlayEconomyAzureServiceBus(configureRetries);
                break;
            case RabbitMq:
            default:
                configure.UsingPlayEconomyRabbitMq(configureRetries);
                break;
        }
    }

    public static void UsingPlayEconomyRabbitMq(
        this IBusRegistrationConfigurator configure,
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

    public static void UsingPlayEconomyAzureServiceBus(
        this IBusRegistrationConfigurator configure,
        Action<IRetryConfigurator> configureRetries = null)
    {
        configure.UsingAzureServiceBus((context, configurator) =>
        {
            var configuration = context.GetService<IConfiguration>();
            ServiceSettings serviceSettings = configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
            var serviceBusSettings = configuration.GetSection(nameof(ServiceBusSetting)).Get<ServiceBusSetting>();
            configurator.Host(serviceBusSettings.ConnectionString);

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
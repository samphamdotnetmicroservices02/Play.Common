using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using Play.Common.Settings;

namespace Play.Common.HealthChecks;

public static class Extension
{
    private const string MongoCheckName = "mongodb";
    private const string ReadyTagName = "ready";
    private const string LiveTagName = "live";
    private const string HealthEndpoint = "health";
    private const int DefaultSeconds = 3;

    public static IHealthChecksBuilder AddMongoDb(this IHealthChecksBuilder builder, TimeSpan? timeout = default)
    {
        return builder.Add(new HealthCheckRegistration(
                    //Add anyname you want
                    MongoCheckName,

                    // the second parameter that we're going to provide here is going to be what we call the factory that is going to
                    // create our model to be health check instance.
                    serviceProvider =>
                    {
                        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                        var mongoDbSettings = configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>();
                        var mongoClient = new MongoClient(mongoDbSettings.ConnectionString);
                        return new MongoDbHealthCheck(mongoClient);
                    },

                    //The next parameter is going to be the failure status. So this is going to be a status that should be reported if the
                    // healthcheck fails.
                    HealthStatus.Unhealthy,

                    //The next parameter we're going to identify here is what we call the tax. So this is a way for you to group a different
                    //set of health check registrations or health checks into specific groups that you want to report in different ways. So
                    //in our case, we're going to create a group that we're going to be calling it, the ready group. Because we're going to
                    //have both live test and readiness checks. And we'll talk about those in a moment. But for now, let's just assign the
                    // ready tag to this.
                    new[] { ReadyTagName },
                    // we can specify time out. So this is how much time this health check is going to wait before giving up. It's try to
                    // do its thing. And if it can't get a healthy status, it's going to just a timeout and produce unhealthy status.
                    TimeSpan.FromSeconds(DefaultSeconds)
                ));
    }

    public static void MapPlayEconomyHealthCheck(this IEndpointRouteBuilder endpoints)
    {
        //endpoints.MapHealthChecks("/health");
        
        //we also have to create appropriate routes so that our clients can access both our ready and are liveness health checks.
        //we defined a very simple health route. So now we're going to define two routes. Like I said, one route for the readiness check
        //and one route for the liveness check. The readiness check is going to be the one that's going to go ahead and make sure that
        //this microservice is actively ready to start serving requests. So even if they make a service is up and running, it doesn't mean 
        //that it is ready. It needs to make sure that, in this case. a database is ready to go and maybe some other services. While 
        //liveness test is just going to be for making sure that these microservice is alive
        endpoints.MapHealthChecks($"/{HealthEndpoint}/{ReadyTagName}", new HealthCheckOptions()
        {
            // this "ready" value comes from "ready" above you defined in .AddHealthChecks().Add(new HealthCheckRegistration...
            Predicate = (check) => check.Tags.Contains(ReadyTagName) 
        });

        //this is a liveness check, the liveness check is just verify if the service is alive or not
            endpoints.MapHealthChecks($"/{HealthEndpoint}/{LiveTagName}", new HealthCheckOptions()
        {
            // false means at this point, do not filter out every single health check registration that you have, and just let me know
            //if the service can respond to this. So that's the way to provide that predicate.
            Predicate = (check) => false
        });
    }
}
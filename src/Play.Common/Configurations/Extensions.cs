using System;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Play.Common.Settings;

namespace Play.Common.Configurations;

public static class Extensions
{
    public static IHostBuilder ConfigureAzureKeyVault(this IHostBuilder builder)
    {
        return builder.ConfigureAppConfiguration((context, configurationBuilder) =>
                {
                    if (context.HostingEnvironment.IsProduction())
                    {
                        var configuration = configurationBuilder.Build();
                        var serviceSetting = configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();


                        configurationBuilder.AddAzureKeyVault(
                            //Uri is from your key vault -> Vault URI
                            new Uri($"https://{serviceSetting.KeyVaultName}.vault.azure.net/"),
                            /*
                            * The idead of DefaultAzureCredential is that the identity Nuget package we just added will try to figure out what is the best way
                            * to accquire credentials or to use credentials for connecting to this key vault. So it will try a bunch of things depending on
                            * what's available in the current environment. Now in our case, what we want is for the microservice process to use the current
                            * identity that has been assigned to the port where our microservice is running. And this is where the Azure AD port managed 
                            * identities feature comes into place. Because we need to assign an identity that has access to the key vault so that when the
                            * microservies runs in a port in Kubernetes, it will have right away access to connect to the key vault. We'll talk more about 
                            * Azure AD port managed identities in the next lesson, but for now let's just create a brand new docker image that we will have
                            * now support for the key vault configuration service.
                            */
                            new DefaultAzureCredential()
                        );
                    }
                });
    }
}
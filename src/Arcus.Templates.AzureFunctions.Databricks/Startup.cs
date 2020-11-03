﻿using Arcus.Templates.AzureFunctions.Databricks;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Configuration;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Arcus.Templates.AzureFunctions.Databricks
{
    public class Startup : FunctionsStartup
    {
        // This method gets called by the runtime. Use this method to configure the app configuration.
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            builder.ConfigurationBuilder.AddEnvironmentVariables();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection
        public override void Configure(IFunctionsHostBuilder builder)
        {
            IConfiguration config = builder.GetContext().Configuration;

            builder.ConfigureSecretStore(stores =>
            {
//[#if DEBUG]
                stores.AddConfiguration(config);
//[#endif]

                //#error Please provide a valid secret provider, for example Azure Key Vault: https://security.arcus-azure.net/features/secrets/consume-from-key-vault
                stores.AddAzureKeyVaultWithManagedServiceIdentity("https://your-keyvault-vault.azure.net/");
            });

            var instrumentationKey = config.GetValue<string>("Arcus:ApplicationInsights:InstrumentationKey");
            var configuration = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.AzureApplicationInsights(instrumentationKey);

            builder.Services.AddLogging(logging => logging.AddSerilog(configuration.CreateLogger(), dispose: true));
        }
    }
}
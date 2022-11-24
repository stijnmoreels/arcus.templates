﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Arcus.Security.Core.Caching.Configuration;
using Arcus.Security.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
#if OpenApi && Isolated
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions; 
#endif
#if Serilog_AppInsights
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Extensions.Hosting;
#endif
 
namespace Arcus.Templates.AzureFunctions.Http
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
#if OpenApi
//[#if DEBUG]
            Environment.SetEnvironmentVariable("OpenApi__HideSwaggerUI", "false");
//[#else]
            Environment.SetEnvironmentVariable("OpenApi__HideSwaggerUI", "true");
//[#endif]
            
#endif
#if Serilog_AppInsights
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateBootstrapLogger();
            
            try
            {
                IHost host = CreateHostBuilder(args).Build();
                await ConfigureSerilogAsync(host);
                await host.RunAsync();
            }
            catch (Exception exception)
            {
                Log.Fatal(exception, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
#else
            IHost host = CreateHostBuilder(args).Build();
            await host.RunAsync();
#endif
        }
        
        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
#if Serilog_AppInsights
                       .UseSerilog(Log.Logger)
#endif
#if Isolated
                       .ConfigureFunctionsWorkerDefaults((context, builder) =>
                       {
#if IncludeHealthChecks
                            builder.Services.AddHealthChecks();
#endif
#if OpenApi
                            builder.Services.AddSingleton<IOpenApiConfigurationOptions, OpenApiConfigurationOptions>();
#endif
                            
                           builder.ConfigureJsonFormatting(options =>
                           {
                               options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                               options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                               options.Converters.Add(new JsonStringEnumConverter());
                           });
                            
                           builder.UseOnlyJsonFormatting()
                                  .UseFunctionContext()
                                  .UseHttpCorrelation()
                                  .UseRequestTracking(options =>
                                  {
                                      options.OmittedRoutes.Add("/");
#if IncludeHealthChecks
		                              options.OmittedRoutes.Add("/api/v1/health");
#endif
#if OpenApi
		                              options.OmittedRoutes.Add("/api/openapi");
                                      options.OmittedRoutes.Add("/api/swagger");
#endif
                                  })
                                  .UseExceptionHandling();
                       })
#if OpenApi
                       .ConfigureOpenApi()
#endif
#endif
                       .ConfigureSecretStore((config, stores) =>
                       {
//[#if DEBUG]
                           stores.AddConfiguration(config);
//[#endif]

                           //#error Please provide a valid secret provider, for example Azure Key Vault: https://security.arcus-azure.net/features/secret-store/provider/key-vault
                           stores.AddAzureKeyVaultWithManagedIdentity("https://your-keyvault.vault.azure.net/", CacheConfiguration.Default);
                       });
        }
#if Serilog_AppInsights
        
        private static async Task ConfigureSerilogAsync(IHost app)
        {
            var secretProvider = app.Services.GetRequiredService<ISecretProvider>();
            string connectionString = await secretProvider.GetRawSecretAsync("APPLICATIONINSIGHTS_CONNECTION_STRING");
            
            var reloadLogger = (ReloadableLogger) Log.Logger;
            reloadLogger.Reload(config =>
            {
               config.MinimumLevel.Information()
                     .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                     .Enrich.FromLogContext()
                     .Enrich.WithComponentName("Azure HTTP Trigger")
                     .Enrich.WithHttpCorrelationInfo(app.Services)
                     .Enrich.WithVersion()
                     .WriteTo.Console();
            
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    config.WriteTo.AzureApplicationInsightsWithConnectionString(connectionString);
                }
                
                return config;
            });
        }
#endif
    }
}

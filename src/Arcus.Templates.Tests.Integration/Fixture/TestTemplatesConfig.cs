using System;
using System.Collections.Generic;
using System.IO;
using Arcus.Templates.Tests.Integration.AzureFunctions.Configuration;
using Arcus.Templates.Tests.Integration.AzureFunctions.Http.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using GuardNet;
using Microsoft.Extensions.Logging;
using Arcus.Templates.Tests.Integration.Worker.EventHubs.Fixture;
using Arcus.Testing;

namespace Arcus.Templates.Tests.Integration.Fixture
{
    /// <summary>
    /// Configuration implementation with test values used in test cases to simulate scenario's.
    /// </summary>
    public class TestTemplatesConfig : Arcus.Testing.TestConfig
    {
        private readonly IConfigurationRoot _configuration;

        private TestTemplatesConfig(
            BuildConfiguration buildConfiguration,
            TargetFramework targetFramework)
            : base(options => options.AddOptionalJsonFile("appsettings.private.json"))
        {
            BuildConfiguration = buildConfiguration;
            TargetFramework = targetFramework;
        }

        /// <summary>
        /// Gets the build configuration for the project created from the template.
        /// </summary>
        public BuildConfiguration BuildConfiguration { get; }

        /// <summary>
        /// Gets the target framework for the project created from the template.
        /// </summary>
        public TargetFramework TargetFramework { get; }

        /// <summary>
        /// Creates a new <see cref="IConfigurationRoot"/> with test values.
        /// </summary>
        /// <param name="buildConfiguration">The configuration in which the created project from the template should be build.</param>
        /// <param name="targetFramework">The target framework in which the created project from the template should be build and run.</param>
        public static TestTemplatesConfig Create(
            BuildConfiguration buildConfiguration = BuildConfiguration.Debug,
            TargetFramework targetFramework = TargetFramework.Net8_0)
        {
           return new TestTemplatesConfig(buildConfiguration, targetFramework);
        }
    }

    public static class TestConfigExtensions
    {
         /// <summary>
        /// Gets the project directory of the web API project.
        /// </summary>
        public static DirectoryInfo GetWebApiProjectDirectory(this TestConfig configuration)
        {
            return PathCombineWithSourcesDirectory(configuration, "Arcus.Templates.WebApi");
        }

        /// <summary>
        /// Gets the project directory of the Service Bus project based on the given <paramref name="entityType"/>.
        /// </summary>
        public static DirectoryInfo GetServiceBusProjectDirectory(this TestConfig configuration, ServiceBusEntityType entityType)
        {
            switch (entityType)
            {
                case ServiceBusEntityType.Queue: return PathCombineWithSourcesDirectory(configuration, "Arcus.Templates.ServiceBus.Queue");
                case ServiceBusEntityType.Topic: return PathCombineWithSourcesDirectory(configuration, "Arcus.Templates.ServiceBus.Topic");
                default:
                    throw new ArgumentOutOfRangeException(nameof(entityType), entityType, "Unknown Service Bus entity");
            }
        }

        public static DirectoryInfo GetEventHubsProjectDirectory(this TestConfig configuration)
        {
            return PathCombineWithSourcesDirectory(configuration, "Arcus.Templates.EventHubs");
        }

        /// <summary>
        /// Gets the project directory of the Azure Functions Service Bus project based on the given <paramref name="entityType"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when no project directory can be found for the given <paramref name="entityType"/>.</exception>
        public static DirectoryInfo GetAzureFunctionsServiceBusProjectDirectory(this TestConfig configuration, ServiceBusEntityType entityType)
        {
            switch (entityType)
            {
                case ServiceBusEntityType.Queue: return PathCombineWithSourcesDirectory(configuration, "Arcus.Templates.AzureFunctions.ServiceBus.Queue");
                case ServiceBusEntityType.Topic: return PathCombineWithSourcesDirectory(configuration, "Arcus.Templates.AzureFunctions.ServiceBus.Topic");
                default:
                    throw new ArgumentOutOfRangeException(nameof(entityType), entityType, "Unknown Service Bus entity type");
            }
        }

        /// <summary>
        /// Gets the project directory of the Azure Functions EventHubs project template.
        /// </summary>
        public static DirectoryInfo GetAzureFunctionsEventHubsProjectDirectory(this TestConfig configuration)
        {
            return PathCombineWithSourcesDirectory(configuration, "Arcus.Templates.AzureFunctions.EventHubs");
        }

        /// <summary>
        /// Gets the project directory of the Azure Functions Databricks project template.
        /// </summary>
        public static DirectoryInfo GetAzureFunctionsHttpProjectDirectory(this TestConfig configuration)
        {
            return PathCombineWithSourcesDirectory(configuration, "Arcus.Templates.AzureFunctions.Http");
        }

        /// <summary>
        /// Gets the project directory where the fixtures are located.
        /// </summary>
        public static DirectoryInfo GetFixtureProjectDirectory(this TestConfig configuration)
        {
            return PathCombineWithSourcesDirectory(configuration, typeof(TestTemplatesConfig).Assembly.GetName().Name);
        }

        private static DirectoryInfo PathCombineWithSourcesDirectory(TestConfig configuration, string subPath)
        {
            DirectoryInfo sourcesDirectory = GetBuildSourcesDirectory(configuration);

            string path = Path.Combine(sourcesDirectory.FullName, "src", subPath);
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException(
                    $"Cannot find sub-directory in build sources directory at: {path}");
            }

            return new DirectoryInfo(path);
        }

        private static DirectoryInfo GetBuildSourcesDirectory(TestConfig configuration)
        {
            const string buildSourcesDirectory = "Build.SourcesDirectory";

            string sourcesDirectory = configuration.GetValue<string>(buildSourcesDirectory);
            Guard.NotNull(sourcesDirectory, nameof(sourcesDirectory), $"No build sources directory configured with the key: {buildSourcesDirectory}");
            Guard.For<ArgumentException>(
                () => !Directory.Exists(sourcesDirectory),
                $"No directory exists at {Path.GetFullPath(sourcesDirectory)}");

            return new DirectoryInfo(sourcesDirectory);
        }

        /// <summary>
        /// Gets the base URL of the to-be-created project from the web API template.
        /// </summary>
        /// <returns></returns>
        public static Uri GetDockerBaseUrl(this TestConfig configuration)
        {
            Uri baseUrl = GetBaseUrl(configuration);
            return baseUrl;
        }

        private static readonly Random RandomPort = new Random();

        /// <summary>
        /// Gets the base URL of the to-be-created project from the web API template.
        /// </summary>
        public static Uri GenerateRandomLocalhostUrl(this TestConfig configuration)
        {
            Uri baseUrl = GetBaseUrl(configuration);

            int port = RandomPort.Next(8080, 9000);
            return new Uri($"http://localhost:{port}{baseUrl.AbsolutePath}");
        }
        
        private static Uri GetBaseUrl(TestConfig configuration)
        {
            const string baseUrlKey = "Arcus:Api:BaseUrl";

            var baseUrl = configuration.GetValue<string>(baseUrlKey);
            Guard.NotNull(baseUrl, nameof(baseUrl), $"No base URL configured with the key: {baseUrlKey}");

            if (!Uri.TryCreate(baseUrl, UriKind.RelativeOrAbsolute, out Uri result))
            {
                throw new InvalidOperationException(
                    $"Cannot create valid URI from configured base URL with the key: {baseUrlKey}");
            }

            return result;
        }

        /// <summary>
        /// Generates a new TCP port for self-containing worker projects.
        /// </summary>
        public static int GenerateWorkerHealthPort(this TestConfig configuration)
        {
            return RandomPort.Next(8080, 9000);
        }

        /// <summary>
        /// Gets the TCP port on which the Service Bus Queue worker projects on docker run on.
        /// </summary>
        public static int GetDockerServiceBusQueueWorkerHealthPort(this TestConfig configuration)
        {
            const string tcpPortKey = "Arcus:Worker:ServiceBus:Queue:HealthPort";

            return configuration.GetValue<int>(tcpPortKey);
        }

        /// <summary>
        /// Gets the TCP port on which the Service Bus topic worker projects on docker run on.
        /// </summary>
        public static int GetDockerServiceBusTopicWorkerHealthPort(this TestConfig configuration)
        {
            const string tcpPortKey = "Arcus:Worker:ServiceBus:Topic:HealthPort";

            return configuration.GetValue<int>(tcpPortKey);
        }

        /// <summary>
        /// Gets the TCP port on which the Service Bus topic worker projects on docker run on.
        /// </summary>
        public static int GetDockerEventHubsWorkerHealthPort(this TestConfig configuration)
        {
            const string tcpPortKey = "Arcus:Worker:EventHubs:HealthPort";

            return configuration.GetValue<int>(tcpPortKey);
        }

        /// <summary>
        /// Gets the Azure Functions application configuration to create valid Azure Functions projects.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when one of the Azure Functions configuration values are not found.</exception>
        public static AzureFunctionsConfig GetAzureFunctionsConfig(this TestConfig configuration)
        {
            var storageAccountConnectionString = configuration.GetRequiredValue<string>("Arcus:AzureFunctions:AzureWebJobsStorage");

            return new AzureFunctionsConfig(storageAccountConnectionString);
        }

        /// <summary>
        /// Gets the application configuration to interact with the HTTP Azure Function.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when one of the HTTP Azure Function configuration values are not found.</exception>
        public static AzureFunctionHttpConfig GetAzureFunctionHttpConfig(this TestConfig configuration)
        {
            return new AzureFunctionHttpConfig(
                configuration.GetRequiredValue<int>("Arcus:AzureFunctions:Http:Isolated:HttpPort"),
                configuration.GetRequiredValue<int>("Arcus:AzureFunctions:Http:InProcess:HttpPort"));
        }
    }
}

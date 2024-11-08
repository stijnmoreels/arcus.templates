using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.AzureFunctions.Admin;
using Arcus.Templates.Tests.Integration.Configuration;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Worker.Configuration;
using Arcus.Templates.Tests.Integration.Worker.Fixture;
using Arcus.Templates.Tests.Integration.Worker.ServiceBus.Fixture;
using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using GuardNet;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.ServiceBus
{
    /// <summary>
    /// Project template to create new Azure Functions Service Bus projects.
    /// </summary>
    [DebuggerDisplay("Project = {ProjectDirectory.FullName}")]
    public class AzureFunctionsServiceBusProject : AzureFunctionsProject, IAsyncDisposable
    {
        private readonly string _entityName;

        private AzureFunctionsServiceBusProject(
            ServiceBusEntityType entityType, 
            string entityName,
            TestTemplatesConfig configuration, 
            AzureFunctionsServiceBusProjectOptions options,
            ITestOutputHelper outputWriter) 
            : base(configuration.GetAzureFunctionsServiceBusProjectDirectory(entityType), 
                   configuration, 
                   options,
                   outputWriter)
        {
            _entityName = entityName;
            Messaging = new TestServiceBusMessagePumpService(entityType, entityName, configuration, ProjectDirectory, outputWriter);
            Admin = new AdminEndpointService(RootEndpoint.Port, "order-processing", outputWriter);
        }

        /// <summary>
        /// Gets the service that interacts with the hosted-service message pump in the Azure Functions Service Bus project.
        /// </summary>
        /// <remarks>
        ///     Only when the project is started, is this service available for interaction.
        /// </remarks>
        public IMessagingService Messaging { get; }

        /// <summary>
        /// Gets the service to run administrative actions on the Azure Functions project.
        /// </summary>
        public AdminEndpointService Admin { get; }

        /// <summary>
        /// Starts a newly created project from the Azure Functions Service Bus project template.
        /// </summary>
        /// <returns>
        ///     An Azure Functions Service Bus project with a set of services to interact with the worker.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="outputWriter"/> is <c>null</c>.</exception>
        public static async Task<AzureFunctionsServiceBusProject> StartNewProjectAsync(
            ServiceBusEntityType entityType,
            string entityName,
            ITestOutputHelper outputWriter)
        {
            return await StartNewProjectAsync(entityType, entityName, new AzureFunctionsServiceBusProjectOptions(), TestTemplatesConfig.Create(), outputWriter);
        }

        /// <summary>
        /// Starts a newly created project from the Azure Functions Service Bus project template.
        /// </summary>
        /// <returns>
        ///     An Azure Functions Service Bus project with a set of services to interact with the worker.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="configuration"/>, or the <paramref name="outputWriter"/> is <c>null</c>.
        /// </exception>
        public static async Task<AzureFunctionsServiceBusProject> StartNewProjectAsync(
            ServiceBusEntityType entityType,
            string entityName,
            TestTemplatesConfig configuration,
            ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a configuration instance to retrieve the configuration values to pass along to the to-be-created project");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to write diagnostic information during the creation and startup process");

            return await StartNewProjectAsync(entityType, entityName, new AzureFunctionsServiceBusProjectOptions(), configuration, outputWriter);
        }

        /// <summary>
        /// Starts a newly created project from the Azure Functions Service Bus project template.
        /// </summary>
        /// <returns>
        ///     An Azure Functions Service Bus project with a set of services to interact with the project.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="options"/>, the <paramref name="configuration"/>, or the <paramref name="outputWriter"/> is <c>null</c>.
        /// </exception>
        public static async Task<AzureFunctionsServiceBusProject> StartNewProjectAsync(
            ServiceBusEntityType entityType,
            string entityName,
            AzureFunctionsServiceBusProjectOptions options,
            TestTemplatesConfig configuration,
            ITestOutputHelper outputWriter)
        {
            Guard.NotNull(options, nameof(options), "Requires a set of project options to pass along to the project creation command");
            Guard.NotNull(configuration, nameof(configuration), "Requires a configuration instance to retrieve the configuration values to pass along to the to-be-created project");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to write diagnostic information during the creation and startup process");

            AzureFunctionsServiceBusProject project = CreateNew(entityType, entityName, options, configuration, outputWriter);

            await project.StartAsync(entityType);
            return project;
        }

        /// <summary>
        /// Creates a new project from the Azure Functions Service Bus project template.
        /// </summary>
        /// <returns>
        ///     An Azure Functions Service Bus project with a set of services to interact with the project.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="options"/>, the <paramref name="configuration"/>, or the <paramref name="outputWriter"/> is <c>null</c>.
        /// </exception>
        public static AzureFunctionsServiceBusProject CreateNew(
            ServiceBusEntityType entityType, 
            string entityName,
            AzureFunctionsServiceBusProjectOptions options, 
            TestTemplatesConfig configuration, 
            ITestOutputHelper outputWriter)
        {
            Guard.NotNull(options, nameof(options), "Requires a set of project options to pass along to the project creation command");
            Guard.NotNull(configuration, nameof(configuration), "Requires a configuration instance to retrieve the configuration values to pass along to the to-be-created project");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to write diagnostic information during the creation process");

            var project = new AzureFunctionsServiceBusProject(entityType, entityName, configuration, options, outputWriter);
            project.CreateNewProject(options);
            project.AddActiveOrderMessageHandlerImplementation(entityType, entityName);
            project.AddLocalSettings();

            return project;
        }

        private void AddActiveOrderMessageHandlerImplementation(ServiceBusEntityType entityType, string entityName)
        {
            AddTypeAsFile<Order>();
            AddTypeAsFile<Customer>();
            AddTypeAsFile<OrderCreatedEventData>();

            AddTypeAsFile<WriteToFileMessageHandler>();
            UpdateFileInProject(RuntimeFileName, contents =>
                RemovesUserErrorsFromContents(contents)
                    .Replace("OrdersAzureServiceBusMessageHandler", nameof(WriteToFileMessageHandler)));

            if (entityType is ServiceBusEntityType.Queue)
            {
                UpdateFileInProject("OrderFunction.cs",
                    contents => contents.Replace(
                        "[ServiceBusTrigger(\"orders\"", 
                        $"[ServiceBusTrigger(\"{entityName}\""));
            }
            else if (entityType is ServiceBusEntityType.Topic)
            {
                UpdateFileInProject("OrderFunction.cs",
                    contents => contents.Replace(
                        "[ServiceBusTrigger(\"orders-topic\"",
                        $"[ServiceBusTrigger(\"{entityName}\""));
            }
        }

        private async Task StartAsync(ServiceBusEntityType entityType)
        {
            try
            {
                ServiceBusConfig serviceBus = Configuration.GetServiceBus();
                Environment.SetEnvironmentVariable("ServiceBusConnection__fullyQualifiedNamespace", serviceBus.FullyQualifiedNamespace);

                if (entityType is ServiceBusEntityType.Topic)
                {
                    await AddServiceBusTopicSubscriptionAsync(serviceBus);
                }

                AppInsightsConfig appInsights = Configuration.GetAppInsights();
                Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING", appInsights.ConnectionString);

                Run(Configuration.BuildConfiguration, TargetFramework.Net8_0);
                await Messaging.StartAsync();
                await WaitUntilTriggerIsAvailableAsync(Admin.Endpoint);
            }
            catch
            {
                await DisposeAsync();
                throw;
            }
        }

        private async Task AddServiceBusTopicSubscriptionAsync(ServiceBusConfig serviceBus)
        {
            var client = new ServiceBusAdministrationClient(serviceBus.FullyQualifiedNamespace, serviceBus.ServicePrincipal.GetCredential());
            var subscriptionName = "order-subscription";

            Response<bool> subscriptionExists = await client.SubscriptionExistsAsync(_entityName, subscriptionName);
            if (!subscriptionExists.Value)
            {
                await client.CreateSubscriptionAsync(_entityName, subscriptionName);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            var exceptions = new Collection<Exception>();

            try
            {
                Dispose();
            }
            catch (Exception exception)
            {
                exceptions.Add(exception);
            }

            try
            {
                await Messaging.DisposeAsync();
            }
            catch (Exception exception)
            {
                exceptions.Add(exception);
            }

            if (exceptions.Count is 1)
            {
                throw exceptions[0];
            }

            if (exceptions.Count > 1)
            {
                throw new AggregateException(exceptions);
            }
        }

        /// <summary>
        /// Performs additional application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">The flag indicating whether or not the additional tasks should be disposed.</param>
        protected override void Disposing(bool disposing)
        {
            base.Disposing(disposing);
            Environment.SetEnvironmentVariable("ServiceBusConnection__fullyQualifiedNamespace", null);
            Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", null);
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING", null);
        }
    }
}

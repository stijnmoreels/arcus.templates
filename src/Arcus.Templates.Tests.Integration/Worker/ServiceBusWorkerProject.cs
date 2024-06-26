﻿using System.Threading.Tasks;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Worker.Configuration;
using Arcus.Templates.Tests.Integration.Worker.ServiceBus.Fixture;
using GuardNet;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker
{
    /// <summary>
    /// Project template to create Azure ServiceBus Queue worker projects.
    /// </summary>
    public class ServiceBusWorkerProject : WorkerProject
    {
        private ServiceBusWorkerProject(
            ServiceBusEntityType entityType,
            TestConfig configuration,
            ITestOutputHelper outputWriter)
            : base(configuration.GetServiceBusProjectDirectory(entityType), 
                   configuration, 
                   outputWriter: outputWriter)
        {
            Messaging = new TestServiceBusMessagePumpService(entityType, configuration, ProjectDirectory, outputWriter);
        }

        /// <summary>
        /// Starts a newly created project from the ServiceBus Queue worker project template.
        /// </summary>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A ServiceBus Queue project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<ServiceBusWorkerProject> StartNewWithQueueAsync(ITestOutputHelper outputWriter)
        {
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to add telemetry information during the creation and startup process");

            var config = TestConfig.Create();
            var options = ServiceBusWorkerProjectOptions.Create(config);
            ServiceBusWorkerProject project = await StartNewWithQueueAsync(config, options, outputWriter);
            
            return project;
        }

        /// <summary>
        /// Starts a newly created project from the ServiceBus Queue worker project template.
        /// </summary>
        /// <param name="config">The collection of configuration values to correctly initialize the resulting project with secret values.</param>
        /// <param name="options">The project options to manipulate the resulting structure of the project.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A ServiceBus Queue project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<ServiceBusWorkerProject> StartNewWithQueueAsync(
            TestConfig config,
            ServiceBusWorkerProjectOptions options,
            ITestOutputHelper outputWriter)
        {
            Guard.NotNull(config, nameof(config), "Requires an integration test configuration to configure the resulting project from the Service Bus worker template");
            Guard.NotNull(options, nameof(options), "Requires a set of options to configure the resulting project from the Service Bus worker template");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to add telemetry information during the creation and startup process");

            ServiceBusWorkerProject project = await StartNewAsync(ServiceBusEntityType.Queue, config, options, outputWriter);
            return project;
        }

        /// <summary>
        /// Starts a newly created project from the ServiceBus Queue worker project template.
        /// </summary>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A ServiceBus Queue project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<ServiceBusWorkerProject> StartNewWithTopicAsync(ITestOutputHelper outputWriter)
        {
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to add telemetry information during the creation and startup process");

            var config = TestConfig.Create();
            var options = ServiceBusWorkerProjectOptions.Create(config);
            ServiceBusWorkerProject project = await StartNewWithTopicAsync(config, options, outputWriter);
            
            return project;
        }

        /// <summary>
        /// Starts a newly created project from the ServiceBus Queue worker project template.
        /// </summary>
        /// <param name="config">The collection of configuration values to correctly initialize the resulting project with secret values.</param>
        /// <param name="options">The project options to manipulate the resulting structure of the project.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A ServiceBus Queue project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<ServiceBusWorkerProject> StartNewWithTopicAsync(
            TestConfig config,
            ServiceBusWorkerProjectOptions options,
            ITestOutputHelper outputWriter)
        {
            Guard.NotNull(config, nameof(config), "Requires an integration test configuration to configure the resulting project from the Service Bus worker template");
            Guard.NotNull(options, nameof(options), "Requires a set of options to configure the resulting project from the Service Bus worker template");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to add telemetry information during the creation and startup process");

            ServiceBusWorkerProject project = await StartNewAsync(ServiceBusEntityType.Topic, config, options, outputWriter);
            return project;
        }

        /// <summary>
        /// Starts a newly created project from the ServiceBus Queue or Topic worker project template.
        /// </summary>
        /// <param name="entityType">The resource entity for which the worker template should be created, you can also use <see cref="StartNewWithQueueAsync(ITestOutputHelper)"/> or <see cref="StartNewWithTopicAsync(ITestOutputHelper)"/> instead.</param>
        /// <param name="configuration">The collection of configuration values to correctly initialize the resulting project with secret values.</param>
        /// <param name="options">The project options to manipulate the resulting structure of the project.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A ServiceBus project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<ServiceBusWorkerProject> StartNewAsync(
            ServiceBusEntityType entityType, 
            TestConfig configuration, 
            ServiceBusWorkerProjectOptions options, 
            ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires an integration test configuration to configure the resulting project from the Service Bus worker template");
            Guard.NotNull(options, nameof(options), "Requires a set of options to configure the resulting project from the Service Bus worker template");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to add telemetry information during the creation and startup process");

            ServiceBusWorkerProject project = CreateNew(entityType, configuration, options, outputWriter);

            string serviceBusConnection = configuration.GetServiceBusConnectionString(entityType);

            await project.StartAsync(options, CommandArgument.CreateSecret("ARCUS_SERVICEBUS_CONNECTIONSTRING", serviceBusConnection));

            return project;
        }

        /// <summary>
        /// Creates a new project from the ServiceBus worker project template.
        /// </summary>
        /// <param name="entityType">The resource entity for which the worker template should be created.</param>
        /// <param name="configuration">The collection of configuration values to correctly initialize the resulting project with secret values.</param>
        /// <param name="options">The project options to manipulate the resulting structure of the project.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation process.</param>
        /// <returns>
        ///     A ServiceBus project with a set of services to interact with the worker.
        /// </returns>
        public static ServiceBusWorkerProject CreateNew(
            ServiceBusEntityType entityType, 
            TestConfig configuration, 
            ServiceBusWorkerProjectOptions options,
            ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires an integration test configuration to configure the resulting project from the Service Bus worker template");
            Guard.NotNull(options, nameof(options), "Requires a set of options to configure the resulting project from the Service Bus worker template");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to add telemetry information during the creation process");

            var project = new ServiceBusWorkerProject(entityType, configuration, outputWriter);
            project.CreateNewProject(options);
            project.AddTestMessageHandler();

            if (entityType is ServiceBusEntityType.Topic)
            {
                project.AddAutomaticTopicSubscription();
            }

            return project;
        }

        private void AddAutomaticTopicSubscription()
        {
            UpdateFileInProject("Program.cs",
                contents => contents.Replace(
                    "AddServiceBusTopicMessagePump(\"Receive-All\", \"ARCUS_SERVICEBUS_CONNECTIONSTRING\")",
                    $"AddServiceBusTopicMessagePump(\"Receive-All\", \"ARCUS_SERVICEBUS_CONNECTIONSTRING\", opt => opt.TopicSubscription = {typeof(TopicSubscription).FullName}.{TopicSubscription.Automatic})"));
        }

        private void AddTestMessageHandler()
        {
            AddTypeAsFile<Order>();
            AddTypeAsFile<Customer>();
            AddTypeAsFile<OrderCreatedEventData>();
            AddTypeAsFile<WriteToFileMessageHandler>();
            
            UpdateFileInProject("Program.cs", contents => 
                RemovesUserErrorsFromContents(contents)
                    .Replace(".MinimumLevel.Debug()", ".MinimumLevel.Verbose()")
                    .Replace("EmptyMessageHandler", nameof(WriteToFileMessageHandler))
                    .Replace("EmptyMessage", nameof(Order))
                    .Replace("stores.AddAzureKeyVaultWithManagedIdentity(\"https://your-keyvault.vault.azure.net/\", CacheConfiguration.Default);", ""));
        }
    }
}

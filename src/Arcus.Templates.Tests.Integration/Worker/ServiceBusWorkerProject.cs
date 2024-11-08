using System.Threading.Tasks;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Templates.Tests.Integration.Configuration;
using Arcus.Templates.Tests.Integration.Fixture;
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
            string entityName,
            TestTemplatesConfig configuration,
            ITestOutputHelper outputWriter)
            : base(configuration.GetServiceBusProjectDirectory(entityType), 
                   configuration, 
                   outputWriter: outputWriter)
        {
            Messaging = new TestServiceBusMessagePumpService(entityType, entityName, configuration, ProjectDirectory, outputWriter);
        }

        /// <summary>
        /// Starts a newly created project from the ServiceBus Queue worker project template.
        /// </summary>
        /// <returns>
        ///     A ServiceBus Queue project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<ServiceBusWorkerProject> StartNewWithQueueAsync(string entityName, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to add telemetry information during the creation and startup process");

            var config = TestTemplatesConfig.Create();
            var options = ServiceBusWorkerProjectOptions.Create(config);
            ServiceBusWorkerProject project = await StartNewWithQueueAsync(entityName, config, options, outputWriter);
            
            return project;
        }

        private static async Task<ServiceBusWorkerProject> StartNewWithQueueAsync(
            string entityName,
            TestTemplatesConfig config,
            ServiceBusWorkerProjectOptions options,
            ITestOutputHelper outputWriter)
        {
            Guard.NotNull(config, nameof(config), "Requires an integration test configuration to configure the resulting project from the Service Bus worker template");
            Guard.NotNull(options, nameof(options), "Requires a set of options to configure the resulting project from the Service Bus worker template");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to add telemetry information during the creation and startup process");

            ServiceBusWorkerProject project = await StartNewAsync(ServiceBusEntityType.Queue, entityName, config, options, outputWriter);
            return project;
        }

        /// <summary>
        /// Starts a newly created project from the ServiceBus Queue worker project template.
        /// </summary>
        /// <returns>
        ///     A ServiceBus Queue project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<ServiceBusWorkerProject> StartNewWithTopicAsync(string entityName, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to add telemetry information during the creation and startup process");

            var config = TestTemplatesConfig.Create();
            var options = ServiceBusWorkerProjectOptions.Create(config);
            ServiceBusWorkerProject project = await StartNewWithTopicAsync(entityName, config, options, outputWriter);
            
            return project;
        }

        private static async Task<ServiceBusWorkerProject> StartNewWithTopicAsync(
            string entityName,
            TestTemplatesConfig config,
            ServiceBusWorkerProjectOptions options,
            ITestOutputHelper outputWriter)
        {
            Guard.NotNull(config, nameof(config), "Requires an integration test configuration to configure the resulting project from the Service Bus worker template");
            Guard.NotNull(options, nameof(options), "Requires a set of options to configure the resulting project from the Service Bus worker template");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to add telemetry information during the creation and startup process");

            ServiceBusWorkerProject project = await StartNewAsync(ServiceBusEntityType.Topic, entityName, config, options, outputWriter);
            return project;
        }

        /// <summary>
        /// Starts a newly created project from the ServiceBus Queue or Topic worker project template.
        /// </summary>
        /// <returns>
        ///     A ServiceBus project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<ServiceBusWorkerProject> StartNewAsync(
            ServiceBusEntityType entityType, 
            string entityName,
            TestTemplatesConfig configuration, 
            ServiceBusWorkerProjectOptions options, 
            ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires an integration test configuration to configure the resulting project from the Service Bus worker template");
            Guard.NotNull(options, nameof(options), "Requires a set of options to configure the resulting project from the Service Bus worker template");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to add telemetry information during the creation and startup process");

            ServiceBusWorkerProject project = CreateNew(entityType, entityName, configuration, options, outputWriter);

            ServiceBusConfig serviceBus = configuration.GetServiceBus();
            
            await project.StartAsync(options,
                CommandArgument.CreateOpen("ARCUS_SERVICEBUS_NAMESPACE", serviceBus.FullyQualifiedNamespace),
                entityType switch
                {
                    ServiceBusEntityType.Queue => CommandArgument.CreateOpen("ARCUS_SERVICEBUS_QUEUENAME", entityName),
                    ServiceBusEntityType.Topic => CommandArgument.CreateOpen("ARCUS_SERVICEBUS_TOPICNAME", entityName)
                });

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
            string entityName,
            TestTemplatesConfig configuration, 
            ServiceBusWorkerProjectOptions options,
            ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires an integration test configuration to configure the resulting project from the Service Bus worker template");
            Guard.NotNull(options, nameof(options), "Requires a set of options to configure the resulting project from the Service Bus worker template");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to add telemetry information during the creation process");

            var project = new ServiceBusWorkerProject(entityType, entityName, configuration, outputWriter);
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
                    "AddServiceBusTopicMessagePumpUsingManagedIdentityWithPrefix(topicName, subscriptionPrefix: \"receive-\", serviceBusNamespace)",
                    $"AddServiceBusTopicMessagePumpUsingManagedIdentityWithPrefix(topicName, subscriptionPrefix: \"receive-\", serviceBusNamespace, configureMessagePump: opt => opt.TopicSubscription = {typeof(TopicSubscription).FullName}.{TopicSubscription.Automatic})"));
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

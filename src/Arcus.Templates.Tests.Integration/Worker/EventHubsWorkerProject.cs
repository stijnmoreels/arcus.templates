using System;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Logging;
using Arcus.Templates.Tests.Integration.Worker.EventHubs.Fixture;
using Arcus.Testing;
using Azure.Storage.Blobs;
using GuardNet;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker
{
    /// <summary>
    /// Project template to create Azure EventHubs worker projects.
    /// </summary>
    public class EventHubsWorkerProject : WorkerProject
    {
        private readonly TemporaryBlobContainer _blobStorageContainer;

        private EventHubsWorkerProject(
            string hubName,
            TestTemplatesConfig configuration, 
            TemporaryBlobContainer blobStorageContainer,
            ITestOutputHelper outputWriter)
            : base(configuration.GetEventHubsProjectDirectory(),
                   configuration,
                   outputWriter)
        {
            _blobStorageContainer = blobStorageContainer;
            Messaging = new TestEventHubsMessagePumpService(hubName, configuration, ProjectDirectory, outputWriter);
        }

        /// <summary>
        /// Starts a newly created project from the Azure EventHubs worker project template.
        /// </summary>
        public static async Task<EventHubsWorkerProject> StartNewAsync(string eventHubName, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a logger instance to write diagnostic information messages during the worker project creation and startup process");

            var config = TestTemplatesConfig.Create();
            var options = EventHubsWorkerProjectOptions.Create(config);

            return await StartNewAsync(eventHubName, config, options, outputWriter);
        }

        /// <summary>
        /// Starts a newly created project from the Azure EventHubs worker project template.
        /// </summary>
        public static async Task<EventHubsWorkerProject> StartNewAsync(string eventHubName, TestTemplatesConfig configuration, EventHubsWorkerProjectOptions options, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a configuration instance to retrieve test configuration values for the used Azure resources in the worker project");
            Guard.NotNull(options, nameof(options), "Requires a project options instance to influence the contents of the worker project");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a logger instance to write diagnostic information messages during the worker project creation and startup process");

            EventHubsWorkerProject project = await CreateNewAsync(eventHubName, configuration, options, outputWriter);
            EventHubsConfig eventHubsConfig = configuration.GetEventHubs();
            
            await project.StartAsync(options,
               CommandArgument.CreateOpen("EVENTHUBS_NAME", eventHubName),
               CommandArgument.CreateOpen("EVENTHUBS_NAMESPACE", eventHubsConfig.FullyQualifiedNamespace),
               CommandArgument.CreateOpen("STORAGEACCOUNT_NAME", eventHubsConfig.StorageAccount.Name),
               CommandArgument.CreateOpen("BLOBSTORAGE_CONTAINERNAME", project._blobStorageContainer.Name));

            return project;
        }

        private static async Task<EventHubsWorkerProject> CreateNewAsync(
            string hubName,
            TestTemplatesConfig configuration, 
            EventHubsWorkerProjectOptions options,
            ITestOutputHelper outputWriter)
        {
            EventHubsConfig eventHubsConfig = configuration.GetEventHubs();
            string containerName = $"container{Guid.NewGuid():N}";

            var serviceClient = new BlobServiceClient(
                new Uri($"https://{eventHubsConfig.StorageAccount.Name}.blob.core.windows.net"), 
                eventHubsConfig.ServicePrincipal.GetCredential());

            var blobStorageContainer = await TemporaryBlobContainer.CreateIfNotExistsAsync(
                serviceClient.GetBlobContainerClient(containerName),
                new XunitTestLogger(outputWriter));

            var project = new EventHubsWorkerProject(hubName, configuration, blobStorageContainer, outputWriter);

            project.CreateNewProject(options);
            project.AddTestMessageHandler();

            return project;
        }

        private void AddTestMessageHandler()
        {
            AddTypeAsFile<SensorUpdate>();
            AddTypeAsFile<SensorStatus>();
            AddTypeAsFile<SensorUpdateEventData>();
            AddTypeAsFile<WriteSensorUpdateToFileAzureEventHubsMessageHandler>();
            
            UpdateFileInProject("Program.cs", contents => 
                RemovesUserErrorsFromContents(contents)
                    .Replace(".MinimumLevel.Debug()", ".MinimumLevel.Verbose()")
                    .Replace("SensorReadingAzureEventHubsMessageHandler", nameof(WriteSensorUpdateToFileAzureEventHubsMessageHandler))
                    .Replace("SensorReading", nameof(SensorUpdate))
                    .Replace("stores.AddAzureKeyVaultWithManagedIdentity(\"https://your-keyvault.vault.azure.net/\", CacheConfiguration.Default);", ""));
        }

        /// <summary>
        /// Performs additional application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">The flag indicating whether or not the additional tasks should be disposed.</param>
        protected override async ValueTask DisposingAsync(bool disposing)
        {
            await _blobStorageContainer.DisposeAsync();
        }
    }
}

using Azure.ResourceManager;
using Azure;
using System.Threading.Tasks;
using System;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Testing;
using Azure.ResourceManager.EventHubs;
using Azure.ResourceManager.EventHubs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Templates.Tests.Integration.Worker.EventHubs.Fixture
{
    /// <summary>
    /// Represents a temporary hub on an Azure EventHubs namespace that is gets deleted when the instance gets disposed.
    /// </summary>
    public class TemporaryEventHubEntity : IAsyncDisposable
    {
        private readonly EventHubsNamespaceResource _eventHubsNamespace;
        private readonly ILogger _logger;

        private TemporaryEventHubEntity(string name, EventHubsNamespaceResource eventHubsNamespace, ILogger logger)
        {
            Name = name;
            _eventHubsNamespace = eventHubsNamespace;
            _logger = logger;
        }

        public string Name { get; }

        public static async Task<TemporaryEventHubEntity> CreateAsync(string name, EventHubsConfig config, ILogger logger)
        {
            var client = new ArmClient(config.ServicePrincipal.GetCredential());

            EventHubsNamespaceResource eventHubsNamespace = client.GetEventHubsNamespaceResource(config.ResourceId);

            logger.LogTrace("[Test] create EventHub '{HubName}'", name);

            EventHubCollection hubs = eventHubsNamespace.GetEventHubs();
            await hubs.CreateOrUpdateAsync(WaitUntil.Completed, name, new EventHubData
            {
                PartitionCount = 1, 
                RetentionDescription = new RetentionDescription
                {
                    CleanupPolicy = CleanupPolicyRetentionDescription.Delete,
                    RetentionTimeInHours = 1,
                }
            });

            return new TemporaryEventHubEntity(name, eventHubsNamespace, logger);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            _logger.LogTrace("[Test] delete EventHub '{HubName}'", Name);
            EventHubResource hub = await _eventHubsNamespace.GetEventHubAsync(Name);
            await hub.DeleteAsync(WaitUntil.Started);

            GC.SuppressFinalize(this);
        }
    }

    public class EventHubsTests : IClassFixture<EventHubsEntityFixture>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubsTests" /> class.
        /// </summary>
        public EventHubsTests(EventHubsEntityFixture fixture)
        {
            HubName = fixture.HubName;
        }

        protected string HubName { get; }
    }

    public class EventHubsEntityFixture : IAsyncLifetime
    {
        private TemporaryEventHubEntity _hub;

        public string HubName { get; } = $"hub-{Guid.NewGuid()}";

        public async Task InitializeAsync()
        {
            var config = TestTemplatesConfig.Create();
            _hub = await TemporaryEventHubEntity.CreateAsync(HubName, config.GetEventHubs(), NullLogger.Instance);
        }

        public async Task DisposeAsync()
        {
            await _hub.DisposeAsync();
        }
    }
}

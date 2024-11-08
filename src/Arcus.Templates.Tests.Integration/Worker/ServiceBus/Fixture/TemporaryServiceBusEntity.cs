using System;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Configuration;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Testing;
using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static Arcus.Observability.Telemetry.Core.ContextProperties.RequestTracking.ServiceBus;
using Xunit;

namespace Arcus.Templates.Tests.Integration.Worker.ServiceBus.Fixture
{
    public class TemporaryServiceBusEntity : IAsyncDisposable
    {
        private readonly ServiceBusEntityType _entityType;
        private readonly ServiceBusNamespaceResource _ns;
        private readonly ILogger _logger;

        private TemporaryServiceBusEntity(ServiceBusEntityType entityType, string entityName, ServiceBusNamespaceResource @namespace, ILogger logger)
        {
            _entityType = entityType;
            _ns = @namespace;
            _logger = logger;
            
            EntityName = entityName;
        }

        public string EntityName { get; }

        public static async Task<TemporaryServiceBusEntity> CreateAsync(ServiceBusEntityType entityType, string entityName, ServiceBusConfig serviceBus, ILogger logger)
        {
            var armClient = new ArmClient(serviceBus.ServicePrincipal.GetCredential());
            ServiceBusNamespaceResource serviceBusNamespace = armClient.GetServiceBusNamespaceResource(serviceBus.ResourceId);

            switch (entityType)
            {
                case ServiceBusEntityType.Queue:
                    logger.LogTrace("[Test] create Service bus queue '{EntityName}'", entityName);
                    await serviceBusNamespace.GetServiceBusQueues()
                                             .CreateOrUpdateAsync(WaitUntil.Completed, entityName, new ServiceBusQueueData());
                    break;

                case ServiceBusEntityType.Topic:
                    logger.LogTrace("[Test] create Service bus topic '{EntityName}'", entityName);
                    await serviceBusNamespace.GetServiceBusTopics()
                                             .CreateOrUpdateAsync(WaitUntil.Completed, entityName, new ServiceBusTopicData());
                    break;

                case ServiceBusEntityType.Unknown:
                default:
                    throw new ArgumentOutOfRangeException(nameof(entityType), entityType, "Unknown Service bus entity type");
            }

            return new TemporaryServiceBusEntity(entityType, entityName, serviceBusNamespace, logger);
        }

        public async ValueTask DisposeAsync()
        {
            switch (_entityType)
            {
                case ServiceBusEntityType.Queue:
                    _logger.LogTrace("[Test] delete Service bus queue '{EntityName}''", EntityName);
                    ServiceBusQueueResource queue = await _ns.GetServiceBusQueueAsync(EntityName);
                    await queue.DeleteAsync(WaitUntil.Started);
                    break;
                
                case ServiceBusEntityType.Topic:
                    _logger.LogTrace("[Test] delete Service bus topic '{EntityName}'", EntityName);
                    ServiceBusTopicResource topic = await _ns.GetServiceBusTopicAsync(EntityName);
                    await topic.DeleteAsync(WaitUntil.Started);
                    break;
                
                case ServiceBusEntityType.Unknown:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public class ServiceBusTests : IClassFixture<ServiceBusEntityFixture>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusTests" /> class.
        /// </summary>
        public ServiceBusTests(ServiceBusEntityFixture fixture)
        {
            QueueName = fixture.QueueName;
            TopicName = fixture.TopicName;
        }

        protected string QueueName { get; }
        protected string TopicName { get; }
        protected string GetEntityName(ServiceBusEntityType entityType)
        {
            return entityType switch
            {
                ServiceBusEntityType.Queue => QueueName,
                ServiceBusEntityType.Topic => TopicName,
            };
        }
    }

    public class ServiceBusEntityFixture : IAsyncLifetime
    {
        private TemporaryServiceBusEntity _queue, _topic;

        public string QueueName { get; } = $"queue-{Guid.NewGuid()}";
        public string TopicName { get; } = $"topic-{Guid.NewGuid()}";

        public async Task InitializeAsync()
        {
            var config = TestTemplatesConfig.Create().GetServiceBus();
            _topic = await TemporaryServiceBusEntity.CreateAsync(ServiceBusEntityType.Topic, TopicName, config, NullLogger.Instance);
            _queue = await TemporaryServiceBusEntity.CreateAsync(ServiceBusEntityType.Queue, QueueName, config, NullLogger.Instance);
        }

        public async Task DisposeAsync()
        {
            await using var disposables = new DisposableCollection(NullLogger.Instance);
            disposables.Add(_queue);
            disposables.Add(_topic);
        }
    }
}

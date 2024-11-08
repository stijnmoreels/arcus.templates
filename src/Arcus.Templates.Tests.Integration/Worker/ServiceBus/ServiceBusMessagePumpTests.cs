using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Worker.ServiceBus.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker.ServiceBus
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class ServiceBusMessagePumpTests(ServiceBusEntityFixture fixture, ITestOutputHelper outputWriter) : ServiceBusTests(fixture)
    {
        [Fact]
        public async Task MinimumServiceBusTopicWorker_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            await using (var project = await ServiceBusWorkerProject.StartNewWithTopicAsync(TopicName, outputWriter))
            {
                // Act / Assert
                await project.Messaging.SimulateMessageProcessingAsync();
            }
        }

         [Fact]
        public async Task MinimumServiceBusQueueWorker_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            await using (var project = await ServiceBusWorkerProject.StartNewWithQueueAsync(QueueName, outputWriter))
            {
                // Act / Assert
                await project.Messaging.SimulateMessageProcessingAsync();
            }
        }
    }
}
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Worker.EventHubs.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker.EventHubs
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class EventHubsMessagePumpTests : EventHubsTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubsMessagePumpTests" /> class.
        /// </summary>
        public EventHubsMessagePumpTests(EventHubsEntityFixture fixture, ITestOutputHelper outputWriter) : base(fixture)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task EventHubsWorker_PublishEventData_MessageSuccessfullyProcessed()
        {
            await using (var project = await EventHubsWorkerProject.StartNewAsync(HubName, _outputWriter))
            {
                await project.Messaging.SimulateMessageProcessingAsync();
            }
        }
    }
}

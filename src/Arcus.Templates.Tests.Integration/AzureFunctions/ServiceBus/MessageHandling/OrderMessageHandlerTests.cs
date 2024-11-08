using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Worker.ServiceBus.Fixture;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.ServiceBus.MessageHandling
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class OrderMessageHandlerTests : ServiceBusTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderMessageHandlerTests" /> class.
        /// </summary>
        public OrderMessageHandlerTests(ServiceBusEntityFixture fixture, ITestOutputHelper outputWriter) : base(fixture)
        {
            _outputWriter = outputWriter;
        }

        [Theory]
        [InlineData(ServiceBusEntityType.Topic)]
        [InlineData(ServiceBusEntityType.Queue)]
        public async Task ServiceBusProject_WithDefault_CorrectlyProcessesMessage(ServiceBusEntityType entityType)
        {
            // Arrange
            string entityName = GetEntityName(entityType);
            await using var project = await AzureFunctionsServiceBusProject.StartNewProjectAsync(entityType, entityName, _outputWriter);

            // Act / Assert
            await project.Messaging.SimulateMessageProcessingAsync();
        }
    }
}

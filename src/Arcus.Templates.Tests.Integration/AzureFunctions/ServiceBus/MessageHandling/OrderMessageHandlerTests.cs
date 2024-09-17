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
    public class OrderMessageHandlerTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderMessageHandlerTests" /> class.
        /// </summary>
        public OrderMessageHandlerTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Theory]
        //[InlineData(ServiceBusEntityType.Topic)]
        [InlineData(ServiceBusEntityType.Queue)]
        public async Task ServiceBusProject_WithDefault_CorrectlyProcessesMessage(ServiceBusEntityType entityType)
        {
            // Arrange
            var config = TestConfig.Create();
            var options = new AzureFunctionsServiceBusProjectOptions();
            options.TearDownOptions = TearDownOptions.KeepProjectDirectory;
            await using var project = await AzureFunctionsServiceBusProject.StartNewProjectAsync(entityType, options, config, _outputWriter);

            //var service = new TestServiceBusMessagePumpService(entityType, config, config.GetAzureFunctionsServiceBusProjectDirectory(entityType), _outputWriter);

            //await service.SimulateMessageProcessingAsync();

            // Act / Assert
            await project.Messaging.SimulateMessageProcessingAsync();
        }
    }
}

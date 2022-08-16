﻿using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
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
        [InlineData(ServiceBusEntityType.Queue)]
        [InlineData(ServiceBusEntityType.Topic)]
        public async Task ServiceBusProject_WithOrderMessageHandlerImplementation_CorrectlyProcessesMessage(ServiceBusEntityType entity)
        {
            // Arrange
            var config = TestConfig.Create();
            await using (var project = await AzureFunctionsServiceBusProject.StartNewProjectAsync(entity, config, _outputWriter))
            {
                // Act / Assert
                await project.MessagePump.SimulateMessageProcessingAsync();
            }
        }
    }
}

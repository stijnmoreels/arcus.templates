using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Worker.ServiceBus.Fixture;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.ServiceBus.Logging
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class SerilogLoggingTests : ServiceBusTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogLoggingTests" /> class.
        /// </summary>
        public SerilogLoggingTests(ServiceBusEntityFixture fixture, ITestOutputHelper outputWriter) : base(fixture)
        {
            _outputWriter = outputWriter;
        }

        [Theory]
        [InlineData(ServiceBusEntityType.Topic)]
        [InlineData(ServiceBusEntityType.Queue)]
        public async Task ServiceBusTopicProject_WithoutSerilog_CorrectlyProcessesMessage(ServiceBusEntityType entityType)
        {
            // Arrange
            var config = TestTemplatesConfig.Create();
            var options = new AzureFunctionsServiceBusProjectOptions().WithExcludeSerilog();

            string entityName = entityType switch
            {
                ServiceBusEntityType.Queue => QueueName,
                ServiceBusEntityType.Topic => TopicName,
            };

            await using var project = await AzureFunctionsServiceBusProject.StartNewProjectAsync(entityType, entityName, options, config, _outputWriter);
            
            // Act / Assert
            await project.Messaging.SimulateMessageProcessingAsync();
            Assert.DoesNotContain("Serilog", project.GetFileContentsOfProjectFile());
            Assert.DoesNotContain("Serilog", project.GetFileContentsInProject(project.RuntimeFileName));
        }
    }
}

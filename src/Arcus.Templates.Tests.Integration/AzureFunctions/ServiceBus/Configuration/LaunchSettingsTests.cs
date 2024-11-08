using System.IO;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Worker.ServiceBus.Fixture;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.ServiceBus.Configuration
{
    [Trait("Category", TestTraits.Integration)]
    public class LaunchSettingsTests : ServiceBusTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="LaunchSettingsTests" /> class.
        /// </summary>
        public LaunchSettingsTests(ServiceBusEntityFixture fixture, ITestOutputHelper outputWriter) : base(fixture)
        {
            _outputWriter = outputWriter;
        }

        [Theory]
        [InlineData(ServiceBusEntityType.Topic)]
        [InlineData(ServiceBusEntityType.Queue)]
        public void ServiceBusTriggerTemplate_WithDefault_ConfiguresLaunchSettings(ServiceBusEntityType entityType)
        {
            // Arrange
            var options = new AzureFunctionsServiceBusProjectOptions();
            var config = TestTemplatesConfig.Create();

            string entityName = entityType switch
            {
                ServiceBusEntityType.Queue => QueueName,
                ServiceBusEntityType.Topic => TopicName,
            };

            // Act
            using (var project = AzureFunctionsServiceBusProject.CreateNew(entityType, entityName, options, config, _outputWriter))
            {
                // Assert
                string relativePath = Path.Combine("Properties", "launchSettings.json");
                string json = project.GetFileContentsInProject(relativePath);
                Assert.Contains(TemplateProject.ProjectName, json);
                Assert.Contains("Docker", json);
            }
        }
    }
}

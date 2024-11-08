using System.IO;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Worker.ServiceBus.Fixture;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker.Configuration
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
        [InlineData(ServiceBusEntityType.Queue)]
        [InlineData(ServiceBusEntityType.Topic)]
        public void WorkerTemplate_WithDefault_ConfiguresLaunchSettings(ServiceBusEntityType entityType)
        {
            // Arrange
            var config = TestTemplatesConfig.Create();
            var options = ServiceBusWorkerProjectOptions.Create(config);

            string entityName = entityType switch
            {
                ServiceBusEntityType.Queue => QueueName,
                ServiceBusEntityType.Topic => TopicName,
            };

            // Act
            using (var project = ServiceBusWorkerProject.CreateNew(entityType, entityName, config, options, _outputWriter))
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

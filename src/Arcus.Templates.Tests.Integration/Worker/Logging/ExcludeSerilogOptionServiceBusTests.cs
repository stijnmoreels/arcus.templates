using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Worker.ServiceBus.Fixture;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker.Logging
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class ExcludeSerilogOptionServiceBusTests : ServiceBusTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcludeSerilogOptionServiceBusTests"/> class.
        /// </summary>
        public ExcludeSerilogOptionServiceBusTests(ServiceBusEntityFixture fixture, ITestOutputHelper outputWriter) : base(fixture)
        {
            _outputWriter = outputWriter;
        }

        [Theory]
        [InlineData(ServiceBusEntityType.Queue)]
        [InlineData(ServiceBusEntityType.Topic)]
        public async Task GetHealthOfServiceBusProject_WithExcludeSerilog_ResponseHealthy(ServiceBusEntityType entityType)
        {
            // Arrange
            var config = TestTemplatesConfig.Create();
            var options = 
                ServiceBusWorkerProjectOptions
                    .Create(config)
                    .WithExcludeSerilog();

            string entityName = GetEntityName(entityType);
            await using var project = await ServiceBusWorkerProject.StartNewAsync(entityType, entityName, config, options, _outputWriter);
            
            // Act
            HealthStatus status = await project.Health.ProbeHealthAsync();
                
            // Assert
            Assert.Equal(HealthStatus.Healthy, status);
            Assert.DoesNotContain("Serilog", project.GetFileContentsInProject("Program.cs"));
            Assert.DoesNotContain("Serilog", project.GetFileContentsOfProjectFile());
        }
    }
}

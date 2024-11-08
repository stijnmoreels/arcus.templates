using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Worker.EventHubs.Fixture;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker.Logging
{
    public class ExcludeSerilogOptionEventHubsTests : EventHubsTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcludeSerilogOptionEventHubsTests" /> class.
        /// </summary>
        public ExcludeSerilogOptionEventHubsTests(EventHubsEntityFixture fixture, ITestOutputHelper outputWriter) : base(fixture)
        {
            _outputWriter = outputWriter;
        }
        
        [Fact]
        public async Task GetHealthOfEventHubsProject_WithExcludeSerilog_ResponseHealthy()
        {
            // Arrange
            var config = TestTemplatesConfig.Create();
            var options =
                EventHubsWorkerProjectOptions
                    .Create(config)
                    .WithExcludeSerilog();

            await using (var project = await EventHubsWorkerProject.StartNewAsync(HubName, config, options, _outputWriter))
            {
                // Act
                HealthStatus status = await project.Health.ProbeHealthAsync();

                // Assert
                Assert.Equal(HealthStatus.Healthy, status);
                Assert.DoesNotContain("Serilog", project.GetFileContentsInProject("Program.cs"));
                Assert.DoesNotContain("Serilog", project.GetFileContentsOfProjectFile());
            }
        }
    }
}
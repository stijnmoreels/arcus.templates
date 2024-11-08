using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Worker.ServiceBus.Fixture;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker.Health
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class TcpHealthProbeTests : ServiceBusTests
    {
        private readonly ITestOutputHelper _outputWriter;

        public TcpHealthProbeTests(ServiceBusEntityFixture fixture, ITestOutputHelper outputWriter) : base(fixture)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task MinimumServiceBusQueueWorker_ProbeForHealthReport_ResponseHealthy()
        {
            // Arrange
            await using (var project = await ServiceBusWorkerProject.StartNewWithQueueAsync(QueueName, _outputWriter))
            {
                // Act
                HealthStatus status = await project.Health.ProbeHealthAsync();

                // Assert
                Assert.Equal(HealthStatus.Healthy, status);
            }
        }
    }
}

﻿using System;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker.Health
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class TcpHealthProbeTests
    {
        private readonly ITestOutputHelper _outputWriter;

        public TcpHealthProbeTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task MinimumServiceBusQueueWorker_ProbeForHealthReport_ResponseHealthy()
        {
            // Arrange
            await using (var project = await ServiceBusWorkerProject.StartNewWithQueueAsync(_outputWriter))
            {
                // Act
                HealthStatus status = await project.Health.ProbeHealthAsync();

                // Assert
                Assert.Equal(HealthStatus.Healthy, status);
            }
        }
    }
}

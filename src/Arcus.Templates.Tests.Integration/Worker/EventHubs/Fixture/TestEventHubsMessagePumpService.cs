using System;
using System.IO;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Logging;
using Arcus.Templates.Tests.Integration.Worker.Fixture;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Bogus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker.EventHubs.Fixture
{
    public class TestEventHubsMessagePumpService : IMessagingService
    {
        private readonly TestConfig _configuration;
        private readonly DirectoryInfo _projectDirectory;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestEventHubsMessagePumpService" /> class.
        /// </summary>
        public TestEventHubsMessagePumpService(
            TestConfig configuration,
            DirectoryInfo projectDirectory,
            ITestOutputHelper outputWriter)
        {
            _configuration = configuration;
            _projectDirectory = projectDirectory;
            _logger = new XunitTestLogger(outputWriter);
        }

        /// <summary>
        /// Simulate the message processing of the message pump using the Service Bus.
        /// </summary>
        public async Task SimulateMessageProcessingAsync()
        {
            var traceParent = TraceParent.Generate();
            SensorUpdate update = GenerateSensorReading();
            await ProduceEventAsync(update, traceParent);

            SensorUpdateEventData sensorReadEventData = await ConsumeMessageAsync(traceParent);
            Assert.NotNull(sensorReadEventData);
            Assert.NotNull(sensorReadEventData.CorrelationInfo);
            Assert.Equal(update.SensorId, sensorReadEventData.SensorId);
            Assert.Equal(update.SensorStatus, sensorReadEventData.SensorStatus);
            Assert.Equal(update.Timestamp, sensorReadEventData.Timestamp);
            Assert.Equal(traceParent.TransactionId, sensorReadEventData.CorrelationInfo.TransactionId);
            Assert.Equal(traceParent.OperationParentId, sensorReadEventData.CorrelationInfo.OperationParentId);
            Assert.NotEmpty(sensorReadEventData.CorrelationInfo.CycleId);
        }

        private static SensorUpdate GenerateSensorReading()
        {
            return new Faker<SensorUpdate>()
                .RuleFor(r => r.SensorId, f => f.Random.Guid().ToString())
                .RuleFor(r => r.SensorStatus, f => f.PickRandom<SensorStatus>())
                .RuleFor(r => r.Timestamp, f => f.Date.RecentOffset())
                .Generate();
        }

        private async Task ProduceEventAsync(SensorUpdate sensorUpdate, TraceParent traceParent)
        {
            var message = new EventData(BinaryData.FromObjectAsJson(sensorUpdate));
            message.Properties["Diagnostic-Id"] = traceParent.DiagnosticId;

            EventHubsConfig eventHubsConfig = _configuration.GetEventHubsConfig();
            await using (var client = new EventHubProducerClient(eventHubsConfig.EventHubsConnectionString, eventHubsConfig.EventHubsName))
            {
                await client.SendAsync(new[] { message });
            }
        }

        private async Task<SensorUpdateEventData> ConsumeMessageAsync(TraceParent traceParent)
        {
            try
            {
                _logger.LogTrace("Consumes a message with transaction ID: {TransactionId}", traceParent.TransactionId);

                FileInfo[] foundFiles =
                    Policy.Timeout(TimeSpan.FromMinutes(1))
                          .Wrap(Policy.HandleResult((FileInfo[] files) => files.Length <= 0)
                                      .WaitAndRetryForever(_ => TimeSpan.FromMilliseconds(200)))
                          .Execute(() => _projectDirectory.GetFiles(traceParent.TransactionId + ".json", SearchOption.AllDirectories));

                FileInfo found = Assert.Single(foundFiles);
                string json = await File.ReadAllTextAsync(found.FullName);
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new MessageCorrelationInfoJsonConverter());

                return JsonConvert.DeserializeObject<SensorUpdateEventData>(json, settings);
            }
            catch (TimeoutException ex)
            {
                throw new TimeoutException(
                    "Failed to retrieve the necessary produced message from the temporary project created from the worker project template, " +
                    "please check whether the injected message handler was correct and if the created project correctly receives the message", ex);
            }
        }
    }
}

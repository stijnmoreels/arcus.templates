using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Azure.Messaging;
using Microsoft.Extensions.Logging;

namespace Arcus.Templates.Tests.Integration.Worker.EventHubs.Fixture
{
    public class WriteSensorUpdateToDiskMessageHandler : IAzureEventHubsMessageHandler<SensorUpdate>
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteSensorUpdateToDiskMessageHandler" /> class.
        /// </summary>
        public WriteSensorUpdateToDiskMessageHandler(ILogger<WriteSensorUpdateToDiskMessageHandler> logger)
        {
            _logger = logger;
        }

        public async Task ProcessMessageAsync(
            SensorUpdate message,
            AzureEventHubsMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing sensor reading {SensorId} for status {SensorStatus} on {Timestamp}", message.SensorId, message.SensorStatus, message.Timestamp);

            await PublishEventToDiskAsync(message, correlationInfo);

            _logger.LogInformation("Sensor {SensorId} processed", message.SensorId);
        }

        private async Task PublishEventToDiskAsync(SensorUpdate message, MessageCorrelationInfo correlationInfo)
        {
            var eventData = new SensorUpdateEventData
            {
                SensorId = message.SensorId,
                SensorStatus = message.SensorStatus,
                Timestamp = message.Timestamp,
                CorrelationInfo = correlationInfo
            };

            var orderCreatedEvent = new CloudEvent(
                "http://test-host",
                "SensorReadEvent",
                jsonSerializableData: eventData)
            {
                Id = correlationInfo.TransactionId,
                Time = DateTimeOffset.UtcNow
            };

            string json = JsonSerializer.Serialize(eventData);
            var fileName = $"{correlationInfo.TransactionId}.json";
            _logger.LogTrace("Processed message by writing on disk: {FileName}", fileName);

            string currentDirPath = Directory.GetCurrentDirectory();
            string filePath = Path.Combine(currentDirPath, fileName);
            await File.WriteAllTextAsync(filePath, json);

            _logger.LogInformation("Event {EventId} was published with subject {EventSubject}", orderCreatedEvent.Id, orderCreatedEvent.Subject);
        }
    }
}

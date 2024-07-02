using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Azure.Messaging.EventHubs;
using GuardNet;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
 
namespace Arcus.Templates.AzureFunctions.EventHubs
{
    public class SensorReadingFunction
    {
        private readonly string _jobId = Guid.NewGuid().ToString();
        private readonly IAzureEventHubsMessageRouter _messageRouter;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SensorReadingFunction"/> class.
        /// </summary>
        /// <param name="messageRouter">The message router instance to route the Azure EventHubs events through the sensor-reading processing.</param
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageRouter"/> is <c>null</c>.</exception>
        public SensorReadingFunction(IAzureEventHubsMessageRouter messageRouter)
        {
            Guard.NotNull(messageRouter, nameof(messageRouter), "Requires a message router instance to route incoming Azure EventHubs events through the sensor-reading processing");
            _messageRouter = messageRouter;
        }

        /// <summary>
        /// Processes Azure EventHubs <paramref name="datas"/>.
        /// </summary>
        /// <param name="datas">The incoming events on the Azure EventHubs instance.</param>
        /// <param name="executionContext">The execution context for this Azure Functions instance.</param>
        [Function("sensor-reading")]
        public async Task Run(
            [EventHubTrigger("sensors", Connection = "EventHubsConnectionString")] EventData[] datas,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger<SensorReadingFunction>();
            logger.LogInformation("Azure EventHubs function triggered with {Length} events", datas.Length);

            foreach (EventData data in datas)
            {
                AzureEventHubsMessageContext messageContext = data.GetMessageContext("sensor-reading.servicebus.windows.net", "sensors", "$Default", _jobId);

                var telemetryClient = executionContext.InstanceServices.GetService<TelemetryClient>();
                (string? transactionId, string? operationParentId) = data.Properties.GetTraceParent();
                using var result = MessageCorrelationResult.Create(telemetryClient, transactionId, operationParentId);

                await _messageRouter.RouteMessageAsync(data, messageContext, result.CorrelationInfo, CancellationToken.None);
            }
        }
    }
}

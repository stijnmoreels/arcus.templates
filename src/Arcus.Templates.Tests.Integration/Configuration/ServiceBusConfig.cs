using Arcus.Templates.Tests.Integration.Fixture;
using GuardNet;
using Microsoft.Extensions.Logging;

namespace Arcus.Templates.Tests.Integration.Configuration
{
    public class ServiceBusConfig
    {
        public ServiceBusConfig(ServicePrincipalConfig servicePrincipal, string ns, string queueName, string topicName)
        {
            Guard.NotNullOrWhitespace(ns, nameof(ns));
            Guard.NotNullOrWhitespace(queueName, nameof(queueName));
            Guard.NotNullOrWhitespace(topicName, nameof(topicName));

            ServicePrincipal = servicePrincipal;
            FullyQualifiedNamespace = ns + ".servicebus.windows.net";
            QueueName = queueName;
            TopicName = topicName;
        }

        public ServicePrincipalConfig ServicePrincipal { get; }
        public string FullyQualifiedNamespace { get; }
        public string QueueName { get; }
        public string TopicName { get; }

        public string GetEntityPath(ServiceBusEntityType type)
        {
            return type switch
            {
                ServiceBusEntityType.Queue => QueueName,
                ServiceBusEntityType.Topic => TopicName,
            };
        }
    }

    public static class ServiceBusTestConfigExtensions
    {
        public static ServiceBusConfig GetServiceBus(this TestConfig config)
        {
            return new ServiceBusConfig(
                config.GetServicePrincipal(),
                config["Arcus:ServiceBus:Namespace"],
                config["Arcus:ServiceBus:QueueName"],
                config["Arcus:ServiceBus:TopicName"]);
        }
    }
}

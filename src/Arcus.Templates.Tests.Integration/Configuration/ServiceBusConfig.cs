using Arcus.Testing;
using Azure.Core;
using Azure.ResourceManager.ServiceBus;
using GuardNet;

namespace Arcus.Templates.Tests.Integration.Configuration
{
    public class ServiceBusConfig
    {
        public ServiceBusConfig(ServicePrincipalConfig servicePrincipal, string subscriptionId, string resourceGroupName, string ns)
        {
            Guard.NotNullOrWhitespace(ns, nameof(ns));

            ServicePrincipal = servicePrincipal;
            FullyQualifiedNamespace = ns + ".servicebus.windows.net";
            ResourceId = ServiceBusNamespaceResource.CreateResourceIdentifier(subscriptionId, resourceGroupName, ns);
        }

        public ServicePrincipalConfig ServicePrincipal { get; }
        public string FullyQualifiedNamespace { get; }
        public ResourceIdentifier ResourceId { get; }
    }

    public static class ServiceBusTestConfigExtensions
    {
        public static ServiceBusConfig GetServiceBus(this TestConfig config)
        {
            return new ServiceBusConfig(
                config.GetServicePrincipal(),
                config.GetSubscriptionId(),
                config.GetResourceGroupName(),
                config["Arcus:ServiceBus:Namespace"]);
        }
    }
}

using System;
using Arcus.Templates.Tests.Integration.Configuration;
using Arcus.Testing;
using Azure.Core;
using Azure.ResourceManager.EventHubs;
using GuardNet;

namespace Arcus.Templates.Tests.Integration.Worker.EventHubs.Fixture
{
    /// <summary>
    /// Represents a configuration section that provides information on Azure EventHubs used during integration testing.
    /// </summary>
    public class EventHubsConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubsConfig" /> class.
        /// </summary>
        public EventHubsConfig(
            string subscriptionId,
            string resourceGroupName,
            ServicePrincipalConfig servicePrincipal,
            StorageAccountConfig storageAccount,
            string eventHubsNamespace)
        {
            ServicePrincipal = servicePrincipal;
            StorageAccount = storageAccount;
            FullyQualifiedNamespace = $"{eventHubsNamespace}.servicebus.windows.net";
            ResourceId = EventHubsNamespaceResource.CreateResourceIdentifier(subscriptionId, resourceGroupName, eventHubsNamespace);
        }

        public ServicePrincipalConfig ServicePrincipal { get; }
        public StorageAccountConfig StorageAccount { get; }

        public ResourceIdentifier ResourceId { get; }
        public string FullyQualifiedNamespace { get; }
    }

    public class StorageAccountConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageAccountConfig" /> class.
        /// </summary>
        public StorageAccountConfig(string accountName)
        {
            Name = accountName;
        }

        public string Name { get; }
    }

    public static class EventHubsConfigTestConfigExtensions
    {
        public static EventHubsConfig GetEventHubs(this TestConfig config)
        {
            return new EventHubsConfig(
                config.GetSubscriptionId(),
                config.GetResourceGroupName(),
                config.GetServicePrincipal(),
                new StorageAccountConfig(config["Arcus:StorageAccount:Name"]),
                config["Arcus:EventHubs:Namespace"]);
        }
    }
}

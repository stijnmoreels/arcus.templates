// Define the location for the deployment of the components.
param location string

// Define the name of the resource group where the components will be deployed.
param resourceGroupName string

// Define the name of the Key vault.
param keyVaultName string

// Define the name of the secret that will store the Application Insights Instrumentation Key in the related Key vault.
param instrumentationKey_secretName string

// Define the topic name of the Service Bus namespace.
param topicName string

// Define the queue name of the Service Bus namespace.
param queueName string

// Define the name of the Event Hub item within the Event Hubs namespace.
param eventHubsName string

// Define the name of the Blob Container within the Storage Account.
param containerName string

// Define the name of the secret that will store the Service Bus topic connection string in the related Key vault.
param topicConnectionString_secretName string

// Define the name of the secret that will store the Service Bus queue connection string in the related Key vault.
param queueConnectionString_secretName string

// Define the name of the secret that will store the Event Hubs connection string in the related Key vault.
param eventHubsConnectionString_secretName string

// Define the name of the secret that will store the Blob Container connection string in the related Key vault.
param storageAccountConnectionString_secretName string

// Define the Service Principal ID that needs access full access to the deployed resource group.
param servicePrincipal_objectId string

targetScope='subscription'

module resourceGroup 'br/public:avm/res/resources/resource-group:0.2.3' = {
  name: 'resourceGroupDeployment'
  params: {
    name: resourceGroupName
    location: location
  }
}

resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' existing = {
  name: resourceGroupName
}

module serviceBus_namespace 'br/public:avm/res/service-bus/namespace:0.5.0' = {
  name: 'namespaceDeployment'
  dependsOn: [
    resourceGroup
  ]
  scope: rg
  params: {
    name: 'arcus-templates-dev-we-sb-ns'
    location: location
    skuObject: {
      name: 'Standard'
  }
  topics: [ { name: topicName } ]
  queues: [ { name: queueName } ]
  authorizationRules: [
    {
      name: 'SendListenAccessKey'
      rights: [ 'Send', 'Listen' ]
    }
  ]
 }
}

resource serviceBus_ns 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' existing = {
  name: serviceBus_namespace.name
  scope: rg
}

module storageAccount 'br/public:avm/res/storage/storage-account:0.9.1' = {
  name: 'storageAccountDeployment'
  dependsOn: [
    resourceGroup
  ]
  scope: rg
  params: {
    name: 'arcustemplatesdevwe'
    location: location
    kind: 'BlobStorage'
    skuName: 'Standard_LRS'
    blobServices: {
      defaultServiceVersion: '2021-06-01'
      container: {
        name: containerName
        publicAccess: 'Container'
      }
    }
  }
}

resource sa 'Microsoft.Storage/storageAccounts@2021-04-01' existing = {
  name: storageAccount.name
  scope: rg
}

module eventHubs_namespace 'br/public:avm/res/event-hub/namespace:0.2.2' = {
  name: 'eventHubNamespaceDeployment'
  dependsOn: [
    resourceGroup
  ]
  scope: rg
  params: {
    name: 'arcus-templates-dev-we-eh-ns'
    location: location
    skuName: 'Basic'
    skuCapacity: 1
    eventhubs: [
      {
        name: eventHubsName
        partitionCount: 2
      }
    ]
    authorizationRules: [
      {
        name: 'SendListenAccessKey'
        rights: [ 'Send', 'Listen' ]
      }
    ]
  }
}

resource eventHubs_ns 'Microsoft.EventHub/namespaces@2024-05-01-preview' existing = {
  name: eventHubs_namespace.name
  scope: rg
}

module workspace 'br/public:avm/res/operational-insights/workspace:0.3.4' = {
  name: 'workspaceDeployment'
  dependsOn: [
    resourceGroup
  ]
  scope: rg
  params: {
    name: 'arcus-templates-dev-we-workspace'
    location: location
  }
}

module component 'br/public:avm/res/insights/component:0.3.0' = {
  name: 'componentDeployment'
  dependsOn: [
    resourceGroup
  ]
  scope: rg
  params: {
    name: 'arcus-templates-dev-we-app-insights'
    workspaceResourceId: workspace.outputs.resourceId
    location: location
  }
}

module vault 'br/public:avm/res/key-vault/vault:0.6.1' = {
  name: 'vaultDeployment'
  dependsOn: [
    resourceGroup
    serviceBus_namespace
  ]
  scope: rg
  params: {
    name: keyVaultName
    location: location
    roleAssignments: [
      {
        principalId: servicePrincipal_objectId
        roleDefinitionIdOrName: 'Key Vault Secrets officer'
      }
    ]
    secrets: [
      {
        name: instrumentationKey_secretName
        value: component.outputs.instrumentationKey
      }
      {
        name: topicConnectionString_secretName
        value: '${listKeys(serviceBus_ns.id, 'SendListenAccessKey').primaryConnectionString};EntityPath=${topicName}'
      }
      {
        name: queueConnectionString_secretName
        value: '${listKeys(serviceBus_ns.id, 'SendListenAccessKey').primaryConnectionString};EntityPath=${queueName}'
      }
      {
        name: eventHubsConnectionString_secretName
        value: '${listKeys(eventHubs_ns.id, 'SendListenAccessKey').primaryConnectionString}'
      }
      {
        name: storageAccountConnectionString_secretName
        value: 'DefaultEndpointsProtocol=https;AccountName=${sa.name};AccountKey=${sa.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
      }
    ]
  }
}

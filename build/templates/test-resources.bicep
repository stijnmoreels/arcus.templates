// Define the location for the deployment of the components.
param location string

// Define the name of the Azure Service bus namespace, used for the Messaging-related project templates.
param serviceBusNamespace string

// Define the name of the Azure EventHubs namespace, used for the Messaging-related project templates.
param eventHubsNamespace string

// Define the name of the Azure Storage account that will be created as a check-point storage for the Azure EventHubs resource.
param storageAccountName string

// Define the name of the single Applization Insights resource.
param appInsightsName string

// Define the name of the Key Vault.
param keyVaultName string

// Define the name of the Azure Key vault secret that holds the Application Insights connection string.
param appInsights_connectionString_secretName string

// Define the name of the Azure Key vault secret that holds the workspace ID of the Log Analytics workspace that hosts the Application Insights resource.
param appInsights_logAnalytics_workspaceId_secretName string

// Define the Service Principal ID that needs access full access to the deployed resource group.
param servicePrincipal_objectId string

module serviceBus 'br/public:avm/res/service-bus/namespace:0.8.0' = {
  name: 'serviceBusDeployment'
  params: {
    name: serviceBusNamespace
    location: location
    skuObject: {
      name: 'Standard'
    }
    publicNetworkAccess: 'Enabled'
    roleAssignments: [
      {
        principalId: servicePrincipal_objectId
        roleDefinitionIdOrName: 'Azure Service Bus Data Owner'
      }
    ]
  }
}

module eventHubs 'br/public:avm/res/event-hub/namespace:0.7.0' = {
  name: 'eventHubsDeployment'
  params: {
    name: eventHubsNamespace
    location: location
    skuName: 'Basic'
    roleAssignments: [
      {
        principalId: servicePrincipal_objectId
        roleDefinitionIdOrName: 'Azure Event Hubs Data Owner'
      }
    ]
  }
}

module workspace 'br/public:avm/res/operational-insights/workspace:0.3.4' = {
  name: 'workspaceDeployment'
  params: {
    name: '${appInsightsName}-workspace'
    location: location
  }
}

module component 'br/public:avm/res/insights/component:0.3.0' = {
  name: 'componentDeployment'
  params: {
    name: appInsightsName
    workspaceResourceId: workspace.outputs.resourceId
    location: location
    roleAssignments: [
      {
        principalId: servicePrincipal_objectId
        roleDefinitionIdOrName: '73c42c96-874c-492b-b04d-ab87d138a893' // Log Analytics Reader
      }
    ]
  }
}

module storageAccount 'br/public:avm/res/storage/storage-account:0.9.1' = {
  name: 'storageAccountDeployment'
  params: {
    name: storageAccountName
    location: location
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
      ipRules: []
      virtualNetworkRules: []
    }
    roleAssignments: [
      {
        principalId: servicePrincipal_objectId
        roleDefinitionIdOrName: 'Storage Blob Data Contributor'
      }
    ]
  }
}

module vault 'br/public:avm/res/key-vault/vault:0.6.1' = {
  name: 'vaultDeployment'
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
        name: appInsights_connectionString_secretName
        value: component.outputs.connectionString
      }
      {
        name: appInsights_logAnalytics_workspaceId_secretName
        value: workspace.outputs.logAnalyticsWorkspaceId
      }
    ]
  }
}

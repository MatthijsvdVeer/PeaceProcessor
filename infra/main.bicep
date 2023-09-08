@description('Resource Location')
param location string = resourceGroup().location

@description('Application Name')
param applicationName string = 'meditation'

@description('Application Insights Workspace Resource Id')
param workspaceResourceId string

var functionsName = 'func-${applicationName}'

module appInsights 'modules/application-insights.bicep' = {
  name: 'application-insights'
  params: {
    functionFullName: functionsName
    applicationName: applicationName
    location: location
    workspaceResourceId: workspaceResourceId
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: 'sa${applicationName}'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    supportsHttpsTrafficOnly: true
  }
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
}

resource storageaccountDiag 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'storageaccountDiag'
  scope: storageAccount
  properties: {
    logAnalyticsDestinationType: 'Dedicated'
    metrics: [
      {
        category: 'Transaction'
        enabled: true
      }
    ]
    workspaceId: workspaceResourceId
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    changeFeed: {
      enabled: true
    }
  }
}

resource blobContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: 'meditation'
  properties: {
    immutableStorageWithVersioning: {
      enabled: false
    }
    publicAccess: 'None'
  }
}

module lifecyclePolicy 'modules/lifecyclePolicy.bicep' = {
  name: 'lifecyclePolicy'
  params: {
    storageAccountName: storageAccount.name
    containerName: blobContainer.name
  }
}

resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
}

resource queue 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-01-01' = {
  parent: queueService
  name: 'topics'
}

module appServicePlan 'modules/hosting-plan.bicep' = {
  name: 'hosting-plan'
  params: {
    location: location
    applicationName: applicationName
  }
}
module functionApp 'modules/function.bicep' = {
  name: 'function'
  params: {
    location: location
    functionFullName: functionsName
    hostingPlanName: appServicePlan.outputs.planName
    applicationInsightsInstrumentationKey: appInsights.outputs.instrumentationKey
    storageAccountName: storageAccount.name
  }
}

module storageBlobDataOwner 'modules/storage-blob-data-owner.bicep' = {
  name: 'storage-blob-data-owner'
  params: {
    storageAccountName: storageAccount.name
    principalIds: [
      functionApp.outputs.principalId
    ]
  }
}

module storageQueueDataContributor 'modules/storage-queue-data-contributor.bicep' = {
  name: 'storage-queue-data-contributor'
  params: {
    storageAccountName: storageAccount.name
    principalIds: [
      functionApp.outputs.principalId
    ]
  }
}

output functionAppName string = functionApp.outputs.functionAppName
output storageAccountName string = storageAccount.name

param principalIds array
param storageAccountName string

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-09-01' existing = {
  name: storageAccountName
}

var adtDataOwner = 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b'
@description('This is the built-in Storage Blob Data Owner role.')
resource contributorRoleDefinition 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: subscription()
  name: adtDataOwner
}

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for principalId in principalIds: {
  scope: storageAccount
  name: guid(storageAccount.id, principalId, adtDataOwner)
  properties: {
    roleDefinitionId: contributorRoleDefinition.id
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}]

param principalIds array
param storageAccountName string

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' existing = {
  name: storageAccountName
}

var adtDataOwner = '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
@description('This is the built-in Storage Queue Data Contributor role.')
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

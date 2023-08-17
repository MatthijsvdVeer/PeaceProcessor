param applicationName string
param location string = resourceGroup().location

resource hostingPlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: 'plan-${applicationName}-${uniqueString(resourceGroup().id)}'
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
    size: 'Y1'
    family: 'Y'
  }
}

output planName string = hostingPlan.name

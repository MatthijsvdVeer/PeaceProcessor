param applicationName string
param location string = resourceGroup().location

resource hostingPlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: 'plan-${applicationName}-${uniqueString(resourceGroup().id)}'
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
    size: 'B1'
    family: 'B'
  }
}

output planName string = hostingPlan.name

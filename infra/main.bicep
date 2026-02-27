targetScope = 'subscription'

@description('Azure region for all resources')
param location string = 'westeurope'

@description('Environment name used in resource naming')
param environmentName string = 'prod'

@description('API key for non-Entra admin auth')
@secure()
param apiKey string

@description('Entra ID tenant ID')
param entraIdTenantId string = subscription().tenantId

@description('Entra ID client ID (app registration)')
param entraIdClientId string

@description('Entra ID audience (defaults to clientId)')
param entraIdAudience string = ''

@description('Email addresses of admin users')
param adminEmails array = []

@description('Tags applied to all resources')
param tags object = {}

var resourceGroupName = 'ansachs-rg-candour-${environmentName}'
var uniqueSuffix = uniqueString(subscription().subscriptionId, resourceGroupName)

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

module resources 'resources.bicep' = {
  name: 'candour-resources'
  scope: rg
  params: {
    location: location
    uniqueSuffix: uniqueSuffix
    apiKey: apiKey
    entraIdTenantId: entraIdTenantId
    entraIdClientId: entraIdClientId
    entraIdAudience: entraIdAudience
    adminEmails: adminEmails
    tags: tags
  }
}

output resourceGroupName string = rg.name
output functionAppName string = resources.outputs.functionAppName
output functionAppUrl string = resources.outputs.functionAppUrl
output cosmosDbAccountName string = resources.outputs.cosmosDbAccountName
output keyVaultName string = resources.outputs.keyVaultName
output appInsightsName string = resources.outputs.appInsightsName

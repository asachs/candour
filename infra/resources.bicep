@description('Azure region for all resources')
param location string

@description('Unique suffix for globally unique names')
param uniqueSuffix string

@secure()
param apiKey string

param entraIdTenantId string
param entraIdClientId string
param entraIdAudience string

@description('Semicolon-delimited admin email addresses')
param adminEmails array = []

@description('Static Web App URL for CORS (empty to skip)')
param staticWebAppUrl string = ''

@description('Tags applied to all resources')
param tags object = {}

// ──────────────────────────────────────────
// Naming
// ──────────────────────────────────────────
var cosmosDbAccountName = 'cosmos-candour-${uniqueSuffix}'
var storageAccountName = 'stcandour${uniqueSuffix}'
var appInsightsName = 'appi-candour-${uniqueSuffix}'
var logAnalyticsName = 'law-candour-${uniqueSuffix}'
var keyVaultName = 'kv-candour-${uniqueSuffix}'
var functionAppName = 'func-candour-${uniqueSuffix}'
var hostingPlanName = 'asp-candour-${uniqueSuffix}'

// ──────────────────────────────────────────
// Log Analytics Workspace
// ──────────────────────────────────────────
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// ──────────────────────────────────────────
// Application Insights
// ──────────────────────────────────────────
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

// ──────────────────────────────────────────
// Storage Account (Function App runtime)
// ──────────────────────────────────────────
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowSharedKeyAccess: false
    allowBlobPublicAccess: false
  }
}

// ──────────────────────────────────────────
// Cosmos DB (Serverless)
// ──────────────────────────────────────────
resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: cosmosDbAccountName
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
  }
}

resource cosmosDb 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' = {
  parent: cosmosDbAccount
  name: 'candour'
  properties: {
    resource: {
      id: 'candour'
    }
  }
}

resource surveyContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: cosmosDb
  name: 'surveys'
  properties: {
    resource: {
      id: 'surveys'
      partitionKey: {
        paths: ['/id']
        kind: 'Hash'
      }
    }
  }
}

resource responseContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: cosmosDb
  name: 'responses'
  properties: {
    resource: {
      id: 'responses'
      partitionKey: {
        paths: ['/surveyId']
        kind: 'Hash'
      }
    }
  }
}

resource usedTokenContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: cosmosDb
  name: 'usedTokens'
  properties: {
    resource: {
      id: 'usedTokens'
      partitionKey: {
        paths: ['/surveyId']
        kind: 'Hash'
      }
      uniqueKeyPolicy: {
        uniqueKeys: [
          {
            paths: ['/tokenHash']
          }
        ]
      }
    }
  }
}

// ──────────────────────────────────────────
// Key Vault
// ──────────────────────────────────────────
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
  }
}

resource batchSecretKey 'Microsoft.KeyVault/vaults/keys@2023-07-01' = {
  parent: keyVault
  name: 'candour-batch-secret'
  properties: {
    kty: 'RSA'
    keySize: 2048
    keyOps: [
      'wrapKey'
      'unwrapKey'
    ]
  }
}

// ──────────────────────────────────────────
// Function App (Consumption Plan, Linux)
// ──────────────────────────────────────────
resource hostingPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: hostingPlanName
  location: location
  tags: tags
  kind: 'functionapp'
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
  }
  properties: {
    reserved: true
  }
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  tags: tags
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${storageAccount.properties.primaryEndpoints.blob}deploymentpackage'
          authentication: {
            type: 'SystemAssignedIdentity'
          }
        }
      }
      scaleAndConcurrency: {
        maximumInstanceCount: 40
        instanceMemoryMB: 2048
      }
      runtime: {
        name: 'dotnet-isolated'
        version: '9.0'
      }
    }
    httpsOnly: true
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage__accountName'
          value: storageAccount.name
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'CosmosDb__ConnectionString'
          value: cosmosDbAccount.listConnectionStrings().connectionStrings[0].connectionString
        }
        {
          name: 'CosmosDb__DatabaseName'
          value: 'candour'
        }
        {
          name: 'KeyVault__Uri'
          value: keyVault.properties.vaultUri
        }
        {
          name: 'KeyVault__KeyName'
          value: 'candour-batch-secret'
        }
        {
          name: 'Candour__ApiKey'
          value: apiKey
        }
        {
          name: 'Candour__Auth__UseEntraId'
          value: 'true'
        }
        {
          name: 'Candour__Auth__TenantId'
          value: entraIdTenantId
        }
        {
          name: 'Candour__Auth__ClientId'
          value: entraIdClientId
        }
        {
          name: 'Candour__Auth__Audience'
          value: empty(entraIdAudience) ? entraIdClientId : entraIdAudience
        }
        {
          name: 'Candour__Auth__AdminEmails'
          value: join(adminEmails, ';')
        }
        {
          name: 'Candour__FrontendBaseUrl'
          value: staticWebAppUrl
        }
      ]
      cors: {
        allowedOrigins: union([
          'https://localhost:5000'
          'https://localhost:5001'
        ], empty(staticWebAppUrl) ? [] : [staticWebAppUrl])
        supportCredentials: true
      }
    }
  }
}

// ──────────────────────────────────────────
// RBAC: Function App → Key Vault Crypto User
// NOTE: Requires Microsoft.Authorization/roleAssignments/write permission.
//       If deployer lacks this, set deployRbac=false and assign manually in portal:
//       Key Vault → Access control (IAM) → Add → Key Vault Crypto User → Function App identity
// ──────────────────────────────────────────
param deployRbac bool = false

var keyVaultCryptoUserId = '12338af4-9209-45ee-9d58-7c5c2b7114f5' // Key Vault Crypto User

resource functionAppKeyVaultRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (deployRbac) {
  name: guid(keyVault.id, functionApp.id, keyVaultCryptoUserId)
  scope: keyVault
  properties: {
    principalId: functionApp.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultCryptoUserId)
    principalType: 'ServicePrincipal'
  }
}

// ──────────────────────────────────────────
// Outputs
// ──────────────────────────────────────────
output functionAppName string = functionApp.name
output functionAppUrl string = 'https://${functionApp.properties.defaultHostName}'
output cosmosDbAccountName string = cosmosDbAccount.name
output keyVaultName string = keyVault.name
output appInsightsName string = appInsights.name
output functionAppPrincipalId string = functionApp.identity.principalId

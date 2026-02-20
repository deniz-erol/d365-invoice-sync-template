// Main Bicep deployment for D365 Invoice Sync
// Deploys: Service Bus, Functions, Key Vault, App Insights, APIM

@description('Environment name')
param environmentName string = 'prod'

@description('Location for all resources')
param location string = resourceGroup().location

@description('External system type: xero, quickbooks, or custom')
param externalSystemType string = 'xero'

@description('D365 organization URL')
param d365OrganizationUrl string

var baseName = 'd365-invoice-${environmentName}'

// Resource Group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: '${baseName}-rg'
  location: location
  tags: {
    Project: 'D365-Invoice-Sync'
    Environment: environmentName
  }
}

// Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${baseName}-insights'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

// Log Analytics Workspace
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2021-06-01' = {
  name: '${baseName}-logs'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// Service Bus Namespace
resource serviceBus 'Microsoft.ServiceBus/namespaces@2021-11-01' = {
  name: '${baseName}-sb'
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {}
}

// Service Bus Topic
resource invoiceTopic 'Microsoft.ServiceBus/namespaces/topics@2021-11-01' = {
  parent: serviceBus
  name: 'invoice-posted'
  properties: {
    defaultMessageTimeToLive: 'P1D'
    maxSizeInMegabytes: 1024
  }
}

// Service Bus Subscription
resource invoiceSubscription 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2021-11-01' = {
  parent: invoiceTopic
  name: 'invoice-processor'
  properties: {
    deadLetteringOnFilterEvaluationExceptions: true
    deadLetteringOnMessageExpiration: true
    maxDeliveryCount: 3
    lockDuration: 'PT5M'
  }
}

// Service Bus DLQ (Dead Letter Queue is automatic, but we create a separate queue for manual retries)
resource dlqQueue 'Microsoft.ServiceBus/namespaces/queues@2021-11-01' = {
  parent: serviceBus
  name: 'invoice-dlq'
  properties: {
    maxDeliveryCount: 10
    lockDuration: 'PT5M'
  }
}

// Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: '${baseName}-kv'
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    accessPolicies: []
    enableRbacAuthorization: true
  }
}

// Storage Account for Functions
resource storageAccount 'Microsoft.Storage/storageAccounts@2021-09-01' = {
  name: '${replace(baseName, '-', '')}func'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
  }
}

// Function App
resource functionApp 'Microsoft.Web/sites@2022-03-01' = {
  name: '${baseName}-func'
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'ServiceBusConnection__fullyQualifiedNamespace'
          value: '${serviceBus.name}.servicebus.windows.net'
        }
        {
          name: 'KeyVaultUri'
          value: keyVault.properties.vaultUri
        }
        {
          name: 'ExternalSystemType'
          value: externalSystemType
        }
        {
          name: 'D365OrganizationUrl'
          value: d365OrganizationUrl
        }
      ]
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
    }
    httpsOnly: true
  }
}

// Function App Hosting Plan (Consumption)
resource hostingPlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: '${baseName}-plan'
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
    size: 'Y1'
    family: 'Y'
    capacity: 0
  }
  properties: {}
}

// API Management
resource apim 'Microsoft.ApiManagement/service@2022-08-01' = {
  name: '${baseName}-apim'
  location: location
  sku: {
    name: 'Consumption'
    capacity: 0
  }
  properties: {
    publisherEmail: 'admin@example.com'
    publisherName: 'D365 Invoice Sync'
  }
}

// Key Vault Secrets
resource externalApiClientId 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'ExternalApiClientId'
  properties: {
    value: 'REPLACE_WITH_YOUR_CLIENT_ID'
  }
}

resource externalApiClientSecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'ExternalApiClientSecret'
  properties: {
    value: 'REPLACE_WITH_YOUR_CLIENT_SECRET'
  }
}

// Role Assignment for Function App to access Service Bus
resource serviceBusRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBus.id, functionApp.id, 'Azure Service Bus Data Receiver')
  scope: serviceBus
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0') // Azure Service Bus Data Receiver
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Role Assignment for Function App to access Key Vault
resource keyVaultSecretsUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, functionApp.id, 'Key Vault Secrets User')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6') // Key Vault Secrets User
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Outputs
output functionAppName string = functionApp.name
output serviceBusNamespace string = serviceBus.name
output keyVaultUri string = keyVault.properties.vaultUri
output appInsightsName string = appInsights.name

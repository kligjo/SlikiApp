targetScope = 'resourceGroup'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Azure region for the storage account.')
param storageLocation string = resourceGroup().location

@description('Azure region for monitoring resources (Log Analytics, Application Insights).')
param monitoringLocation string = resourceGroup().location

@description('Short application base name used for generated resource names.')
@minLength(3)
@maxLength(18)
param appBaseName string = 'sliki'

@description('Environment suffix used in resource names.')
@allowed([
  'dev'
  'test'
  'prod'
])
param environmentName string = 'dev'

@description('Blob container name used by the application.')
param containerName string = 'sliki'

@description('App Service plan SKU.')
param appServiceSkuName string = 'B1'

@description('App Service plan tier.')
param appServiceSkuTier string = 'Basic'

@description('Linux runtime stack for the web app.')
param linuxFxVersion string = 'DOTNETCORE|10.0'

@description('Maximum upload size per file in bytes.')
@minValue(1048576)
param maxUploadBytes int = 10485760

@description('Gallery page size.')
@minValue(1)
@maxValue(50)
param pageSize int = 12

@description('Override for the web app resource name.')
param webAppNameOverride string = ''

@description('Override for the App Service plan resource name.')
param appServicePlanNameOverride string = ''

@description('Override for the storage account resource name.')
param storageAccountNameOverride string = ''

var normalizedBaseName = toLower(replace(appBaseName, '-', ''))
var uniqueSuffix = toLower(uniqueString(subscription().subscriptionId, resourceGroup().id, appBaseName, environmentName))
var webAppName = empty(webAppNameOverride) ? take('${normalizedBaseName}-${environmentName}-${uniqueSuffix}', 60) : webAppNameOverride
var appServicePlanName = empty(appServicePlanNameOverride) ? '${appBaseName}-${environmentName}-plan' : appServicePlanNameOverride
var storageAccountName = empty(storageAccountNameOverride) ? take('${normalizedBaseName}${environmentName}${uniqueSuffix}', 24) : storageAccountNameOverride
var workspaceName = '${appBaseName}-${environmentName}-law'
var appInsightsName = '${appBaseName}-${environmentName}-appi'
var blobContributorRoleId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  'ba92f5b4-2d11-453d-a403-e96b0029c9fe')

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: storageLocation
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  name: 'default'
  parent: storageAccount
}

resource imageContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  name: containerName
  parent: blobService
  properties: {
    publicAccess: 'None'
  }
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: workspaceName
  location: monitoringLocation
  properties: {
    retentionInDays: 30
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: monitoringLocation
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Flow_Type: 'Bluefield'
    IngestionMode: 'LogAnalytics'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  kind: 'linux'
  sku: {
    name: appServiceSkuName
    tier: appServiceSkuTier
  }
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: linuxFxVersion
      alwaysOn: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'BlobStorage__ServiceUri'
          value: storageAccount.properties.primaryEndpoints.blob
        }
        {
          name: 'BlobStorage__ContainerName'
          value: containerName
        }
        {
          name: 'BlobStorage__MaxUploadBytes'
          value: string(maxUploadBytes)
        }
        {
          name: 'BlobStorage__PageSize'
          value: string(pageSize)
        }
      ]
    }
  }
}

resource blobContributorAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, webApp.id, blobContributorRoleId)
  scope: storageAccount
  properties: {
    principalId: webApp.identity.principalId
    roleDefinitionId: blobContributorRoleId
    principalType: 'ServicePrincipal'
  }
}

output webAppName string = webApp.name
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output storageAccountName string = storageAccount.name
output blobContainerName string = imageContainer.name

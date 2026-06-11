targetScope = 'resourceGroup'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Azure region for SQL resources (use a region that accepts SQL provisioning).')
param sqlLocation string = 'austriaeast'

@description('Short application base name used for generated resource names.')
@minLength(3)
@maxLength(18)
param appBaseName string = 'desklmphotos'

@description('Environment suffix used in resource names.')
@allowed([
  'dev'
  'test'
  'prod'
])
param environmentName string = 'dev'

@description('Blob container name used by the application.')
param containerName string = 'desklmphotos'

@description('SQL Server administrator login name.')
param sqlAdminLogin string = 'sqladmin'

@description('SQL Server administrator password.')
@secure()
param sqlAdminPassword string

var normalizedBaseName = toLower(replace(appBaseName, '-', ''))
var uniqueSuffix = toLower(uniqueString(subscription().subscriptionId, resourceGroup().id, appBaseName, environmentName))
var appServicePlanName = '${appBaseName}-${environmentName}-asp'
var webAppName = '${appBaseName}-${environmentName}-app'
var storageAccountName = take('${normalizedBaseName}${environmentName}${uniqueSuffix}', 24)
var workspaceName = '${appBaseName}-${environmentName}-law'
var appInsightsName = '${appBaseName}-${environmentName}-appi'
var sqlServerName = '${appBaseName}-${environmentName}-sql'
var sqlDatabaseName = '${appBaseName}-${environmentName}-db'
var blobContributorRoleId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  'ba92f5b4-2d11-453d-a403-e96b0029c9fe')

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
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
  location: location
  properties: {
    retentionInDays: 30
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Flow_Type: 'Bluefield'
    IngestionMode: 'LogAnalytics'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: 'austriaeast'
  kind: 'linux'
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2023-01-01' = {
  name: webAppName
  location: 'austriaeast'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      http20Enabled: true
      minTlsVersion: '1.2'
      appCommandLine: 'dotnet UmbracoCms.Web.dll'
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
          value: '10485760'
        }
        {
          name: 'BlobStorage__PageSize'
          value: '12'
        }
        {
          name: 'ConnectionStrings__umbracoDbDSN'
          value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabase.name};Persist Security Info=False;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
        }
        {
          name: 'ConnectionStrings__umbracoDbDSN_ProviderName'
          value: 'Microsoft.Data.SqlClient'
        }
        {
          name: 'WEBSITES_CONTAINER_START_TIME_LIMIT'
          value: '600'
        }
        {
          name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
          value: 'true'
        }
        {
          name: 'UMBRACO__CMS__HOSTING__LOCALTEMPSTORAGELOCATION'
          value: 'EnvironmentTemp'
        }
        {
          name: 'UMBRACO__CMS__EXAMINE__LUCENEDIRECTORYFACTORY'
          value: 'SyncedTempFileSystemDirectoryFactory'
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

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: sqlLocation
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource sqlFirewallAllowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  name: 'AllowAllWindowsAzureIps'
  parent: sqlServer
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  name: sqlDatabaseName
  parent: sqlServer
  location: sqlLocation
  sku: {
    name: 'S0'
    tier: 'Standard'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 10737418240
  }
}

output webAppName string = webApp.name
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output storageAccountName string = storageAccount.name
output blobContainerName string = imageContainer.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlDatabaseName string = sqlDatabase.name

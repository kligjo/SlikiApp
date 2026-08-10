targetScope = 'resourceGroup'

@description('Short base name used for generated resource names.')
@minLength(3)
@maxLength(15)
param baseName string = 'aksdemo'

@description('Environment suffix used in resource names.')
param environmentName string = 'demo'

@description('Location for all resources.')
param location string = 'westeurope'

@description('Number of nodes in the AKS system node pool.')
@minValue(1)
@maxValue(5)
param nodeCount int = 2

@description('VM size for AKS nodes.')
param nodeVmSize string = 'Standard_D2s_v5'

@description('ACR SKU.')
@allowed([
  'Basic'
  'Standard'
])
param acrSku string = 'Basic'

@description('Object ID (not client ID) of the GitHub Actions OIDC service principal, granted AcrPush + AKS Cluster Admin. Leave empty to skip.')
param githubActionsPrincipalId string = ''

var normalizedBaseName = toLower(replace(baseName, '-', ''))
var uniqueSuffix = toLower(uniqueString(subscription().subscriptionId, resourceGroup().id, baseName, environmentName))
var acrName = take('acr${normalizedBaseName}${uniqueSuffix}', 50)
var aksName = '${baseName}-${environmentName}-aks'

var acrPullRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
var acrPushRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8311e382-0749-4cb8-b61a-304f252e45ec')
var aksClusterAdminRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0ab0b1a8-8aac-4efd-b8c2-3ee1fb270be8')

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  sku: {
    name: acrSku
  }
  properties: {
    adminUserEnabled: false
  }
}

resource aks 'Microsoft.ContainerService/managedClusters@2024-02-01' = {
  name: aksName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    dnsPrefix: '${baseName}${environmentName}'
    agentPoolProfiles: [
      {
        name: 'system'
        count: nodeCount
        vmSize: nodeVmSize
        mode: 'System'
        osType: 'Linux'
      }
    ]
    enableRBAC: true
  }
}

// Mirrors `az aks create --attach-acr`: let the cluster's kubelet identity pull images.
resource aksAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, aks.id, acrPullRoleId)
  scope: acr
  properties: {
    principalId: aks.properties.identityProfile.kubeletidentity.objectId
    roleDefinitionId: acrPullRoleId
    principalType: 'ServicePrincipal'
  }
}

// Lets the GitHub Actions OIDC identity push images and deploy via `az aks get-credentials --admin`.
resource pipelineAcrPush 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(githubActionsPrincipalId)) {
  name: guid(acr.id, githubActionsPrincipalId, acrPushRoleId)
  scope: acr
  properties: {
    principalId: githubActionsPrincipalId
    roleDefinitionId: acrPushRoleId
    principalType: 'ServicePrincipal'
  }
}

resource pipelineAksAdmin 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(githubActionsPrincipalId)) {
  name: guid(aks.id, githubActionsPrincipalId, aksClusterAdminRoleId)
  scope: aks
  properties: {
    principalId: githubActionsPrincipalId
    roleDefinitionId: aksClusterAdminRoleId
    principalType: 'ServicePrincipal'
  }
}

output acrName string = acr.name
output acrLoginServer string = acr.properties.loginServer
output aksClusterName string = aks.name

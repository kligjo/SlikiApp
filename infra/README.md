# Sliki Azure infrastructure

This folder provisions the Azure resources required by the app:

- Linux Azure App Service plan
- Azure Web App with **system-assigned managed identity**
- Azure Storage account
- Private blob container named `sliki`
- Storage RBAC: **Storage Blob Data Contributor** for the web app identity
- Log Analytics workspace
- Application Insights

## Files

- `main.bicep` - Azure resource definitions
- `main.parameters.json` - default deployment parameters
- `deploy.ps1` - wrapper script for `az deployment group create`

## GitHub Actions CI/CD

The repository includes a workflow at `.github/workflows/deploy-azure.yml` that:

1. Builds and publishes the Blazor app
2. Deploys the Azure infrastructure from `main.bicep`
3. Zip-deploys the web app to Azure App Service

### Required GitHub secrets

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

These are used by `azure/login` with OpenID Connect (OIDC).

### Required GitHub variables

- `AZURE_RESOURCE_GROUP`
- `AZURE_LOCATION`

### Azure setup for GitHub OIDC

Create or reuse a Microsoft Entra application / service principal, then add a **federated credential** for your GitHub repository and branch/environment. Grant that identity permissions to:

- deploy resources in the target resource group
- deploy the web app
- assign RBAC if the infrastructure deployment creates role assignments

At minimum, the identity typically needs **Contributor** on the resource group and **User Access Administrator** if role assignments are created by the Bicep template.

## Prerequisites

1. Azure CLI installed
2. Logged in with an account that can create resource groups, role assignments, App Service, and Storage resources

## Deploy

From the repository root:

```powershell
.\infra\deploy.ps1 -ResourceGroupName rg-sliki-dev
```

Optional:

```powershell
.\infra\deploy.ps1 -ResourceGroupName rg-sliki-prod -Location westeurope
```

## Parameters you may want to change

Edit `main.parameters.json` if needed:

- `appBaseName`
- `environmentName`
- `linuxFxVersion`
- `appServiceSkuName`
- `appServiceSkuTier`
- `maxUploadBytes`
- `pageSize`

## After deployment

Deploy the app itself to the created Web App, for example:

```powershell
dotnet publish .\Sliki.Web\Sliki.Web.csproj -c Release
Compress-Archive -Path .\Sliki.Web\bin\Release\net10.0\publish\* -DestinationPath .\Sliki.Web\bin\Release\net10.0\publish.zip -Force
az webapp deploy --resource-group <resource-group> --name <web-app-name> --src-path .\Sliki.Web\bin\Release\net10.0\publish.zip --type zip
```

You can get the created web app name from the deployment output.

If you change the app target framework or your Azure subscription does not support `.NET 10` yet, update `linuxFxVersion` in `main.parameters.json` before deploying.

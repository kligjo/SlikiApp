[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,

    [string]$Location = "westeurope",

    [string]$TemplateFile = ".\infra\main.bicep",

    [string]$ParametersFile = ".\infra\main.parameters.json"
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI (az) is required."
}

function Resolve-RepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepositoryRoot,

        [Parameter(Mandatory = $true)]
        [string]$PathValue
    )

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return $PathValue
    }

    if ($PathValue.StartsWith(".\") -or $PathValue.StartsWith("./")) {
        $PathValue = $PathValue.Substring(2)
    }

    return Join-Path $RepositoryRoot $PathValue
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$templatePath = Resolve-RepoPath -RepositoryRoot $repositoryRoot -PathValue $TemplateFile
$parametersPath = Resolve-RepoPath -RepositoryRoot $repositoryRoot -PathValue $ParametersFile

if (-not (Test-Path $templatePath)) {
    throw "Template file not found: $templatePath"
}

if (-not (Test-Path $parametersPath)) {
    throw "Parameters file not found: $parametersPath"
}

$accountCheck = az account show --output none 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Signing in to Azure..."
    az login | Out-Null
}

Write-Host "Ensuring resource group '$ResourceGroupName' exists in '$Location'..."
az group create `
    --name $ResourceGroupName `
    --location $Location `
    --output table

Write-Host "Deploying infrastructure..."
az deployment group create `
    --resource-group $ResourceGroupName `
    --template-file $templatePath `
    --parameters "@$parametersPath" `
    --parameters location=$Location `
    --output table

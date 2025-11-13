# Deployment Guide (.NET Azure Functions)

## Overview
Deploy the .NET 10 Azure Functions implementation of the Central Memory MCP Server to Azure. Local development uses Azurite; production uses Azure Table Storage with managed identity.

## Local Development
**Prerequisites:**
- .NET 10 SDK
- Azure Functions Core Tools v4
- Azurite (local Table Storage emulator)

**Steps:**
```bash
dotnet restore
dotnet build
func start --port 7071
curl http://localhost:7071/api/health
curl http://localhost:7071/api/ready
```
**Local settings** (set env vars or use `local.settings.json`):
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
```

## Azure Deployment (CLI)
**Create resources:**
```bash
az group create --name cmemory-rg --location eastus
az storage account create --name <storageName> --resource-group cmemory-rg --location eastus --sku Standard_LRS
az functionapp create --name <funcAppName> --resource-group cmemory-rg --storage-account <storageName> --runtime dotnet-isolated --functions-version 4 --consumption-plan-location eastus
```
**Assign managed identity:**
```bash
az functionapp identity assign --name <funcAppName> --resource-group cmemory-rg
```
**Configure settings** (if using connection string fallback):
```bash
az functionapp config appsettings set --name <funcAppName> --resource-group cmemory-rg --settings "AzureWebJobsStorage=<connectionString>"
```
**Publish:**
```bash
dotnet publish dotnet/CentralMemoryMcp.Functions/CentralMemoryMcp.Functions.csproj -c Release -o publish
func azure functionapp publish <funcAppName> --dotnet-isolated --source-path publish
```

## GitHub Actions (Sample)
```yaml
name: build-deploy
on:
  push:
    branches: [ main ]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '10.0.x'
    - name: Restore
      run: dotnet restore dotnet/CentralMemoryMcp.Functions
    - name: Build
      run: dotnet build dotnet/CentralMemoryMcp.Functions -c Release --no-restore
    - name: Publish
      run: dotnet publish dotnet/CentralMemoryMcp.Functions -c Release -o publish
    - name: Deploy
      uses: Azure/functions-action@v1
      with:
        app-name: ${{ secrets.AZURE_FUNCTIONAPP_NAME }}
        package: publish
        publish-profile: ${{ secrets.AZURE_FUNCTIONAPP_PUBLISH_PROFILE }}
```

## Configuration
**Key settings:**
- `AzureWebJobsStorage`: connection string or managed identity
- `FUNCTIONS_WORKER_RUNTIME`: dotnet-isolated
- `APPINSIGHTS_CONNECTION_STRING` (optional)

## Health & Readiness
Use `/api/health` and `/api/ready` for probes in Azure (App Service health checks / container orchestrators).

## Security
- Prefer managed identity over raw keys
- Store secrets in GitHub Actions secrets / Key Vault
- Restrict public network access to storage (private endpoints)

## Observability
**Add Application Insights:**
```bash
az monitor app-insights component create --app cmemory-ai --location eastus --resource-group cmemory-rg
az functionapp config appsettings set --name <funcAppName> --resource-group cmemory-rg --settings "APPLICATIONINSIGHTS_CONNECTION_STRING=<conn>"
```

## Scaling
Consumption plan auto-scales. For higher throughput consider Premium plan (pre-warmed instances). Monitor storage saturation and function cold starts.

## Rollbacks
Maintain previous publish artifacts; redeploy prior version using stored package.

## Future Enhancements
- Bicep/ARM infra as code
- Canary deployment slot
- Blue/green slot swap automation

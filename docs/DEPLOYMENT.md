# Deployment Guide (Alpha Implementation)

## Local Development

Prerequisites:

- .NET 10 SDK
- Azure Functions Core Tools v4
- Azurite (for local Table Storage)

Steps:

```bash
dotnet restore
dotnet build
func start --port 7071
curl http://localhost:7071/api/health
```

## Azure Deployment (Manual)

```bash
az group create --name cmemory-rg --location eastus
az storage account create --name <storageName> --resource-group cmemory-rg --location eastus --sku Standard_LRS
az functionapp create --name <funcAppName> --resource-group cmemory-rg --storage-account <storageName> --runtime dotnet-isolated --functions-version 4 --consumption-plan-location eastus
```

Publish:

```bash
dotnet publish CentralMemoryMcp.Functions/CentralMemoryMcp.Functions.csproj -c Release -o publish
func azure functionapp publish <funcAppName> --dotnet-isolated --source-path publish
```

## Configuration Settings

- AzureWebJobsStorage (required)
- FUNCTIONS_WORKER_RUNTIME = dotnet-isolated
- APPLICATIONINSIGHTS_CONNECTION_STRING (optional)

## Health Endpoints

- GET /api/health -> OK
- GET /api/ready -> READY

Use for probes and monitoring.

## Current Scope Reminder

Only four MCP tools implemented: read_graph, upsert_entity, upsert_relation, get_entity_relations.
Adjust monitoring expectations accordingly (no search/stats endpoints yet).

## GitHub Actions Sample (future)

Add workflow to restore, build, publish artifact, deploy with publish profile or Azure login.

## Roadmap

- Add CI pipeline.
- Slot-based deployments.
- Infrastructure as code (Bicep).
- Managed identity for Table Storage (replace connection string).

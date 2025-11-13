# Central Memory MCP Server (.NET)

Index page for the Central Memory MCP Server documentation.

## Overview
A .NET 10 Azure Functions (isolated) implementation of a Model Context Protocol (MCP) memory & knowledge graph service. Provides durable, queryable project memory for AI assistants through HTTP-exposed MCP tool endpoints.

### Capabilities
- Entities (typed nodes with observations & metadata)
- Relations (directed, typed edges)
- Observations (append-only time-stamped facts)
- Graph stats & temporal events
- Duplicate detection & merge operations
- Workspace isolation via `workspaceName`
- Health & readiness endpoints

## Quick Start
```bash
dotnet restore
dotnet build
func start --port 7071
curl http://localhost:7071/api/health
curl http://localhost:7071/api/ready
```
Local storage:
```
AzureWebJobsStorage=UseDevelopmentStorage=true
```

## MCP Tool Workflow (Recommended)
1. read_graph
2. search_entities
3. create_entities
4. create_relations
5. add_observation

Edge cases: Missing entities auto-created during relation/observation operations.

## Directory Highlights
- Functions: HTTP endpoints (`GraphFunctions.cs`, `HealthFunctions.cs`)
- Services: Domain logic (`KnowledgeGraphService.cs`)
- Storage: Azure Table abstraction (`TableStorageService.cs`)
- Models: DTO contracts (`GraphModels.cs`)

## Configuration
Edit `appsettings.json` (local). Set production values via environment/environment variables:
```
AzureWebJobsStorage=DefaultEndpointsProtocol=https;AccountName=<name>;AccountKey=<key>;EndpointSuffix=core.windows.net
```

## Documentation Set
- [ARCHITECTURE.md](./ARCHITECTURE.md) – Design & layering
- [API.md](./API.md) – MCP tool endpoints
- [STORAGE.md](./STORAGE.md) – Persistence model
- [DEPLOYMENT.md](./DEPLOYMENT.md) – Deployment guidance

## License
MIT (see repository `LICENSE`).

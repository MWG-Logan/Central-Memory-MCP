# Central Memory MCP Server (.NET 10 Azure Functions)
[![Trust Score](https://archestra.ai/mcp-catalog/api/badge/quality/MWGMorningwood/Central-Memory-MCP)](https://archestra.ai/mcp-catalog/mwgmorningwood__central-memory-mcp)

Model Context Protocol (MCP) compliant memory & knowledge graph server implemented in .NET 10 (Azure Functions isolated worker). Provides durable project memory (entities, relations, observations, statistics) for AI assistants (e.g. GitHub Copilot) with workspace isolation and simple HTTP tool endpoints.

## 🧠 Core Concepts
- Workspace Isolation via `workspaceName` partition key
- Entities: name, type, observations (append-only facts), metadata
- Relations: directed edges (`from` -> `to`, typed, optional user metadata)
- Observations: time-stamped appended facts enriching entities
- Graph Stats: counts, temporal events, duplicate detection helpers

## 🏗 Technology Stack
- .NET 10 Azure Functions (isolated)
- Azure Table Storage (Azurite for local dev)
- Dependency Injection (generic host builder)
- Structured Logging (ILogger scopes per workspace)
- Simple DTO surface for MCP tools

## 🚀 Quick Start
```bash
# Restore & build
dotnet restore
dotnet build

# Run locally (Azure Functions Core Tools v4 required)
func start --port 7071

# Health
curl http://localhost:7071/api/health
curl http://localhost:7071/api/ready
```
Local storage configuration:
```
AzureWebJobsStorage=UseDevelopmentStorage=true
```
Production example:
```
AzureWebJobsStorage=DefaultEndpointsProtocol=https;AccountName=<name>;AccountKey=<key>;EndpointSuffix=core.windows.net
```

## 🔧 MCP Tool Endpoints
Core operations exposed as HTTP functions (mapped to MCP tools):
- read_graph
- create_entities
- create_relations
- search_entities / search_relations
- add_observation
- update_entity
- delete_entity
- get_stats
- clear_memory
- merge_entities
- detect_duplicate_entities
- get_temporal_events
- execute_batch_operations
- get_user_stats
- search_relations_by_user

### Recommended Workflow
1. read_graph (baseline)  
2. search_entities (prevent duplicates)  
3. create_entities (upsert)  
4. create_relations (connect graph)  
5. add_observation (incremental enrichment)

Edge cases: missing entities auto-created during relation / observation operations.

## 📁 Directory Layout
```text
dotnet/CentralMemoryMcp.Functions/
├── Program.cs                  # Host bootstrap
├── ServiceRegistration.cs      # DI wiring
├── Functions/
│   ├── HealthFunctions.cs      # /api/health & /api/ready
│   ├── GraphFunctions.cs       # Tool endpoints
├── Services/
│   ├── KnowledgeGraphService.cs # Domain logic
├── Storage/
│   └── TableStorageService.cs   # Azure Table abstraction
├── Models/                     # DTOs / contracts
├── appsettings.json            # Local config
└── host.json                   # Functions host config
```

## 🧪 Validation
```bash
dotnet build
func start --port 7071 &
curl http://localhost:7071/api/health
curl http://localhost:7071/api/ready
```

## 📊 Logging & Telemetry
- Structured workspace-scoped logging
- Ready for Application Insights (add connection settings)

## 🪙 Roadmap
- Vector similarity enrichment
- Blob archival for large observation history
- Incremental graph export (JSON / NDJSON)
- Advanced duplicate resolution heuristics

## 📚 Documentation
See `docs/readme.md` (GitHub Pages index) and other docs in `docs/`:
- ARCHITECTURE.md
- API.md
- STORAGE.md
- DEPLOYMENT.md

## 🔒 Production Notes
- Prefer managed identity / Key Vault for credentials
- Enforce workspaceName validation for multi-tenant isolation
- Monitor health + readiness endpoints

## 📝 License
MIT License - see `LICENSE`.

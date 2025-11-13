# Architecture Guide (.NET Implementation)

## Overview
The Central Memory MCP Server is a serverless Azure Functions (.NET 10 isolated worker) application exposing Model Context Protocol (MCP) memory & knowledge graph operations. It stores entities, relations, observations, and statistics in Azure Table Storage with strong workspace isolation.

## High-Level Diagram
```text
┌──────────────┐   HTTP / MCP Tools   ┌─────────────────────┐   Table API   ┌────────────────────┐
│ AI Assistant │  ─────────────────▶  │ Azure Functions (.NET)│ ───────────▶ │ Azure Table Storage │
│ (Copilot MCP)│  ◀─────────────────  │ Graph / Health funcs │ ◀─────────── │  (Azurite local)    │
└──────────────┘                      └─────────────────────┘              └────────────────────┘
```

## Core Components
1. Functions Layer (`GraphFunctions`, `HealthFunctions`) – HTTP triggers for MCP tool endpoints and health/readiness checks.
2. Domain Service (`KnowledgeGraphService`) – Orchestrates entity/relation/observation/stat operations.
3. Storage Abstraction (`TableStorageService`) – Encapsulates Azure Table operations (upsert/search/delete/batch) using `workspaceName` as partition key.
4. Models (`GraphModels.cs`) – DTOs & contracts for graph entities, relations, requests, responses.
5. DI & Host Bootstrap (`Program.cs`, `ServiceRegistration.cs`) – Configures services, logging, configuration binding.

## Data Model
```csharp
public class EntityDto { 
    public string workspaceName { get; set; } 
    public string Name { get; set; } 
    public string EntityType { get; set; } 
    public List<string> Observations { get; set; } = new(); 
    public Dictionary<string, string>? Metadata { get; set; } 
}

public class RelationDto { 
    public string workspaceName { get; set; } 
    public string From { get; set; } 
    public string To { get; set; } 
    public string RelationType { get; set; } 
    public Dictionary<string, string>? Metadata { get; set; } 
}
```

## Workspace Isolation
- PartitionKey: `workspaceName`
- RowKey (Entities): sanitized entity name
- RowKey (Relations): composite key (from|relationType|to)
- Benefit: Natural multi-tenant separation and efficient partition scans.

## Operations Flow
1. Request hits HTTP trigger (mapped to MCP tool).
2. Input validated (workspaceName required; entity/relation fields sanitized).
3. Domain service invokes storage abstraction for CRUD/batch operations.
4. Storage layer executes Azure Table calls and transforms back to DTOs.
5. Response returned with minimal shape suited for LLM consumption.

## Batch & Performance
- Batching applied for multi-entity or multi-relation upserts respecting Azure Table batch limits per partition.
- Reuse of `TableClient` via DI reduces connection churn.
- Observations appended; large lists may be future candidates for archival (roadmap).

## Error Handling
- Validation errors: 400 with clear message & example.
- Storage errors: logged, surfaced as 500 minimal error object.
- NotFound semantics: delete operations idempotent.

## Logging
- Scoped logging per request/workspace for correlation.
- Structured fields: workspaceName, operation, counts, duration.

## Health & Readiness
- `/api/health`: basic process & configuration validation.
- `/api/ready`: storage dependency probe (table client reachable).

## Security & Configuration
- Local dev: `UseDevelopmentStorage=true` (Azurite).
- Production: Use managed identity or secure connection string (Key Vault retrieval recommended).
- Input sanitation prevents malformed Table keys.

## Future Enhancements
- Vector similarity enrichment layer.
- Blob archival for oversized observation histories.
- Temporal event indexing improvements.
- Advanced duplicate detection heuristics.

## Monitoring
- Integrate Application Insights via instrumentation key / connection string.
- Track: request latency, batch sizes, error counts.

## MCP Tool Surface Mapping
Each MCP tool corresponds to a public method in `KnowledgeGraphService` invoked by `GraphFunctions` (e.g. create_entities -> CreateEntitiesAsync).

## Deployment
- Build & publish via GitHub Actions (CI) using `dotnet publish` output.
- Azure Functions consumption or premium plan recommended for scale.

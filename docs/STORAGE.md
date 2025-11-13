# Storage Guide (Current Implementation)

## Overview

Alpha implementation persists entities and relations in Azure Table Storage. Workspace isolation via `WorkspaceName` (PartitionKey). Row keys are GUIDs (no reliance on names). Observations stored as a single delimited string (`||`).

## Tables

- entities
  - PartitionKey: WorkspaceName
  - RowKey: Entity Guid (N format)
  - Properties: Id, WorkspaceName, Name, EntityType, Observations ("||" joined), Metadata
- relations
  - PartitionKey: WorkspaceName
  - RowKey: Relation Guid (N format)
  - Properties: Id, WorkspaceName, FromEntityId, ToEntityId, RelationType, Metadata
- workspaces (reserved for future use)

## Example Entity Row

```
PartitionKey = demo
RowKey = 5f3a2e9d2c8f4a1e9d3b2c7a5e4f1d0a
Name = Alice
EntityType = Person
Observations = Software engineer||Loves Go
Metadata = {"dept":"Engineering"}
Id = 5f3a2e9d2c8f4a1e9d3b2c7a5e4f1d0a
```

## Upsert Strategy

1. Lookup existing entity by PartitionKey + Name (OData filter)
2. If found, reuse existing Id
3. Replace entire row (Replace mode)

## Relation Upsert

- Requires existing entity GUIDs or resolves names first
- Reuses relation Id if identical From/To/Type previously upserted (handled in service layer)

## Observations Format

Stored joined by `||`. On read split into list:

```csharp
string.Join("||", observations)
// Read: value.Split("||", RemoveEmptyEntries)
```

## Workspace Isolation

Single partition per workspace keeps queries simple; scalability acceptable for early stage. Future optimizations may introduce composite partitioning if hot partitions emerge.

## Configuration

Local:

```
AzureWebJobsStorage=UseDevelopmentStorage=true
```

Production example:

```
AzureWebJobsStorage=DefaultEndpointsProtocol=https;AccountName=<name>;AccountKey=<key>;EndpointSuffix=core.windows.net
```

Use managed identity / Key Vault for secretless access (future enhancement).

## Query Patterns

- Read graph: filter `PartitionKey eq '<workspace>'`
- Lookup by name: adds `and Name eq '<escaped>'`
- Relations from entity: filter by `PartitionKey` then client-side match (future: add FromEntityId indexed property query optimization)

## Error Handling

- Invalid payloads produce `{ success:false, message }`
- Missing referenced entity aborts relation upsert.

## Limitations / Roadmap

- No pagination (entire workspace scan)
- No secondary indexes (e.g., by type)
- No search by observation content
- No batch writes beyond per-entity upsert

Future improvements: batch APIs, observation archival, indexed queries, search entities by prefix/type, relation reverse lookups.

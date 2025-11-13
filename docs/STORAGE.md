# Storage Guide (.NET Implementation)

## Overview
Azure Table Storage is used for durable persistence of entities and relations with strict workspace isolation. Local development uses Azurite via `UseDevelopmentStorage=true`.

## Tables
Two logical tables (names may be configured):
- Entities Table
  - PartitionKey: workspaceName
  - RowKey: sanitized entity name
  - Properties: entityType, observations (JSON array), metadata (JSON object), createdAt, updatedAt
- Relations Table
  - PartitionKey: workspaceName
  - RowKey: from|relationType|to (sanitized)
  - Properties: from, to, relationType, metadata (JSON), createdAt, updatedAt

## Workspace Isolation
All queries scoped to a single partition (workspaceName). This enables efficient partition scans and natural multi-tenant separation.

## Configuration
Local:
```
AzureWebJobsStorage=UseDevelopmentStorage=true
```
Production example:
```
AzureWebJobsStorage=DefaultEndpointsProtocol=https;AccountName=<name>;AccountKey=<key>;EndpointSuffix=core.windows.net
```
Use managed identity or Key Vault for secure credential management.

## Operations
- Upsert Entities: append observations; merge metadata keys
- Upsert Relations: auto-create missing entities if needed
- Search: partition-filtered queries with RowKey / property filters
- Delete Entity: removes entity + related relations
- Clear Workspace: deletes all partition rows (iterative scan)

## Batching & Performance
- Batch limit: 100 operations per partition per Azure Table batch
- Large upserts segmented automatically
- Reuse of TableClient reduces connection overhead

## Data Shape (C# DTO excerpt)
```csharp
public class EntityDto {
  public string workspaceName { get; set; } = default!;
  public string Name { get; set; } = default!;
  public string EntityType { get; set; } = default!;
  public List<string> Observations { get; set; } = new();
  public Dictionary<string,string>? Metadata { get; set; }
}

public class RelationDto {
  public string workspaceName { get; set; } = default!;
  public string From { get; set; } = default!;
  public string To { get; set; } = default!;
  public string RelationType { get; set; } = default!;
  public Dictionary<string,string>? Metadata { get; set; }
}
```

## Error Handling
- Validation: reject empty names / types
- Storage: transient errors logged; may retry (future enhancement)
- Idempotency: delete & upsert safe to repeat

## Monitoring
Integrate Application Insights for dependency tracking and custom metrics (batch size, latency, error counts).

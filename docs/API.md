# API Reference (MCP Tools) – .NET Implementation

## Overview
HTTP-triggered Azure Functions expose MCP tooling for knowledge graph operations. All endpoints require `workspaceName` for isolation. Responses are optimized for LLM consumption (concise, structured, minimal nesting).

## Entity Operations
### create_entities
Upserts one or more entities (existing entities append new observations; metadata merged by key).
Parameters:
- workspaceName (string)
- entities (array|object) – single entity or array
Entity shape:
```json
{
  "name": "Alice",
  "entityType": "Person",
  "observations": ["Software engineer"],
  "metadata": {"dept": "Engineering"}
}
```

### add_observation
Append a single observation to an entity (auto-creates entity if missing).
Parameters:
- workspaceName
- entityName
- observation (string)
- entityType (optional if creating implicitly)

### update_entity
Replace metadata keys and optionally append observations.
Parameters:
- workspaceName
- entityName
- observations (array, optional)
- metadata (object, optional)

### delete_entity
Removes entity and its relations (idempotent).
Parameters:
- workspaceName
- entityName

### search_entities
Fuzzy search by partial name or filter by entityType.
Parameters:
- workspaceName
- name (optional)
- entityType (optional)

## Relation Operations
### create_relations
Upserts one or more relations (auto-creates missing entities).
Parameters:
- workspaceName
- relations (array|object) – relation shape below
Relation shape:
```json
{
  "from": "Alice",
  "to": "Central Memory Server",
  "relationType": "worksOn",
  "metadata": {"since": "2025"}
}
```

### search_relations
Filter relations by from, to, relationType.
Parameters:
- workspaceName
- from (optional)
- to (optional)
- relationType (optional)

### search_relations_by_user
Filter relations with metadata user context.
Parameters:
- workspaceName
- userId (optional)
- relationType (optional)

## Graph Operations
### read_graph
Returns all entities + relations for workspace.
Parameters:
- workspaceName
Response:
```json
{
  "entities": [...],
  "relations": [...]
}
```

### clear_memory
Deletes all entities and relations for workspace.
Parameters:
- workspaceName

## Statistics & Temporal
### get_stats
Aggregated counts and distribution.
Response example:
```json
{
  "totalEntities": 5,
  "totalRelations": 3,
  "entityTypes": {"Person":3,"Project":2},
  "relationTypes": {"worksOn":2},
  "workspaceName": "my-project"
}
```

### get_user_stats
User-specific aggregated view (if user metadata tracked).
Parameters:
- workspaceName
- userId (optional)

### get_temporal_events
Return entities & relations created/updated in time range.
Parameters:
- workspaceName
- startTime (ISO 8601)
- endTime (ISO 8601)
- entityName (optional)
- relationType (optional)

## Advanced
### detect_duplicate_entities
Heuristic similarity across names & observations.
Parameters:
- workspaceName
- threshold (float 0-1, default 0.8)

### merge_entities
Merge sources into target combining observations & metadata.
Parameters:
- workspaceName
- targetEntityName
- sourceEntityNames (array)
- mergeStrategy ("combine" | "replace"; default combine)

### execute_batch_operations
Execute mixed entity/relation operations.
Parameters:
- workspaceName
- operations (array)
Operation item:
```json
{
  "type": "create_entity|create_relation|delete_entity",
  "data": { /* shape depends on type */ }
}
```

## Error Model
Errors return:
```json
{
  "error": "ValidationError",
  "message": "Entity name is required",
  "details": {"field":"name"}
}
```
Common types: ValidationError, NotFoundError, ConflictError, StorageError.

## Conventions
- All string inputs trimmed; empty -> validation error.
- Observations appended; duplicates allowed (LLM may reassert facts).
- Metadata merged shallow (key overwrite).
- Idempotent deletes & upserts.

## Limits & Performance
- Batch size tuned to Azure Table 100 entity limit per partition.
- Typical latency < 100ms for single operations (local).
- Large graph reads may paginate in future roadmap.

## Authentication
Current implementation assumes trusted environment; add Azure AD / API key layer for production if exposed publicly.

## Versioning
Tool set considered v1; additive changes will extend without breaking existing shapes.

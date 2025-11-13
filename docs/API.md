# API Reference (Current Implemented MCP Tools)

Alpha implementation exposes four MCP tools plus health endpoints.

## Tools Summary

- read_graph: Returns all entities and relations for a workspace.
- upsert_entity: Creates or updates an entity by name within a workspace.
- upsert_relation: Creates or updates a relation between two existing entities.
- get_entity_relations: Lists all relations originating from a given entity.

(Health endpoints `/api/health`, `/api/ready` are standard HTTP, not MCP tools.)

## Common Parameter

- workspaceName (string, required) â€“ partition key for isolation.

## Entity Model Response Shape

```json
{
  "workspaceName": "demo",
  "name": "Alice",
  "entityType": "Person",
  "observations": ["Software engineer"],
  "metadata": "{\"dept\":\"Engineering\"}",
  "id": "<guid>"
}
```

Observations persisted internally joined by "||" delimiter; returned as list.

## read_graph

Returns all entities and relations.
Parameters:

- workspaceName

Response:

```json
{
  "workspaceName": "demo",
  "entities": [ {"name":"Alice"}, {"name":"ProjectX"} ],
  "relations": [ {"relationType":"works_on"} ]
}
```

## upsert_entity

Create or update an entity (matched by name within workspace).
Request body shape:

```json
{
  "workspaceName": "demo",
  "name": "Alice",
  "entityType": "Person",
  "observations": ["Software engineer"],
  "metadata": "{\"dept\":\"Engineering\"}"
}
```

Response:

```json
{ "success": true, "id": "<guid>", "workspace": "demo", "name": "Alice" }
```

Failure example:

```json
{ "success": false, "message": "Invalid entity payload. Require workspaceName, name and entityType." }
```

## upsert_relation

Creates or updates a relation between two entities.
Parameters body:

```json
{
  "workspaceName": "demo",
  "fromEntityId": "<guid>",
  "toEntityId": "<guid>",
  "relationType": "works_with",
  "metadata": "{\"since\":\"2025\"}"
}
```

Alternatively supply legacy names:

```json
{
  "workspaceName": "demo",
  "from": "Alice",
  "to": "ProjectX",
  "relationType": "works_on"
}
```

Response:

```json
{ "success": true, "relationId": "<guid>", "workspace": "demo", "fromEntityId": "<guid>", "toEntityId": "<guid>", "relationType": "works_on" }
```

Errors:

- Source entity 'X' not found.
- Target entity 'Y' not found.
- Invalid relation payload. Provide fromEntityId/toEntityId or from/to names that exist.

## get_entity_relations

List all relations from a given entity.
Parameters:

- workspaceName (string)
- entityId (GUID preferred) OR entityName (string)

Response:

```json
{
  "success": true,
  "workspaceName": "demo",
  "entityId": "<guid>",
  "relations": [ {"relationType": "works_on", "toEntityId": "<guid>"} ]
}
```

Failure:

```json
{ "success": false, "message": "Entity 'Alice' not found in workspace 'demo'." }
```

## Error Format

Errors returned uniformly:

```json
{ "success": false, "message": "<reason>" }
```
Common types: ValidationError, NotFoundError, ConflictError, StorageError.

## Not Implemented (Roadmap)

search_entities, search_relations, stats, temporal events, batch operations, merge/detect duplicates, user-specific metadata features.

## Authentication

Currently none (trusted dev environment). Add Azure AD / API key layer before external exposure.

## Versioning

Tool set considered v0 (alpha). Additions will be non-breaking by extending responses.

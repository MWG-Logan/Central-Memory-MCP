# Central Memory MCP Server (.NET 10 / Azure Functions)

Model Context Protocol (MCP) memory server built with Azure Functions (.NET 10, C# 14, isolated worker) providing persistent knowledge graph storage (Entities + Relations) for AI assistants.

**ALWAYS reference these instructions first. Only fall back to ad‑hoc search or shell commands if something here is contradicted by current code.**

---

## Technology Stack (Current Rewrite)

- .NET 10 (preview) / C# 14
- Azure Functions isolated worker model
- Azure Functions MCP Extension (McpToolTrigger / McpToolProperty / ToolInvocationContext)
- Azure Table Storage (Entities, Relations, Workspaces tables)
- Azurite (local emulator) for development
- Idempotent upsert logic for Entities and Relations (no duplicates by logical key)

---

## Storage Schema (Current)

### Tables

- `entities`: PartitionKey = WorkspaceName, RowKey = Guid (entity Id as N format)
- `relations`: PartitionKey = WorkspaceName, RowKey = Guid (relation Id as N format)
- `workspaces`: PartitionKey = `workspaces`, RowKey = workspace name (lowercase)

### Entities

- Unique per (WorkspaceName + Name) – Upsert reuses existing Id.
- Stored fields: Id, Name, EntityType, Observations ("||" delimited), Metadata.

### Relations

- Unique per (WorkspaceName + FromEntityId + ToEntityId + RelationType) – Upsert reuses existing Id.
- Stored fields: Id, FromEntityId, ToEntityId, RelationType, Metadata.
- Upsert accepts either GUIDs (`fromEntityId` / `toEntityId`) or legacy names (`from` / `to`) which are resolved server‑side.

---

## Key Azure Functions (MCP Tools)

Defined in `Functions/GraphFunctions.cs` using MCP attributes:

- `read_graph` – Return all entities + relations for a workspace.
- `upsert_entity` – Create/update entity (idempotent by name).
- `upsert_relation` – Create/update relation (idempotent by logical key).
- `get_entity_relations` – List relations originating from an entity (accepts `entityId` or legacy `entityName`).

Health / readiness endpoints: `HealthFunctions` (`/api/health`, `/api/ready`).

---

## Working Effectively

### 1. Bootstrap (Restore Dependencies)

```bash
# From solution root
dotnet restore
```

(Time: ~2–5 seconds. NEVER CANCEL.)

### 2. Build

```bash
dotnet build --no-restore -c Debug
```

(Time: Usually <10 seconds. NEVER CANCEL.)

Optional:

```bash
dotnet build -c Release
```

### 3. Run Locally (Azure Functions Runtime)

Prerequisites:

- Azure Functions Core Tools v4
- Azurite (recommend VS Code extension or npm global)

Start Azurite (if not already running):

```bash
# VS Code extension (preferred) OR command line:
azurite --location ./azurite --debug
```

Set required environment variable (PowerShell example):

```powershell
$env:AzureWebJobsStorage = "UseDevelopmentStorage=true"
```

Start Functions host (from project directory containing .csproj):

```bash
func start --port 7071 --verbose
```

First run may take 30+ seconds. NEVER CANCEL.

### 4. Container Alternative (Firewall / Network Restrictions)

If Functions Core Tools or Azurite cannot be installed:

1. Document limitation: `Azure Functions Core Tools installation fails due to firewall restrictions.`
2. Use Dockerfile (if provided) pattern:

   ```bash
   docker build -t central-memory-mcp-dotnet .   # May take 10+ minutes (do not cancel)
   docker run -p 7071:7071 central-memory-mcp-dotnet
   ```

3. Validate via health endpoints (see Validation section).

---

## MCP Tool Invocation Examples

Use object payloads (properties, not raw JSON strings). Examples:

### Upsert Entity

```jsonc
{
  "workspaceName": "DummyTest",
  "name": "Logan Cook",
  "entityType": "Person",
  "observations": ["Example observation"],
  "metadata": "{\"role\":\"developer\"}"
}
```

### Upsert Relation (names only)

```jsonc
{
  "workspaceName": "DummyTest",
  "from": "Logan Cook",
  "to": "Visual Studio Code",
  "relationType": "uses"
}
```

### Upsert Relation (GUIDs)

```jsonc
{
  "workspaceName": "DummyTest",
  "fromEntityId": "bb3387d342434f28815a57c272d08d28",
  "toEntityId": "a1ac3089dc4b41d7b2fdf537f219127c",
  "relationType": "uses",
  "metadata": "{\"confidence\":0.95}"
}
```

### Get Entity Relations

```jsonc
{ "workspaceName": "DummyTest", "entityName": "Logan Cook" }
```

OR

```jsonc
{ "workspaceName": "DummyTest", "entityId": "bb3387d342434f28815a57c272d08d28" }
```

### Read Graph

```jsonc
{ "workspaceName": "DummyTest" }
```

---

## Validation Workflow (MANDATORY After Changes)

1. **Build**

   ```bash
   dotnet build
   ```

2. **Start Host (if available)**

   ```bash
   func start --port 7071
   ```

3. **Health Checks**

   ```bash
   curl http://localhost:7071/api/health
   curl http://localhost:7071/api/ready
   ```

   Expect JSON or text indicating healthy/ready.

4. **MCP Tool Smoke Test** (from client / VS Code Copilot Chat)

   - Create entity (`upsert_entity`)
   - Create relation (`upsert_relation`)
   - Read graph (`read_graph`)
   - List relations (`get_entity_relations`)

5. **Idempotency Checks**

   - Upsert same entity name: Id unchanged.
   - Upsert same relation (workspace + from + to + type): relationId unchanged.

6. **Special Characters / Unicode**

   - Names with spaces, punctuation, Unicode should persist (RowKey uses GUID, eliminating reserved char issues).

---

## Project Structure (.NET)

```text
CentralMemoryMcp.Functions/
  Program.cs                # Host bootstrap + DI registrations
  local.settings.json       # Local Azure Functions config
  Functions/
    GraphFunctions.cs       # MCP tool endpoints
    HealthFunctions.cs      # /api/health & /api/ready
  Models/
    GraphModels.cs          # EntityModel, RelationModel, WorkspaceModel
  Services/
    KnowledgeGraphService.cs # Entity logic (idempotent upsert)
    RelationService.cs        # Relation logic (idempotent upsert)
    TableStorageService.cs    # Table access abstraction
  docs/ (architecture, API, storage, deployment)
```

---

## Dependency Injection Summary

Registered in `Program.cs`:

- `TableServiceClient` (from AzureWebJobsStorage)
- `ITableStorageService` → `TableStorageService`
- `IKnowledgeGraphService` → `KnowledgeGraphService`
- `IRelationService` → `RelationService`

Ensure `AzureWebJobsStorage` is set before starting the host.

---

## Environment Variables

Development:

```bash
AzureWebJobsStorage=UseDevelopmentStorage=true
```

Production (example):

```bash
AzureWebJobsStorage=DefaultEndpointsProtocol=https;AccountName=<name>;AccountKey=<key>;EndpointSuffix=core.windows.net
```

---

## Performance & Scaling Notes

- Partition strategy uses workspace name for both entities and relations (simplifies workspace isolation & scanning).
- RowKey uses GUID (N format) reducing risk of reserved character collisions and improves distribution.
- Idempotent upsert reduces table growth from duplicates.
- For very large workspaces consider eventual introduction of sub‑partitioning (e.g., sharded prefix) if hot partitions emerge.

---

## Troubleshooting

| Issue | Action |
|-------|--------|
| Functions host slow start | Wait; do not cancel. First load may JIT assemblies. |
| Duplicate entities still appear | Confirm you updated by exact name (case sensitive) – names are matched with original casing but stored separately. |
| Relation duplicates | Verify same GUIDs or names map to same entity IDs and same RelationType casing. |
| Azurite not available | Document limitation, switch to Docker path. |
| `AzureWebJobsStorage` missing | Set env var; restart host. |
| MCP tool missing property error | Provide required tool properties; check `GraphFunctions` attributes for names. |

---

## Contribution Guidelines (Quick)

1. Make code change.
2. `dotnet build` (NEVER CANCEL).
3. Run local host if possible and validate health + tools.
4. Update docs if schema / behavior changes (especially this file + `docs/STORAGE.md`, `docs/API.md`).
5. Keep changes consistent with idempotent semantics.

---

## Quick Command Reference

```bash
# Restore & build
dotnet restore && dotnet build
# Run Functions
func start --port 7071
# Health checks
curl http://localhost:7071/api/health
curl http://localhost:7071/api/ready
```

---

## Critical Timing (Updated)

- `dotnet restore`: ~2–5s
- `dotnet build`: ~5–10s
- `func start` first run: 30–60s (do NOT cancel)

**NEVER CANCEL BUILDS OR LONG‑RUNNING STARTUPS** – This can corrupt the local Functions host state.

---

## Always Re‑Validate After Schema Changes

Run: build → health → sample MCP tool flows → confirm idempotency → review Table Storage data integrity.

---

**End of Instructions – This file is authoritative for .NET version.**
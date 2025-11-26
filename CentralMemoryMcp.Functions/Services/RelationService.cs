using Azure;
using Azure.Data.Tables;
using CentralMemoryMcp.Functions.Models;
using CentralMemoryMcp.Functions.Storage;

namespace CentralMemoryMcp.Functions.Services;

public interface IRelationService
{
    Task<RelationModel> UpsertRelationAsync(RelationModel model, CancellationToken ct = default);
    Task<RelationModel?> GetRelationAsync(string workspaceName, Guid relationId, CancellationToken ct = default);
    Task<List<RelationModel>> GetRelationsFromEntityAsync(string workspaceName, Guid fromEntityId, CancellationToken ct = default);
    Task<List<RelationModel>> GetRelationsForWorkspaceAsync(string workspaceName, CancellationToken ct = default);
    Task DeleteRelationAsync(string workspaceName, Guid relationId, CancellationToken ct = default);
}

public class RelationService(ITableStorageService storage) : IRelationService
{
    public async Task<RelationModel> UpsertRelationAsync(RelationModel model, CancellationToken ct = default)
    {
        var table = await storage.GetRelationsTableAsync(ct);
        // Check for existing relation (same workspace, from, to, type)
        var filter = $"PartitionKey eq '{model.WorkspaceName}' and FromEntityId eq '{model.FromEntityId:N}' and ToEntityId eq '{model.ToEntityId:N}' and RelationType eq '{EscapeFilterValue(model.RelationType)}'";
        await foreach(var e in table.QueryAsync<TableEntity>(filter: filter, maxPerPage:1, cancellationToken: ct))
        {
            // Reuse its Id
            if (e.TryGetValue("Id", out var idObj) && idObj is string idStr && Guid.TryParse(idStr, out var existingId))
            {
                model.Id = existingId;
            }
            break;
        }
        var entity = new TableEntity(model.PartitionKey, model.RowKey)
        {
            {"Id", model.Id.ToString("N")},
            {"WorkspaceName", model.WorkspaceName},
            {"FromEntityId", model.FromEntityId.ToString("N")},
            {"ToEntityId", model.ToEntityId.ToString("N")},
            {"RelationType", model.RelationType},
            {"Metadata", model.Metadata ?? string.Empty}
        };
        await table.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);
        return model;
    }

    public async Task<RelationModel?> GetRelationAsync(string workspaceName, Guid relationId, CancellationToken ct = default)
    {
        var table = await storage.GetRelationsTableAsync(ct);
        try
        {
            var response = await table.GetEntityAsync<TableEntity>(workspaceName, relationId.ToString("N"), cancellationToken: ct);
            var model = new RelationModel(
                response.Value.GetString("WorkspaceName")!,
                Guid.Parse(response.Value.GetString("FromEntityId")!),
                Guid.Parse(response.Value.GetString("ToEntityId")!),
                response.Value.GetString("RelationType")!,
                response.Value.GetString("Metadata"))
            {
                Id = relationId
            };
            return model;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<List<RelationModel>> GetRelationsFromEntityAsync(string workspaceName, Guid fromEntityId, CancellationToken ct = default)
    {
        var table = await storage.GetRelationsTableAsync(ct);
        var results = new List<RelationModel>();
        var fromIdStr = fromEntityId.ToString("N");
        await foreach (var e in table.QueryAsync<TableEntity>(
            filter: $"PartitionKey eq '{workspaceName}' and FromEntityId eq '{fromIdStr}'",
            cancellationToken: ct))
        {
            var relationId = Guid.TryParse(e.GetString("Id"), out var rid) ? rid : Guid.NewGuid();
            var model = new RelationModel(
                e.GetString("WorkspaceName")!,
                Guid.Parse(e.GetString("FromEntityId")!),
                Guid.Parse(e.GetString("ToEntityId")!),
                e.GetString("RelationType")!,
                e.GetString("Metadata"));
            model.Id = relationId;
            results.Add(model);
        }
        return results;
    }

    public async Task<List<RelationModel>> GetRelationsForWorkspaceAsync(string workspaceName, CancellationToken ct = default)
    {
        var table = await storage.GetRelationsTableAsync(ct);
        var results = new List<RelationModel>();
        await foreach (var e in table.QueryAsync<TableEntity>(
            filter: $"PartitionKey eq '{workspaceName}'",
            cancellationToken: ct))
        {
            var relationId = Guid.TryParse(e.GetString("Id"), out var rid) ? rid : Guid.NewGuid();
            var model = new RelationModel(
                e.GetString("WorkspaceName")!,
                Guid.Parse(e.GetString("FromEntityId")!),
                Guid.Parse(e.GetString("ToEntityId")!),
                e.GetString("RelationType")!,
                e.GetString("Metadata"));
            model.Id = relationId;
            results.Add(model);
        }
        return results;
    }

    public async Task DeleteRelationAsync(string workspaceName, Guid relationId, CancellationToken ct = default)
    {
        var table = await storage.GetRelationsTableAsync(ct);
        try
        {
            await table.DeleteEntityAsync(workspaceName, relationId.ToString("N"), cancellationToken: ct);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // not found; ignore
        }
    }

    private static string EscapeFilterValue(string value) => value.Replace("'", "''");
}
using Azure;
using Azure.Data.Tables;
using CentralMemoryMcp.Functions.Models;
using CentralMemoryMcp.Functions.Storage;

namespace CentralMemoryMcp.Functions.Services;

public interface IRelationService
{
    Task<RelationModel> UpsertRelationAsync(RelationModel model, CancellationToken ct = default);
    Task<RelationModel?> GetRelationAsync(string workspaceName, Guid relationId, CancellationToken ct = default);
    Task<List<RelationModel>> GetRelationsFromEntityAsync(string workspaceName, Guid fromEntityId, CancellationToken ct = default);
    Task<List<RelationModel>> GetRelationsForWorkspaceAsync(string workspaceName, CancellationToken ct = default);
    Task DeleteRelationAsync(string workspaceName, Guid relationId, CancellationToken ct = default);
}

public class RelationService(ITableStorageService storage) : IRelationService
{
    public async Task<RelationModel> UpsertRelationAsync(RelationModel model, CancellationToken ct = default)
    {
        var table = await storage.GetRelationsTableAsync(ct);
        // Check for existing relation (same workspace, from, to, type)
        var filter = $"PartitionKey eq '{model.WorkspaceName}' and FromEntityId eq '{model.FromEntityId:N}' and ToEntityId eq '{model.ToEntityId:N}' and RelationType eq '{EscapeFilterValue(model.RelationType)}'";
        await foreach(var e in table.QueryAsync<TableEntity>(filter: filter, maxPerPage:1, cancellationToken: ct))
        {
            // Reuse its Id
            if (e.TryGetValue("Id", out var idObj) && idObj is string idStr && Guid.TryParse(idStr, out var existingId))
            {
                model.Id = existingId;
            }
            break;
        }
        var entity = new TableEntity(model.PartitionKey, model.RowKey)
        {
            {"Id", model.Id.ToString("N")},
            {"WorkspaceName", model.WorkspaceName},
            {"FromEntityId", model.FromEntityId.ToString("N")},
            {"ToEntityId", model.ToEntityId.ToString("N")},
            {"RelationType", model.RelationType},
            {"Metadata", model.Metadata ?? string.Empty}
        };
        await table.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);
        return model;
    }

    public async Task<RelationModel?> GetRelationAsync(string workspaceName, Guid relationId, CancellationToken ct = default)
    {
        var table = await storage.GetRelationsTableAsync(ct);
        try
        {
            var response = await table.GetEntityAsync<TableEntity>(workspaceName, relationId.ToString("N"), cancellationToken: ct);
            var model = new RelationModel(
                response.Value.GetString("WorkspaceName")!,
                Guid.Parse(response.Value.GetString("FromEntityId")!),
                Guid.Parse(response.Value.GetString("ToEntityId")!),
                response.Value.GetString("RelationType")!,
                response.Value.GetString("Metadata"))
            {
                Id = relationId
            };
            return model;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<List<RelationModel>> GetRelationsFromEntityAsync(string workspaceName, Guid fromEntityId, CancellationToken ct = default)
    {
        var table = await storage.GetRelationsTableAsync(ct);
        var results = new List<RelationModel>();
        var fromIdStr = fromEntityId.ToString("N");
        await foreach (var e in table.QueryAsync<TableEntity>(
            filter: $"PartitionKey eq '{workspaceName}' and FromEntityId eq '{fromIdStr}'",
            cancellationToken: ct))
        {
            var relationId = Guid.TryParse(e.GetString("Id"), out var rid) ? rid : Guid.NewGuid();
            var model = new RelationModel(
                e.GetString("WorkspaceName")!,
                Guid.Parse(e.GetString("FromEntityId")!),
                Guid.Parse(e.GetString("ToEntityId")!),
                e.GetString("RelationType")!,
                e.GetString("Metadata"));
            model.Id = relationId;
            results.Add(model);
        }
        return results;
    }

    public async Task<List<RelationModel>> GetRelationsForWorkspaceAsync(string workspaceName, CancellationToken ct = default)
    {
        var table = await storage.GetRelationsTableAsync(ct);
        var results = new List<RelationModel>();
        await foreach (var e in table.QueryAsync<TableEntity>(
            filter: $"PartitionKey eq '{workspaceName}'",
            cancellationToken: ct))
        {
            var relationId = Guid.TryParse(e.GetString("Id"), out var rid) ? rid : Guid.NewGuid();
            var model = new RelationModel(
                e.GetString("WorkspaceName")!,
                Guid.Parse(e.GetString("FromEntityId")!),
                Guid.Parse(e.GetString("ToEntityId")!),
                e.GetString("RelationType")!,
                e.GetString("Metadata"));
            model.Id = relationId;
            results.Add(model);
        }
        return results;
    }

    public async Task DeleteRelationAsync(string workspaceName, Guid relationId, CancellationToken ct = default)
    {
        var table = await storage.GetRelationsTableAsync(ct);
        try
        {
            await table.DeleteEntityAsync(workspaceName, relationId.ToString("N"), cancellationToken: ct);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // not found; ignore
        }
    }

    private static string EscapeFilterValue(string value) => value.Replace("'", "''");
}

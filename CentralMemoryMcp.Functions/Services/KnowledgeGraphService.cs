using Azure;
using Azure.Data.Tables;
using CentralMemoryMcp.Functions.Models;

namespace CentralMemoryMcp.Functions;

public interface IKnowledgeGraphService
{
    Task<EntityModel> UpsertEntityAsync(EntityModel model, CancellationToken ct = default);
    Task<EntityModel?> GetEntityAsync(string workspaceName, string name, CancellationToken ct = default);
    Task<List<EntityModel>> ReadGraphAsync(string workspaceName, CancellationToken ct = default);
}

public class KnowledgeGraphService(ITableStorageService storage) : IKnowledgeGraphService
{
    public async Task<EntityModel> UpsertEntityAsync(EntityModel model, CancellationToken ct = default)
    {
        var table = await storage.GetEntitiesTableAsync(ct);
        // Try to find existing by name
        var existing = await GetEntityAsync(model.WorkspaceName, model.Name, ct);
        if (existing is not null)
        {
            // Reuse existing Id
            model.Id = existing.Id;
        }
        var entity = new TableEntity(model.PartitionKey, model.RowKey)
        {
            {"Id", model.Id.ToString("N")},
            {"WorkspaceName", model.WorkspaceName},
            {"Name", model.Name},
            {"EntityType", model.EntityType},
            {"Observations", string.Join("||", model.Observations)},
            {"Metadata", model.Metadata ?? string.Empty}
        };
        await table.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);
        return model;
    }

    public async Task<EntityModel?> GetEntityAsync(string workspaceName, string name, CancellationToken ct = default)
    {
        var table = await storage.GetEntitiesTableAsync(ct);
        await foreach (var e in table.QueryAsync<TableEntity>(
            filter: $"PartitionKey eq '{workspaceName}' and Name eq '{EscapeFilterValue(name)}'",
            maxPerPage: 1,
            cancellationToken: ct))
        {
            var model = new EntityModel(
                workspaceName,
                e.GetString("Name")!,
                e.GetString("EntityType")!,
                e.GetString("Observations")!.Split("||", StringSplitOptions.RemoveEmptyEntries).ToList(),
                e.GetString("Metadata"));
            if (e.TryGetValue("Id", out var idObj) && idObj is string idStr && Guid.TryParse(idStr, out var parsed))
            {
                model.Id = parsed; // internal setter
            }
            return model;
        }
        return null;
    }

    public async Task<List<EntityModel>> ReadGraphAsync(string workspaceName, CancellationToken ct = default)
    {
        var table = await storage.GetEntitiesTableAsync(ct);
        var results = new List<EntityModel>();
        await foreach (var e in table.QueryAsync<TableEntity>(
            filter: $"PartitionKey eq '{workspaceName}'",
            cancellationToken: ct))
        {
            var model = new EntityModel(
                workspaceName,
                e.GetString("Name")!,
                e.GetString("EntityType")!,
                e.GetString("Observations")!.Split("||", StringSplitOptions.RemoveEmptyEntries).ToList(),
                e.GetString("Metadata"));
            if (e.TryGetValue("Id", out var idObj) && idObj is string idStr && Guid.TryParse(idStr, out var parsed))
            {
                model.Id = parsed;
            }
            results.Add(model);
        }
        return results;
    }

    private static string EscapeFilterValue(string value)
    {
        // Escape single quotes by doubling them per OData filter rules
        return value.Replace("'", "''");
    }
}

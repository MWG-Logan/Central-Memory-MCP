using Azure;
using Azure.Data.Tables;

namespace CentralMemoryMcp.Functions
{
    public interface ITableStorageService
    {
        Task<TableClient> GetEntitiesTableAsync(CancellationToken ct = default);
        Task<TableClient> GetRelationsTableAsync(CancellationToken ct = default);
        Task<TableClient> GetWorkspacesTableAsync(CancellationToken ct = default);
    }

    public class TableStorageService : ITableStorageService
    {
        private readonly TableServiceClient _serviceClient;
        private const string EntitiesTableName = "entities";
        private const string RelationsTableName = "relations";
        private const string WorkspacesTableName = "workspaces";

        public TableStorageService(TableServiceClient serviceClient) => _serviceClient = serviceClient;

        public async Task<TableClient> GetEntitiesTableAsync(CancellationToken ct = default)
        {
            var client = _serviceClient.GetTableClient(EntitiesTableName);
            await client.CreateIfNotExistsAsync(ct);
            return client;
        }

        public async Task<TableClient> GetRelationsTableAsync(CancellationToken ct = default)
        {
            var client = _serviceClient.GetTableClient(RelationsTableName);
            await client.CreateIfNotExistsAsync(ct);
            return client;
        }

        public async Task<TableClient> GetWorkspacesTableAsync(CancellationToken ct = default)
        {
            var client = _serviceClient.GetTableClient(WorkspacesTableName);
            await client.CreateIfNotExistsAsync(ct);
            return client;
        }
    }
}

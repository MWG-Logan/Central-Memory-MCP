using Azure.Identity;
using Azure.Data.Tables;

namespace CentralMemoryMcp.Functions.Storage
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

        public TableStorageService()
        {
            var conn = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            if (!string.IsNullOrWhiteSpace(conn))
            {
                _serviceClient = new TableServiceClient(conn);
                return;
            }

            var endpoint = Environment.GetEnvironmentVariable("AzureWebJobsStorage__tableServiceUri")
                           ?? throw new InvalidOperationException("AzureWebJobsStorage is not configured for managed identity.");
            _serviceClient = new TableServiceClient(new Uri(endpoint), new DefaultAzureCredential());
        }

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

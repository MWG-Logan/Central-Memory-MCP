namespace CentralMemoryMcp.Functions.Models
{
    /// <summary>
    /// Entity in the knowledge graph.
    /// Table: entities
    /// PartitionKey: WorkspaceName (groups all entities in a workspace)
    /// RowKey: Id (Guid) - stable, avoids special character issues in names
    /// </summary>
    public record EntityModel(string WorkspaceName, string Name, string EntityType, List<string> Observations, string? Metadata = null)
    {
        public Guid Id { get; internal set; } = Guid.NewGuid();
        public string PartitionKey => WorkspaceName;
        public string RowKey => Id.ToString("N"); // compact guid without dashes
    }

    /// <summary>
    /// Relation between entities in the knowledge graph.
    /// Table: relations
    /// PartitionKey: WorkspaceName (groups all relations in a workspace for partition scalability)
    /// RowKey: Id (Guid) - stable unique relation identifier
    /// </summary>
    public record RelationModel(string WorkspaceName, Guid FromEntityId, Guid ToEntityId, string RelationType, string? Metadata = null)
    {
        public Guid Id { get; internal set; } = Guid.NewGuid();
        public string PartitionKey => WorkspaceName;
        public string RowKey => Id.ToString("N");
    }

    /// <summary>
    /// Workspace metadata.
    /// Table: workspaces
    /// PartitionKey: "workspaces" (all workspaces in single partition for listing)
    /// RowKey: WorkspaceName (unique workspace identifier)
    /// </summary>
    public record WorkspaceModel(string Name, string? Description = null, DateTime? CreatedAt = null)
    {
        public Guid Id { get; internal set; } = Guid.NewGuid();
        public string PartitionKey => "workspaces";
        public string RowKey => Name.ToLowerInvariant();
    }
}

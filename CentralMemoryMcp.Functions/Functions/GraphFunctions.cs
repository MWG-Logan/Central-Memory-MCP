using CentralMemoryMcp.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using System.ComponentModel;
using System; // for Guid

namespace CentralMemoryMcp.Functions.Functions;

public class GraphFunctions
{
    private readonly IKnowledgeGraphService _graph;
    private readonly IRelationService _relations;
    
    public GraphFunctions(IKnowledgeGraphService graph, IRelationService relations)
    {
        _graph = graph;
        _relations = relations;
    }

    [Function(nameof(ReadGraph))]
    public async Task<object> ReadGraph(
        [McpToolTrigger("read_graph", "Reads the entire knowledge graph (entities and relations) for a workspace.")] 
        ToolInvocationContext context,
        [McpToolProperty("workspaceName", "The unique identifier of the workspace.", isRequired: true)]
        string workspaceName)
    {
        var entities = await _graph.ReadGraphAsync(workspaceName);
        var relations = await _relations.GetRelationsForWorkspaceAsync(workspaceName);
        
        return new
        {
            WorkspaceName = workspaceName,
            Entities = entities,
            Relations = relations
        };
    }

    [Function(nameof(UpsertEntity))]
    public async Task<object> UpsertEntity(
        [McpToolTrigger("upsert_entity", "Creates or updates an entity in the knowledge graph.")] 
        UpsertEntityRequest request,
        ToolInvocationContext context)
    {
        if (string.IsNullOrWhiteSpace(request.WorkspaceName) || 
            string.IsNullOrWhiteSpace(request.Name) || 
            string.IsNullOrWhiteSpace(request.EntityType))
        {
            return new { success = false, message = "Invalid entity payload. Require workspaceName, name and entityType." };
        }
        
        var model = new EntityModel(
            request.WorkspaceName, 
            request.Name, 
            request.EntityType, 
            request.Observations ?? [], 
            request.Metadata);
        
        model = await _graph.UpsertEntityAsync(model); // capture potentially reused Id
        return new { success = true, id = model.Id, workspace = model.WorkspaceName, name = model.Name };
    }

    [Function(nameof(UpsertRelation))]
    public async Task<object> UpsertRelation(
        [McpToolTrigger("upsert_relation", "Creates or updates a relation between two entities in the knowledge graph.")] 
        UpsertRelationRequest request,
        ToolInvocationContext context)
    {
        if (string.IsNullOrWhiteSpace(request.WorkspaceName) || 
            string.IsNullOrWhiteSpace(request.RelationType))
        {
            return new { success = false, message = "Invalid relation payload. Require workspaceName and relationType." };
        }

        Guid fromId = request.FromEntityId.HasValue && request.FromEntityId.Value != Guid.Empty 
            ? request.FromEntityId.Value 
            : Guid.Empty;
        Guid toId = request.ToEntityId.HasValue && request.ToEntityId.Value != Guid.Empty 
            ? request.ToEntityId.Value 
            : Guid.Empty;

        if (fromId == Guid.Empty && !string.IsNullOrWhiteSpace(request.From))
        {
            var entity = await _graph.GetEntityAsync(request.WorkspaceName, request.From);
            if (entity is not null) fromId = entity.Id; else return new { success = false, message = $"Source entity '{request.From}' not found." };
        }
        if (toId == Guid.Empty && !string.IsNullOrWhiteSpace(request.To))
        {
            var entity = await _graph.GetEntityAsync(request.WorkspaceName, request.To);
            if (entity is not null) toId = entity.Id; else return new { success = false, message = $"Target entity '{request.To}' not found." };
        }

        if (fromId == Guid.Empty || toId == Guid.Empty)
        {
            return new { success = false, message = "Invalid relation payload. Provide fromEntityId/toEntityId or from/to names that exist." };
        }
        
        var model = new RelationModel(
            request.WorkspaceName, 
            fromId, 
            toId, 
            request.RelationType, 
            request.Metadata);
        
        model = await _relations.UpsertRelationAsync(model); // capture reused relation Id if existed
        return new { success = true, relationId = model.Id, workspace = model.WorkspaceName, fromEntityId = model.FromEntityId, toEntityId = model.ToEntityId, relationType = model.RelationType };
    }

    [Function(nameof(GetEntityRelations))]
    public async Task<object> GetEntityRelations(
        [McpToolTrigger("get_entity_relations", "Gets all relations originating from a specific entity.")] 
        ToolInvocationContext context,
        [McpToolProperty("workspaceName", "The workspace identifier.", isRequired: true)]
        string workspaceName,
        [McpToolProperty("entityId", "The GUID of the entity (preferred).", isRequired: false)]
        Guid? entityId,
        [McpToolProperty("entityName", "Legacy entity name (used if entityId not provided).", isRequired: false)]
        string? entityName)
    {
        Guid resolvedId = Guid.Empty;
        if (entityId.HasValue && entityId.Value != Guid.Empty)
        {
            resolvedId = entityId.Value;
        }
        else if (!string.IsNullOrWhiteSpace(entityName))
        {
            var entity = await _graph.GetEntityAsync(workspaceName, entityName);
            if (entity is null)
            {
                return new { success = false, message = $"Entity '{entityName}' not found in workspace '{workspaceName}'." };
            }
            resolvedId = entity.Id;
        }
        else
        {
            return new { success = false, message = "Provide either entityId (GUID) or entityName." };
        }

        var relations = await _relations.GetRelationsFromEntityAsync(workspaceName, resolvedId);
        
        return new
        {
            success = true,
            WorkspaceName = workspaceName,
            EntityId = resolvedId,
            Relations = relations
        };
    }

    public class UpsertEntityRequest
    {
        [Description("The workspace name for the entity.")]
        public required string WorkspaceName { get; set; }
        
        [Description("The name of the entity.")]
        public required string Name { get; set; }
        
        [Description("The type/category of the entity.")]
        public required string EntityType { get; set; }
        
        [Description("List of observations about the entity.")]
        public List<string>? Observations { get; set; }
        
        [Description("Optional metadata as JSON string.")]
        public string? Metadata { get; set; }
    }

    public class UpsertRelationRequest
    {
        [Description("The workspace name for the relation.")]
        public required string WorkspaceName { get; set; }
        
        [Description("The GUID of the source entity.")]
        public Guid? FromEntityId { get; set; }
        
        [Description("The GUID of the target entity.")]
        public Guid? ToEntityId { get; set; }
        
        [Description("Legacy source entity name (used if fromEntityId not provided).")]
        public string? From { get; set; }
        
        [Description("Legacy target entity name (used if toEntityId not provided).")]
        public string? To { get; set; }
        
        [Description("The type of relationship (e.g., 'knows', 'works_with', 'owns').")]
        public required string RelationType { get; set; }
        
        [Description("Optional metadata as JSON string.")]
        public string? Metadata { get; set; }
    }
}

using System.Collections.Generic;
using System.Linq;

namespace Owlcat.Blueprints.Server.FileDatabase;

internal class References
{
    // blueprint -> outgoing blueprint's references
    private readonly Dictionary<string, HashSet<string>> m_ReferencesFrom = new();
    // blueprint -> incoming blueprint's references
    private readonly Dictionary<string, HashSet<string>> m_ReferencedBy = new();

    // blueprint -> outgoing entity's references
    private readonly Dictionary<string, HashSet<string>> m_EntitiesReferencedByBlueprint = new();
    // entity -> incoming blueprint's references
    private readonly Dictionary<string, HashSet<string>> m_BlueprintsWithReferencesToEntity = new();

    internal void Update(IndexEntry entry)
    {
        UpdateBlueprintReferences(entry);
        UpdateEntityReferences(entry);
    }

    private void UpdateBlueprintReferences(IndexEntry entry)
    {
        if (entry.ReferencedBlueprints.Count > 0 && !m_ReferencesFrom.ContainsKey(entry.Id))
        {
            m_ReferencesFrom.Add(entry.Id, new HashSet<string>());
        }

        var currentDepends = m_ReferencesFrom.GetValueOrDefault(entry.Id);
        var addReferencedBlueprints = currentDepends != null
            ? entry.ReferencedBlueprints.Except(currentDepends).ToArray()
            : entry.ReferencedBlueprints.ToArray();
        var removeReferencedBlueprints = currentDepends?.Except(entry.ReferencedBlueprints).ToArray();

        if (addReferencedBlueprints.Length > 0)
        {
            if (!m_ReferencesFrom.ContainsKey(entry.Id))
            {
                m_ReferencesFrom.Add(entry.Id, new HashSet<string>());
            }

            foreach (var referencedBlueprintId in addReferencedBlueprints)
            {
                m_ReferencesFrom[entry.Id].Add(referencedBlueprintId);
                AddBlueprintReferencedBy(referencedBlueprintId, entry.Id);
            }
        }

        if (removeReferencedBlueprints is { Length: > 0 })
        {
            var referencesFrom = m_ReferencesFrom.GetValueOrDefault(entry.Id);
            foreach (var referencedBlueprintId in removeReferencedBlueprints)
            {
                referencesFrom?.Remove(referencedBlueprintId);
                RemoveBlueprintReferencedBy(referencedBlueprintId, entry.Id);
            }

            if (referencesFrom is { Count: <= 0 })
            {
                m_ReferencesFrom.Remove(entry.Id);
            }
        }
    }

    private void UpdateEntityReferences(IndexEntry entry)
    {
        if (entry.ReferencedEntities.Count > 0 && !m_EntitiesReferencedByBlueprint.ContainsKey(entry.Id))
        {
            m_EntitiesReferencedByBlueprint.Add(entry.Id, new HashSet<string>());
        }

        var currentDepends = m_EntitiesReferencedByBlueprint.GetValueOrDefault(entry.Id);
        var addReferencedEntities = currentDepends != null
            ? entry.ReferencedEntities.Except(currentDepends).ToArray()
            : entry.ReferencedEntities.ToArray();
        var removeReferencedEntities = currentDepends?.Except(entry.ReferencedEntities).ToArray();

        if (addReferencedEntities.Length > 0)
        {
            if (!m_EntitiesReferencedByBlueprint.ContainsKey(entry.Id))
            {
                m_EntitiesReferencedByBlueprint.Add(entry.Id, new HashSet<string>());
            }

            foreach (var referencedEntityId in addReferencedEntities)
            {
                m_EntitiesReferencedByBlueprint[entry.Id].Add(referencedEntityId);
                AddEntityReferencedBy(referencedEntityId, entry.Id);
            }
        }

        if (removeReferencedEntities is {Length: > 0})
        {
            var entityReferencesFrom = m_EntitiesReferencedByBlueprint.GetValueOrDefault(entry.Id);
            foreach (var referencedEntityId in removeReferencedEntities)
            {
                entityReferencesFrom?.Remove(referencedEntityId);
                RemoveEntityReferencedBy(referencedEntityId, entry.Id);
            }

            if (entityReferencesFrom is {Count: <= 0})
            {
                m_EntitiesReferencedByBlueprint.Remove(entry.Id);
            }
        }
    }

    internal void Remove(IndexEntry entry)
    {
        foreach (string referencedBlueprintId in entry.ReferencedBlueprints)
        {
            RemoveBlueprintReferencedBy(referencedBlueprintId, entry.Id);
        }

        m_ReferencesFrom.Remove(entry.Id);
    }

    private void AddBlueprintReferencedBy(string id, string referencedById)
    {
        if (!m_ReferencedBy.ContainsKey(id))
        {
            m_ReferencedBy.Add(id, new HashSet<string>());
        }

        m_ReferencedBy[id].Add(referencedById);
    }

    private void RemoveBlueprintReferencedBy(string id, string referencedById)
    {
        if (!m_ReferencedBy.ContainsKey(id))
        {
            return;
        }

        m_ReferencedBy[id].Remove(referencedById);

        if (m_ReferencedBy[id].Count <= 0)
        {
            m_ReferencedBy.Remove(id);
        }
    }

    private void AddEntityReferencedBy(string entityId, string referencedById)
    {
        if (!m_BlueprintsWithReferencesToEntity.ContainsKey(entityId))
        {
            m_BlueprintsWithReferencesToEntity.Add(entityId, new HashSet<string>());
        }

        m_BlueprintsWithReferencesToEntity[entityId].Add(referencedById);
    }

    private void RemoveEntityReferencedBy(string entityId, string referencedById)
    {
        if (!m_BlueprintsWithReferencesToEntity.ContainsKey(entityId))
        {
            return;
        }

        m_BlueprintsWithReferencesToEntity[entityId].Remove(referencedById);

        if (m_BlueprintsWithReferencesToEntity[entityId].Count <= 0)
        {
            m_BlueprintsWithReferencesToEntity.Remove(entityId);
        }
    }
    
    internal IEnumerable<string> GetBlueprintsReferencedBy(string id)
        => m_ReferencedBy.GetValueOrDefault(id) ?? Enumerable.Empty<string>();
    
    internal IEnumerable<string> GetBlueprintReferencesFrom(string id)
        => m_ReferencesFrom.GetValueOrDefault(id) ?? Enumerable.Empty<string>();
    
    internal IEnumerable<string> GetEntitiesReferencedByBlueprint(string blueprintId)
        => m_EntitiesReferencedByBlueprint.GetValueOrDefault(blueprintId) ?? Enumerable.Empty<string>();
    
    internal IEnumerable<string> GetBlueprintsWithReferencesToEntity(string entityId)
        => m_BlueprintsWithReferencesToEntity.GetValueOrDefault(entityId) ?? Enumerable.Empty<string>();
    
    internal IEnumerable<string> GetAllReferencedBlueprints()
        => m_ReferencedBy.Keys;
    
    internal IEnumerable<string> GetAllReferencedEntities()
        => m_BlueprintsWithReferencesToEntity.Keys;
    
    internal IEnumerable<string> GetAllBlueprintsWithReferencesToEntity()
        => m_EntitiesReferencedByBlueprint.Keys;
}
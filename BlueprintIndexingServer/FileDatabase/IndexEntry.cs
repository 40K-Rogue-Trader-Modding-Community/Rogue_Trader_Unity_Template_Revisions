using System;
using System.Collections.Generic;

namespace Owlcat.Blueprints.Server.FileDatabase
{
    internal readonly struct IndexEntry : IEquatable<IndexEntry>
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Path;
        public readonly string TypeId;
        public readonly bool IsShadowDeleted;
        public readonly HashSet<string> ReferencedBlueprints;
        public readonly HashSet<string> ReferencedEntities;

        public IndexEntry(
            string id,
            string name,
            string typeId,
            string path,
            bool isShadowDeleted,
            HashSet<string> referencedBlueprints,
            HashSet<string> referencedEntities)
        {
            Id = id;
            Name = name;
            Path = path;
            TypeId = typeId;
            IsShadowDeleted = isShadowDeleted;
            ReferencedBlueprints = referencedBlueprints;
            ReferencedEntities = referencedEntities;
        }

        public bool Equals(IndexEntry other)
            => Id == other.Id;

        public override bool Equals(object? obj)
            => obj is IndexEntry other && Equals(other);

        public override int GetHashCode()
            => Id.GetHashCode();
    }
}
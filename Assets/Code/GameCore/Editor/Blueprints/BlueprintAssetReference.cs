using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Editor.Validation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

#nullable enable

namespace Kingmaker.Editor.Blueprints
{
	/// <summary>
	/// This class provides some convenient string representations
	/// of blueprint references from unity assets for various viewers and editors
	///
	/// *All blueprints are referenced from unity scenes only actually
	///
	/// Relates to BlueprintPropertyPath
	/// </summary>
	public class BlueprintAssetReference
	{
		public readonly string ScenePath;
		public readonly string GameObjectName;
		public readonly string ComponentTypeName;

		public BlueprintAssetReference(string scenePath, string gameObjectName, string componentTypeName)
		{
			ScenePath = scenePath;
			GameObjectName = gameObjectName;
			ComponentTypeName = componentTypeName;
		}

		/// <summary>
		/// Collects given blueprint back-references to unity asses as BlueprintAssetReferences
		/// </summary>
		public static IEnumerable<BlueprintAssetReference>? GetReferencedBy(BlueprintScriptableObject bp)
		{
			var entry = ReferenceGraph.Graph.Entries.Find(e => e.ObjectGuid == bp.AssetGuid);
			var references = entry?.References
				.Where(r => r.IsScene)
				.Select(r => new BlueprintAssetReference(r.AssetPath, r.ReferencingObjectName, r.ReferencingObjectType))
				.ToArray();

			return references?.Length > 0 ? references : null;
		}
	}
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Utility.Attributes;
using Kingmaker.Utility.DotNetExtensions;
using UnityEditor;
using UnityEngine;

#nullable enable

namespace Kingmaker.Editor.Blueprints
{
	[Serializable]
	public class EtudeBackReference
	{
		public enum Kind
		{
			Other,
			Started,
			Completed,
			StatusCheck,
			CutsceneParam,
			UnStart,
			SceneObject,
		}

		private class RxKind
		{
			public Regex Regex { get; }
			public Kind Kind { get; }

			public RxKind(Regex regex, Kind kind)
			{
				Regex = regex;
				Kind = kind;
			}
		}

		private static readonly RxKind[] ShortRxToKind =
		{
			new (new Regex(@"^StartsWith\[\d+\]$"), Kind.Started),
			new (new Regex(@"^StartEtudes\[\d+\]$"), Kind.Started),
		};

		private static readonly RxKind[] VerboseRxToKind =
		{
			new (new Regex(@"(^|\.)StartEtude\.Etude$"), Kind.Started),
			new (new Regex(@"(^|\.)CompleteEtude\.Etude$"), Kind.Completed),
			new (new Regex(@"(^|\.)EtudeStatus\.Etude$"), Kind.StatusCheck),
			new (new Regex(@"(^|\.)PlayCutscene\.Parameters\.ParameterEntry\.Evaluator\.Value$"), Kind.CutsceneParam),
			new (new Regex(@"(^|\.)UnStartEtude\.Etude$"), Kind.UnStart),
		};

		[HideInInspector] // Name goes first just to become array element label and not needed by itself
		public string Name;

		[HideInInspector]
		public Kind ReferenceKind;

		[HideIf(nameof(IsSceneObject))]
		public AnyBlueprintReference? Blueprint;
		[HideIf(nameof(IsSceneObject))]
		public string? ShortPath;
		[HideIf(nameof(IsSceneObject))]
		public string? VerbosePath;

		[ShowIf(nameof(IsSceneObject))]
		public SceneAsset? SceneAsset;
		[ShowIf(nameof(IsSceneObject))]
		public string? ObjectName;
		[ShowIf(nameof(IsSceneObject))]
		public string? ComponentType;

		public bool IsSceneObject => ReferenceKind == Kind.SceneObject;

		private EtudeBackReference(AnyBlueprintReference blueprint, Kind kind, string shortPath, string verbosePath)
		{
			Blueprint = blueprint;
			Name = $"{kind} in {blueprint.NameSafe()} [{blueprint.GetBlueprint().GetType().Name}]";
			ReferenceKind = kind;
			ShortPath = shortPath;
			VerbosePath = verbosePath;

			SceneAsset = null;
			ObjectName = null;
			ComponentType = null;
		}

		private EtudeBackReference(BlueprintAssetReference assetRef)
		{
			Blueprint = null;
			ReferenceKind = Kind.SceneObject;
			ShortPath = null;
			VerbosePath = null;

			SceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(assetRef.ScenePath);
			string sceneName = Path.GetFileNameWithoutExtension(assetRef.ScenePath);
			ObjectName = assetRef.GameObjectName;
			ComponentType = assetRef.ComponentTypeName;
			Name = $"{ReferenceKind} in {sceneName} [{ObjectName}.{ComponentType}]";
		}

		public static EtudeBackReference Create(BlueprintPropertyPath bppPath)
		{
			var rxKind = ShortRxToKind.FirstItem(rxKind => rxKind.Regex.IsMatch(bppPath.ShortPath))
			             ?? VerboseRxToKind.FirstItem(rxKind => rxKind.Regex.IsMatch(bppPath.VerbosePath));

			return new EtudeBackReference(
				AnyBlueprintReference.CreateTyped<AnyBlueprintReference>(bppPath.Blueprint),
				rxKind?.Kind ?? Kind.Other,
				bppPath.ShortPath,
				bppPath.VerbosePath);
		}

		public static EtudeBackReference Create(BlueprintAssetReference assetRef)
		{
			return new EtudeBackReference(assetRef);
		}

		public static IEnumerable<EtudeBackReference> GetReferencedBy(
			BlueprintEtude etude,
			BlueprintPropertyPath.RefException[]? refExceptions = null)
		{
			var etudeBlueprintRefs =
				BlueprintPropertyPath.GetReferencedBy(etude, refExceptions)?
					.Select(EtudeBackReference.Create) ?? Enumerable.Empty<EtudeBackReference>();

			var etudeAssetRefs =
				BlueprintAssetReference.GetReferencedBy(etude)?
					.Select(EtudeBackReference.Create) ?? Enumerable.Empty<EtudeBackReference>();

			var etudeRefs = etudeBlueprintRefs.Concat(etudeAssetRefs)
				.OrderBy(etudeRef => etudeRef.ReferenceKind);

			return etudeRefs;
		}
	}
}
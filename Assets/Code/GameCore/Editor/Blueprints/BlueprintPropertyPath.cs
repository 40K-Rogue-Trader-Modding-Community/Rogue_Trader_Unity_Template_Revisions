using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.Utility;
using Kingmaker.Utility.DotNetExtensions;
using UnityEditor;

#nullable enable

namespace Kingmaker.Editor.Blueprints
{
	/// <summary>
	/// This class provides some convenient string representations
	/// of blueprint property paths for various viewers and editors
	/// Relates to BlueprintAssetReference
	/// </summary>
	public class BlueprintPropertyPath
	{
		public readonly BlueprintScriptableObject Blueprint;
		public readonly string FullPath;
		public readonly string ShortPath;
		public readonly string VerbosePath;

		private const string ParentPath = "Blueprint.m_Parent";
		private const string AllElementsPrefix = "Blueprint.m_AllElements.";

		private static readonly Regex NameEndRx = new (@"(^|\.)\w+$");
		private static readonly Regex ArrayNameEndRx = new (@"(^|\.)\w+\.Array\.data\[\d+\]$");

		private static readonly Regex ArrayIndexRx = new (@"\.Array\.data(\[\d+\])");

		/// <summary>
		/// Parts of the path to remove (i.e. in case they are obvious or bring nothing valuable to the table)
		/// </summary>
		private static readonly Regex[] PartsToRemoveRx =
		{
			new(@"^Blueprint\."),
			new(@"(?<=\.|^)m_"),
			new(@"(?<=\.|^)Action\."),
			new(@"(?<=\.|^)Actions\."),
			new(@"(?<=\.|^)ActionList\."),
			new(@"(?<=\.|^)Conditions\."),
			new(@"(?<=\.|^)ConditionsChecker\."),
		};

		public class RefException
		{
			private readonly Type m_RefType;
			private readonly Regex m_PropertyRx;

			public RefException(Type refType, Regex propertyRx)
			{
				m_RefType = refType;
				m_PropertyRx = propertyRx;
			}

			public bool ShouldSkip(BlueprintScriptableObject reference, string path)
			{
				return reference.GetType() == m_RefType && m_PropertyRx.IsMatch(path);
			}
		}

		private BlueprintPropertyPath(
			BlueprintScriptableObject blueprint,
			string fullPath,
			string shortPath,
			string verbosePath)
		{
			Blueprint = blueprint;
			FullPath = fullPath;
			ShortPath = shortPath;
			VerbosePath = verbosePath;
		}

		private static BlueprintPropertyPath? Create(
			BlueprintScriptableObject blueprint,
			SerializedObject so,
			SerializedProperty sp,
			RefException[]? refExceptions)
		{
			string fullPath = sp.propertyPath;

			if (fullPath.StartsWith(AllElementsPrefix))
			{
				// TODO: Find out the purpose of such properties that duplicate other ones and not stored in a file
				return null;
			}

			if (fullPath == ParentPath)
			{
				// Skip parent refs
				return null;
			}

			if (refExceptions.Any(refException => refException.ShouldSkip(blueprint, fullPath)))
			{
				// Skip paths from exceptions
				return null;
			}

			string verbosePath = ReplaceArrayElementsWithTypeNames(fullPath, so);

			// Replace array stuff with pure indexes i.e. 'SomeStuff.Array.data[0]' -> 'SomeStuff[0]'
			string shortPath = ArrayIndexRx.Replace(fullPath, match => match.Groups[1].Value);
			verbosePath = ArrayIndexRx.Replace(verbosePath, match => match.Groups[1].Value);

			// Remove other parts
			foreach (var regex in PartsToRemoveRx)
			{
				shortPath = regex.Replace(shortPath, "");
				verbosePath = regex.Replace(verbosePath, "");
			}

			return new BlueprintPropertyPath(blueprint, fullPath, shortPath, verbosePath);
		}

		private static string ReplaceArrayElementsWithTypeNames(string origPath, SerializedObject so)
		{
			var parts = new Stack<string>(32);
			do
			{
				origPath = ClipFromEnd(origPath, NameEndRx, out string? nameEnding);
				if (nameEnding != null)
				{
					// Just regular property
					parts.Push(nameEnding);
					continue;
				}

				string arrayPath = ClipFromEnd(origPath, ArrayNameEndRx, out string? arrayEnding);
				if (arrayEnding != null)
				{
					// Array element property - get the type name from it
					var actionProp = so.FindProperty(origPath);
					if (actionProp != null)
					{
						object? propertyObj = PropertyResolver.GetPropertyObject(actionProp);
						if (propertyObj != null)
						{
							arrayEnding = $".{propertyObj.GetType().Name}";
						}
					}

					parts.Push(arrayEnding);
					origPath = arrayPath;
					continue;
				}

				throw new Exception($"Unexpected path ending for '{origPath}'");

			} while (origPath.Contains('.'));

			parts.Push(origPath);
			return string.Join("", parts);
		}

		private static string ClipFromEnd(string src, Regex endRx, out string? ending)
		{
			ending = null;
			var match = endRx.Match(src);
			if (match.Success)
			{
				ending = match.Value;
				src = src[..^ending.Length];
			}
			return src;
		}

		/// <summary>
		/// Collects given blueprint back-references as BlueprintPropertyPaths
		/// </summary>
		public static IEnumerable<BlueprintPropertyPath>? GetReferencedBy(BlueprintScriptableObject blueprint, RefException[]? refExceptions = null)
		{
			var allReferences = BlueprintsDatabase.GetReferencedBy(blueprint.AssetGuid)
				.Select(tuple => BlueprintsDatabase.LoadAtPath<BlueprintScriptableObject>(tuple.Path));

			var propertyPaths = new List<BlueprintPropertyPath>(16);
			foreach (var reference in allReferences)
			{
				var sc = BlueprintEditorWrapper.Wrap(reference);
				var so = new SerializedObject(sc);
				var it = so.GetIterator();
				while (it.Next(true))
				{
					if (it.isArray ||
					    it.propertyType != SerializedPropertyType.Generic)
					{
						continue;
					}

					if (PropertyResolver.GetPropertyObject(it) is not BlueprintReferenceBase bpRef
					    || bpRef.Guid != blueprint.AssetGuid)
					{
						continue;
					}

					var blueprintPropertyPath = Create(reference, so, it, refExceptions);
					if (blueprintPropertyPath != null)
					{
						propertyPaths.Add(blueprintPropertyPath);
					}
				}

			}
			return propertyPaths.Count <= 0 ? null : propertyPaths;
		}
	}
}
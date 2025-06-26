using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Code.GameCore.Editor.Validation;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.Utility;
using Kingmaker.Localization;
using Kingmaker.Utility.DotNetExtensions;
using UnityEditor;
using UnityEngine;

#nullable enable

namespace Kingmaker.Editor.Localization
{
	/// <summary>
	/// https://jira.owlcat.games/browse/WH2-3620
	/// Renames blueprints along with corresponding string assets
	/// that named in the same way
	/// </summary>
	public class RenameWithStrings
	{
#if UNITY_EDITOR && EDITOR_FIELDS
		private const string JbpExt = ".jbp";
		private const string SharedPrefix = "Assets/Mechanics/";
		private const string StringsPrefix = "Strings/Mechanics/";

		private readonly string m_SrcPrefix;
		private readonly string m_DstPrefix;
		private readonly string m_SharedSrcPrefix;
		private readonly string m_SharedDstPrefix;
		private readonly string m_StringSrcPrefix;
		private readonly string m_StringDstPrefix;
		private readonly string m_StringSrcPrefixNormalized;
		private readonly string m_BlueprintPath;

		private RenameWithStrings(string srcPrefix, string dstPrefix, string blueprintPath)
		{
			m_SrcPrefix = srcPrefix;
			m_DstPrefix = dstPrefix;
			m_StringSrcPrefix = StringsPrefix + m_SrcPrefix;
			m_StringDstPrefix = StringsPrefix + m_DstPrefix;
			m_StringSrcPrefixNormalized = m_StringSrcPrefix.Replace("/", "\\");

			// Shared strings are unity assets, so slashes are expected to be forward
			m_SharedSrcPrefix = SharedPrefix + m_SrcPrefix.Replace("\\", "/");
			m_SharedDstPrefix = SharedPrefix + m_DstPrefix.Replace("\\", "/");

			m_BlueprintPath = blueprintPath;
		}

		private string? DoIt()
		{
			bool isSrcPrefixOk = m_SrcPrefix.StartsWith(BlueprintsDatabase.DbPathPrefix);
			bool isDstPrefixOk = m_DstPrefix.StartsWith(BlueprintsDatabase.DbPathPrefix);
			bool isSuccess = isSrcPrefixOk && isDstPrefixOk;
			if (!isSuccess)
			{
				return $"Bad prefixes:\nsrc:{m_DstPrefix}\ndst:{m_SrcPrefix}";
			}

			if (!m_BlueprintPath.StartsWith(m_SrcPrefix))
			{
				return $"Unexpected src path:\n{m_BlueprintPath}";
			}

			string dstPath = m_DstPrefix + m_BlueprintPath[m_SrcPrefix.Length..];
			var bp = BlueprintsDatabase.LoadAtPath<SimpleBlueprint>(m_BlueprintPath);

			EnsureDirectoryExists(dstPath);
			BlueprintsDatabase.MoveAsset(bp, dstPath);

			// Move referenced string jsons and fix their path references
			bool isBlueprintDirty = false;
			var sc = BlueprintEditorWrapper.Wrap(bp);
			var so = new SerializedObject(sc);
			var it = so.GetIterator();
			while (it.Next(true))
			{
				if (it is
				    {
					    isArray: false,
					    propertyType: SerializedPropertyType.Generic,
					    type: nameof(LocalizedString)
				    })
				{
					var localizedString = PropertyResolver.GetPropertyObject<LocalizedString>(it);

					string? errorMessage = null;
					if (!string.IsNullOrEmpty(localizedString.JsonPath))
					{
						errorMessage = RenameJson(localizedString, ref isBlueprintDirty);
					}
					else if (localizedString.Shared != null)
					{
						errorMessage = RenameShared(localizedString, ref isBlueprintDirty);
					}

					if (errorMessage != null)
					{
						return errorMessage;
					}
				}
			}

			if (isBlueprintDirty)
			{
				BlueprintsDatabase.SetDirty(bp.AssetGuid);
			}
			return null;
		}

		private string? RenameJson(LocalizedString localizedString, ref bool isBlueprintDirty)
		{
			string jsonPath = localizedString.JsonPath;
			if (!File.Exists(jsonPath))
			{
				// This is unexpected, but not fatal for blueprint rename
				Debug.LogError("String JSON not found for " + m_BlueprintPath);
				return null;
			}

			// Json paths are stored with random separators, so we need to normalize them before match
			string normalizedJsonPath = jsonPath.Replace("/", "\\");
			if (!normalizedJsonPath.StartsWith(m_StringSrcPrefixNormalized))
			{
				// This string path is already mismatched with the blueprint one
				// Leave it as is
				return null;
			}

			// string dstJsonPath = LocalizedString.GetCorrespondingJsonPath(it);
			// ^^^ Cannot use this as it fails in a strange way randomly..

			string dstJsonPath = m_StringDstPrefix + jsonPath[m_StringSrcPrefix.Length..];
			if (File.Exists(dstJsonPath))
			{
				return $"Failed to move string JSON from\n{jsonPath}\nto\n{dstJsonPath}\nFile already exists";
			}

			EnsureDirectoryExists(dstJsonPath);
			File.Move(jsonPath, dstJsonPath);

			localizedString.JsonPath = dstJsonPath;
			isBlueprintDirty = true;
			return null;
		}

		private string? RenameShared(LocalizedString localizedString, ref bool isBlueprintDirty)
		{
			var sharedString = localizedString.Shared;
			if (sharedString == null)
			{
				// This is unexpected, but not fatal for blueprint rename
				Debug.LogError("Shared string not found for " + m_BlueprintPath);
				return null;
			}

			string sharedPath = AssetDatabase.GetAssetPath(sharedString);
			if (!sharedPath.StartsWith(m_SharedSrcPrefix))
			{
				// This string path is already mismatched with the blueprint one
				// Leave it as is
				return null;
			}

			string dstSharedPath = m_SharedDstPrefix + sharedPath[m_SharedSrcPrefix.Length..];
			if (File.Exists(dstSharedPath))
			{
				// As this is a shared string - it may be already been moved
				// by some other blueprint rename operation
				return null;
			}

			// Just move asset in a usual way
			// String JSONs will be moved accordingly by LocalizationAssetsTracker
			EnsureDirectoryExists(dstSharedPath);
			string moveError = AssetDatabase.MoveAsset(sharedPath, dstSharedPath);

			return string.IsNullOrEmpty(moveError) ? null : moveError;
		}

		public static void RenameFolder(string blueprintFolder)
		{
			blueprintFolder = BlueprintsDatabase.FullToRelativePath(blueprintFolder);
			var bpPaths = BlueprintsDatabase.SearchByFolder(blueprintFolder)
				.Select(tuple => tuple.Path);

			RenameBlueprints(bpPaths);
		}

		public static void RenameBlueprints(IEnumerable<string> blueprintPaths)
		{
			string[] paths = blueprintPaths.ToArray();
			string commonPrefix = paths.GetCommonPrefix();

			if (paths.Length <= 0)
			{
				EditorUtility.DisplayDialog("ERROR", "No blueprints found in selection", "Ok");
				return;
			}

			// Get new prefix from user to rename to and do rename at will
			var renameWindow = EditorWindow.GetWindow<RenameWindow>();
			renameWindow.Set(
				"Rename Blueprints With Strings",
				commonPrefix,
				(string srcPrefix, string dstPrefix, out string result)
					=> RenameBlueprints(paths, srcPrefix, dstPrefix, out result));
			renameWindow.Show();
		}

		private static bool RenameBlueprints(
			string[] blueprintPaths,
			string srcPrefix,
			string dstPrefix,
			out string statusMessage)
		{
			bool isSuccess = true;
			statusMessage = "Selected blueprints were renamed along with corresponding string assets";

			// Remove blueprint extensions as these should act like prefixes
			// for the corresponding string json paths as well
			srcPrefix = srcPrefix.EndsWith(JbpExt) ? srcPrefix[..^JbpExt.Length] : srcPrefix;
			dstPrefix = dstPrefix.EndsWith(JbpExt) ? dstPrefix[..^JbpExt.Length] : dstPrefix;

			try
			{
				var progress = new SafeProgressBar("Fixing String JSON Paths", blueprintPaths.Length, 1);
				foreach (string srcPath in blueprintPaths)
				{
					progress.DisplayProgress(srcPath);

					string? errorMessage = new RenameWithStrings(srcPrefix, dstPrefix, srcPath).DoIt();
					if (errorMessage != null)
					{
						isSuccess = false;
						statusMessage = errorMessage;
						break;
					}
				}
			}
			catch(Exception e)
			{
				isSuccess = false;
				Debug.LogError(e);
				statusMessage = e.ToString();
			}
			finally
			{
				// As it turned out, SafeProgressBar is not safe enough..
				EditorUtility.ClearProgressBar();
			}

			if (isSuccess)
			{
				using var _ = new ReferenceGraphGuard(isReferenceTrackingSuppressed:true);
				BlueprintsDatabase.SaveAllDirty();
				EditorApplication.delayCall += () =>
				{
					CleanupEmptyFolders(srcPrefix);
					CleanupEmptyFolders(SharedPrefix + srcPrefix);
					CleanupEmptyFolders(StringsPrefix + srcPrefix);
				};
			}
			return isSuccess;
		}

		private static void EnsureDirectoryExists(string path)
		{
			string? dir = Path.GetDirectoryName(path);
			if (dir == null || Directory.Exists(dir))
			{
				return;
			}

			if (!path.StartsWith("Assets/"))
			{
				Directory.CreateDirectory(dir);
				return;
			}

			// Create via AssetDatabase to allow Unity track it's creation
			// Otherwise, commands like AssetDatabase.MoveAsset() will not work correctly.
			//
			// AssetDatabase.CreateFolder(parentDir, dirName) creates only one dir level per call so,
			// let us do some tricky stuff

			dir = dir.Replace("\\", "/");
			string[] dirNames = dir.Split("/");
			if (dirNames.Length < 1)
			{
				return;
			}

			string root = dirNames[0];
			if (!AssetDatabase.IsValidFolder(root))
			{
				return;
			}

			for (int i = 1; i < dirNames.Length; ++i)
			{
				string dirName = dirNames[i];
				string currentDir = $"{root}/{dirName}";
				if (!AssetDatabase.IsValidFolder(currentDir))
				{
					AssetDatabase.CreateFolder(root, dirName);
				}
				root = currentDir;
			}
		}

		private static void CleanupEmptyFolders(string pathPrefix)
		{
			string? rootFolder = Directory.Exists(pathPrefix) ? pathPrefix : Path.GetDirectoryName(pathPrefix);
			if (rootFolder == null || !Directory.Exists(rootFolder))
			{
				return;
			}

			// Make a reverse-sorted paths of all folders inside root including itself
			string[] folderPaths = Directory.GetDirectories(rootFolder, "*", SearchOption.AllDirectories);
			folderPaths = folderPaths.Concat(new[] {rootFolder}).ToArray();
			Array.Sort(folderPaths);
			Array.Reverse(folderPaths);

			// Perform cleanup of empty folders
			foreach (string path in folderPaths)
			{
				if (Directory.GetFileSystemEntries(path).Length <= 0)
				{
					Directory.Delete(path);
				}
			}
		}

		[MenuItem("Tools/Misc/Cleanup empty folders")]
		private static void CleanupEmptyFolders()
		{
			CleanupEmptyFolders("./");
		}

		// [MenuItem("Tests/test")]
		// ReSharper disable once UnusedMember.Local
		private static void Test()
		{
			RenameBlueprints(
				new [] {@"Blueprints\World\Dialogs\Ch5\Farewell\VoxMasterFarewell\Answer_0004.jbp"},
				@"Blueprints\World\Dialogs\Ch5\Farewell\VoxMasterFarewell\Answer_0004.jbp",
				@"Blueprints\World\Dialogs\Ch5\Farewell\VoxMasterFarewell_ll\Answer_0004.jbp",
				out string errorMessage);
			Debug.LogError(errorMessage);
		}
#endif
	}
}
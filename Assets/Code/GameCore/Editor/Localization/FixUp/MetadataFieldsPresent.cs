using System.IO;
using JetBrains.Annotations;
using Kingmaker.Localization;
using UnityEditor;

namespace Kingmaker.Editor.Localization.FixUp
{
	public static class MetadataFieldsPresent
	{
#if EDITOR_FIELDS
		public static bool Check([NotNull] LocalizedString str)
		{
			if (str.IsTrulyEmpty || str.Shared || string.IsNullOrWhiteSpace(str.JsonPath))
				return true;

			if (!File.Exists(str.JsonPath))
				return true;

			return !string.IsNullOrWhiteSpace(str.Key)
			       && !string.IsNullOrWhiteSpace(str.OwnerString)
			       && !string.IsNullOrWhiteSpace(str.OwnerPropertyPath);
		}

		public static void Fix([NotNull] LocalizedString str, [NotNull] SerializedProperty prop)
		{
			if (Check(str))
				return;

			var data = str.GetLoadedData() ?? str.LoadData();
			if (data == null || string.IsNullOrWhiteSpace(data.Key))
				return;

			string jsonPath = str.JsonPath;
			str.ForceJsonFile(prop, data.Key, jsonPath);

			if (data.OwnerGuid != str.OwnerString)
			{
				data.OwnerGuid = str.OwnerString;
				str.SaveJson(prop);
			}
		}
#endif
	}
}

using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Blueprints.Quests;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Editor.Utility;
using Kingmaker.Localization;
using Kingmaker.Localization.Enums;
using Kingmaker.Localization.Shared;
using UnityEditor;

public class LocalizationUtility : EditorWindow
{
	// Types whose BlueprintScriptableObject.Comment should be mirrored
	// into LocaleData.TranslationComment of every LocalizedString field
	// (so localization tooling can see the editor's design notes).
	// Add new blueprint types here to extend coverage.
	private static bool IsCommentPropagationSupported(SimpleBlueprint blueprint)
		=> blueprint is BlueprintAnswer
			or BlueprintCue
			or BlueprintQuest
			or BlueprintQuestObjective;

	[MenuItem("Tools/Localization/Update localization comments")]
	public static void AddCommentsToJsons()
	{
		if (Selection.objects == null || Selection.objects.Length == 0)
			return;
		EditorUtility.DisplayProgressBar("Loading everything", "", 0);
		var selected = Selection.objects
			.Select(_ => _ as BlueprintEditorWrapper)
			.Where(_ => _ != null).ToList();
		foreach (BlueprintEditorWrapper selectedObj in new ProgressWrapper<BlueprintEditorWrapper>(selected, "Handling blueprints"))
		{
			if (!IsCommentPropagationSupported(selectedObj.Blueprint))
				continue;
			AddCommentsToJsons(selectedObj);
		}
		EditorUtility.DisplayProgressBar("Saving blueprints", "", 0);
		BlueprintsDatabase.SaveAllDirty();
		EditorUtility.ClearProgressBar();
	}
        
	public static void AddCommentsToJsons(BlueprintEditorWrapper asset)
	{
#if UNITY_EDITOR && EDITOR_FIELDS
		var comment = (asset.Blueprint as BlueprintScriptableObject)?.Comment;
		if (string.IsNullOrEmpty(comment))
			return;
	        
		var so = new SerializedObject(asset);
		var p = so.GetIterator();
		p.Next(true);
		do
		{
			if (p.propertyType == SerializedPropertyType.Generic && !p.isArray && p.type == nameof(LocalizedString))
			{
				var ls = FieldFromProperty.GetFieldValue(p) as LocalizedString;
				if (ls == null)
					continue;
				//ls.Init(p);
				if (!ls.Shared && ls.JsonPath != "" && ls.GetData()?.Languages.Count > 0)
				{
					var data = ls.GetData();
					bool modified = WriteBlueprintComment(data, Locale.dev, comment);

					if (LocalizationManager.Instance.CurrentLocale == Locale.ruRU)
						modified |= WriteBlueprintComment(data, Locale.ruRU, comment);

					if (modified)
						ls.SaveJson(p);
				}
			}
		} while (p.Next((p.propertyType == SerializedPropertyType.Generic ||
		                 p.propertyType == SerializedPropertyType.ManagedReference) &&
		                p.propertyPath != "Blueprint.Components"));

		so.ApplyModifiedPropertiesWithoutUndo();
#endif
	}

#if UNITY_EDITOR && EDITOR_FIELDS
	private static bool WriteBlueprintComment(LocalizedStringData data, Locale locale, string blueprintComment)
	{
		if (data == null)
			return false;

		var localeData = data.GetOrCreateLocaleData(locale);
		var composed = LocalizedStringCommentSections.ReplaceBlueprintPart(localeData.TranslationComment, blueprintComment);
		return data.UpdateTranslationComment(localeData, composed);
	}
#endif
}

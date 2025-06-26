#if EDITOR_FIELDS
using System;
using System.Linq;
using Kingmaker.Editor.Localization;
using Kingmaker.Editor.UIElements;
using Kingmaker.Editor.UIElements.Custom.Properties;
using Kingmaker.Editor.Utility;
using Kingmaker.Localization;
using Kingmaker.Localization.Shared;
using Kingmaker.Utility.DotNetExtensions;
using Kingmaker.Utility.UnityExtensions;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor
{
	[CustomPropertyDrawer(typeof(LocalizedString))]
	public class LocalizedStringPropertyDrawer : PropertyDrawer
	{
		private const float SplitterHeight = 0;
		private const float MinTextHeight = 63;

		public override VisualElement CreatePropertyGUI(SerializedProperty property)
			=> new LocalizedStringProperty(property);

		private bool m_ShowStringTraits;
		private bool m_ShowLocaleTraits;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.serializedObject.targetObjects.Length > 1)
			{
				EditorGUI.LabelField(position, label, new GUIContent("- multiple texts -"));
				return;
			}

			var localizedString = PropertyResolver.GetPropertyObject<LocalizedString>(property);
			if (localizedString == null)
			{
				return;
			}

			position.yMin += SplitterHeight;
			position.height -= 2 * EditorGUIUtility.singleLineHeight + SplitterHeight;
			if (localizedString.Shared != null)
			{
				label.text += " (Shared)";
			}
			bool isExpanded = LocalizationEditorGUI.LocalizedStringPropertyField(position, label, property, localizedString);
			if (!isExpanded)
			{
				return;
			}

			// voice over
			Rect pos = position;
			pos.xMin += EditorGUI.indentLevel * 15f;
			pos.yMin += pos.height;
			pos.height = EditorGUIUtility.singleLineHeight;
			pos.width = 100;

			// Remember start rect of buttons layout
			var buttonsRect = pos;

			var robustProp = new RobustSerializedProperty(property);
			if (!localizedString.Check(property))
			{
				var pos2 = pos;
				pos2.width = 200;
				if (GUI.Button(pos2, "String is broken. Try to fix"))
				{
					localizedString.Fix(property);
				}
				pos2.x += 200;
				pos2.width += 100;
				if (!localizedString.Shared)
				{
					if (GUI.Button(pos2, "Make new Shared String Duplicate", EditorStyles.miniButton))
					{
						var shared = SharedStringAssetPropertyDrawer.CreateShared(robustProp);
						localizedString.MakeNewShared(robustProp, shared, false);
					}
				}

				return;
			}

			// shared strings
			if (property.serializedObject.targetObject is SharedStringAsset)
				return;
			int oldIndent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			try
			{
				if (GUI.Button(pos, "Set Shared", EditorStyles.miniButton))
				{
					AssetPicker.ShowAssetPicker(
						typeof(SharedStringAsset),
						fieldInfo,
						shared =>
						{
							localizedString.SetShared(robustProp, (SharedStringAsset)shared);
						}
					);
				}

				if (!localizedString.Shared)
				{
					pos.x += 100;
					if (GUI.Button(pos, "Make Shared", EditorStyles.miniButtonLeft))
					{
						SharedStringAssetPropertyDrawer.ShowCreator(property, fieldInfo.GetAttribute<StringCreateWindowAttribute>());
					}
					pos.x += 100;
					if (GUI.Button(pos, "Delete String", EditorStyles.miniButton))
					{
						localizedString.ClearData();
						localizedString.MarkDirty(property);
					}
				}
				else
				{
					pos.x += 100;
					if (GUI.Button(pos, "Clear Shared", EditorStyles.miniButton))
					{
						localizedString.SetShared(robustProp, null);
					}

					pos.x += 100;
					pos.x -= EditorGUI.indentLevel * 15f;
                    using (new EditorGUI.DisabledScope(localizedString.Shared))
					{
						EditorGUI.ObjectField(pos, localizedString.Shared, typeof(SharedStringAsset), false);
					}
                    if(localizedString.Shared && GUI.Button(pos,"", GUIStyle.none)) // ping string (built-in pinging is disabled by DisabledScope)
					{
                        EditorGUIUtility.PingObject(localizedString.Shared);
                    }
				}

				string path = localizedString.Shared?.String.JsonPath ?? localizedString.JsonPath;
				pos.x += 100;
				if (!path.IsNullOrEmpty() && GUI.Button(pos, "Show File", EditorStyles.miniButton))
				{
					EditorUtility.RevealInFinder(path);
				}

				// traits

				// Shift down after buttons
				var foldoutRect = buttonsRect;
				foldoutRect.height = EditorGUIUtility.singleLineHeight;
				foldoutRect.y += buttonsRect.height;

				float stringTraitsHeight = DrawTraits(
					TraitUtility.StringTraits,
					localizedString.GetStringTraits(),
					"String traits",
					foldoutRect,
					ref m_ShowStringTraits,
					t => TraitsPartElement.ToggleTrait(localizedString, property, true, t));

				// Shift next foldout down after traits drawn
				foldoutRect.y += stringTraitsHeight;

				var locale = LocalizationManager.Instance.CurrentLocale;
				DrawTraits(
					TraitUtility.Values.Concat(TraitUtility.LocaleTraits).Distinct().ToArray(),
					localizedString.GetLocaleTraits(locale),
					$"{locale} traits",
					foldoutRect,
					ref m_ShowLocaleTraits,
					t => TraitsPartElement.ToggleTrait(localizedString, property, false, t));
			}
			catch (Exception e)
			{
				PFLog.Default.Exception(e);
			}
			finally
			{
				EditorGUI.indentLevel = oldIndent;
			}
		}

		private static float DrawTraits(
			string[] allTraits,
			string[] activeTraits,
			string foldoutLabel,
			Rect foldoutRect,
			ref bool isExpanded,
			Action<string> toggleTrait)
		{
			const float traitWidth = 150;
			const int traitRowCount = 3;

			float overallHeight = foldoutRect.height;

			isExpanded = EditorGUI.Foldout(foldoutRect, isExpanded, foldoutLabel,
				toggleOnLabelClick:true, LocalizationEditorGUI.BoldFoldoutStyle);

			if (isExpanded)
			{
				float traitHeight = EditorGUIUtility.singleLineHeight;

				var traitsPos = foldoutRect;
				traitsPos.y += foldoutRect.height;
				traitsPos.width = traitWidth;

				// Draw traits "matrix"
				int index = -1;
				foreach (string trait in allTraits)
				{
					index++;
					int x = index % traitRowCount;
					int y = index / traitRowCount;
					var traitPos = traitsPos;
					traitPos.x += x * traitsPos.width;
					traitPos.y = traitsPos.y + traitHeight * y;

					bool isSet = activeTraits.IndexOf(trait) >= 0;
					bool newSet = GUI.Toggle(traitPos, isSet, trait, GUI.skin.button);
					if (newSet != isSet)
					{
						toggleTrait(trait);
					}
				}

				int rowsCount = allTraits.Length / traitRowCount + 1;
				overallHeight += rowsCount * traitHeight;
			}

			// Shift layout by the size of all GUI stuff
			GUILayout.Box("", GUIStyle.none, GUILayout.Width(traitWidth), GUILayout.Height(overallHeight));

			return overallHeight;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (property.serializedObject.targetObjects.Length > 1)
			{
				return EditorGUIUtility.singleLineHeight;
			}

			if (!property.isExpanded)
			{
				return SplitterHeight * 2 + EditorGUIUtility.singleLineHeight;
			}

			var localizedString = PropertyResolver.GetPropertyObject<LocalizedString>(property);
			if (localizedString == null)
			{
				return 100;
			}
			float textHeight = 12f + LocalizationEditorGUI.TextAreaStyle.CalcHeight(
				new GUIContent(localizedString.GetText(LocalizationManager.Instance.CurrentLocale)),
				EditorGUIUtility.currentViewWidth - EditorGUI.indentLevel * 15
			);
			textHeight = Mathf.Max(textHeight, MinTextHeight);

			float h = SplitterHeight; // splitter
			h += EditorGUIUtility.singleLineHeight; // header
			h += textHeight; // text, huh
			h += EditorGUIUtility.singleLineHeight; // voice over sound
			h += EditorGUIUtility.singleLineHeight; // shared buttons

			return h;
		}
	}
}
#endif
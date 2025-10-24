using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Owlcat.QA.Validation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

#nullable enable

namespace Kingmaker.Editor.ValidationErrorsWindow
{
	public class ValidationErrorsWindow : EditorWindow
	{
		private VisualElement? content;

		private readonly Dictionary<Object, string> assetsWithErrors = new();
		private readonly Dictionary<Object, string> blueprintsWithErrors = new();

		private static void Revalidate()
		{
			// foreach (string? path in AssetValidator.FilesWithErrors)
			// {
			// 	object? asset = null;
			// 	if (path.StartsWith("Blueprints"))
			// 	{
			// 		asset = BlueprintsDatabase.LoadAtPath<SimpleBlueprint>(path);
			// 	}
			// 	else if (path.StartsWith("Assets"))
			// 	{
			// 		asset = AssetDatabase.LoadAssetAtPath<Object>(path);
			// 	}
			// 	else
			// 	{
			// 		continue;
			// 	}
			// 	// AssetValidator.ValidateAsset(asset, new ValidationContext());
			// }
		}

		public void FindErrors()
		{
			assetsWithErrors.Clear();
			blueprintsWithErrors.Clear();
			foreach (string? path in AssetValidator.FilesWithErrors)
			{
				Object? asset;
				if (path.StartsWith("Assets"))
				{
					asset = AssetDatabase.LoadAssetAtPath<Object>(path);
					if (asset != null)
					{
						assetsWithErrors.Add(asset, path);
					}
				}
				else if (path.StartsWith("Blueprints"))
				{
					var bp = BlueprintsDatabase.LoadAtPath<SimpleBlueprint>(path);
					asset = BlueprintEditorWrapper.Wrap(bp);
					if (asset != null)
					{
						blueprintsWithErrors.Add(asset, path);
					}
				}
				else
				{
					continue;
				}

				if (asset is null)
				{
					AssetValidator.RemoveFromErrorCache(path);
				}
			}
		}

		[MenuItem("Tools/Validates/Current Validation Errors", false)]
		public static void ShowWindow()
		{
			var window = GetWindow<ValidationErrorsWindow>();
			window.titleContent = new GUIContent("Validation Errors");
			window.minSize = new Vector2(480, 320);
		}

		private void CreateGUI()
		{
			var scroll = new ScrollView
			{
				horizontalScrollerVisibility = ScrollerVisibility.Hidden,
				verticalScrollerVisibility = ScrollerVisibility.Auto,
				style = {flexGrow = 1},
			};

			content = new VisualElement();
			SetContentStyle(content.style);
			scroll.Add(content);

			var selectCurrentArea = new Button(Refresh)
			{
				text = "Refresh"
			};

			rootVisualElement.Add(selectCurrentArea);
			rootVisualElement.Add(scroll);

			Refresh();
		}

		private static void SetContentStyle(IStyle style)
		{
			style.flexGrow = 1;

			// Inspector-style paddings
			style.paddingBottom = 2;
			style.paddingLeft = 15;
			style.paddingRight = 6;
			style.paddingTop = 2;
		}

		private static void SetCorkLabelStyle(IStyle style)
		{
			style.fontSize = 12;
			style.marginTop = 20;
			style.unityTextAlign = TextAnchor.MiddleCenter;
		}

		private void Refresh()
		{
			Revalidate();
			FindErrors();

			if (assetsWithErrors.Count <= 0 && blueprintsWithErrors.Count <= 0)
			{
				var corkLabel = new Label("No errors found");
				SetCorkLabelStyle(corkLabel.style);
				content?.Add(corkLabel);
				return;
			}

			content?.Clear();
			if (assetsWithErrors.Count > 0)
			{
				var assetsElement = new ValidationErrorElement("Assets", SkipObjectError);
				assetsElement.UpdateErrorObjects(assetsWithErrors);
				content?.Add(assetsElement);
			}
			if (blueprintsWithErrors.Count > 0)
			{
				var blueprintsElement = new ValidationErrorElement("Blueprints", SkipObjectError);
				blueprintsElement.UpdateErrorObjects(blueprintsWithErrors);
				content?.Add(blueprintsElement);
			}
		}

		private void SkipObjectError(string path)
		{
			AssetValidator.RemoveFromErrorCache(path);
			Refresh();
		}
	}
}
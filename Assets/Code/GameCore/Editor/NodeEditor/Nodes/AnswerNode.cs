using System;
using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.Controllers.Dialog;
using Kingmaker.DialogSystem;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Editor.NodeEditor.Utility;
using Kingmaker.Editor.NodeEditor.Window;
using Kingmaker.ElementsSystem;
using Kingmaker.ElementsSystem.Interfaces;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.Utility.DotNetExtensions;
using Owlcat.Editor.Core.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Kingmaker.Editor.NodeEditor.Nodes
{
	public class AnswerNode : EditorNode<BlueprintAnswer>, IForceableConditionNode
	{
		public AnswerNode(Graph graph, BlueprintAnswer asset) : base(graph, asset, new Vector2(200, 80))
		{
		}

		private bool IsFinalAnswer
			=> !Asset.NextCue.Cues.Any();

		public override Color GetWindowColor()
		{
			return Colors.AnswerWindow;
		}

		public override string GetText()
		{
#if UNITY_EDITOR && EDITOR_FIELDS
			return Asset.Text.GetText(LocalizationManager.Instance.CurrentLocale);
#else
			return "";
#endif
		}

		protected override void DrawContent()
		{
			if (IsFinalAnswer)
			{
				DrawFunctions.NodeIcon(Icons.FinalNode, "This answer is final");
			}

			using (GuiScopes.UpdateObject(SerializedObject))
			{
				if (Asset.HasShowCheck)
				{
					Profiler.BeginSample("Show Check");
					using (GuiScopes.Horizontal())
					{
						EditorGUIUtility.fieldWidth = 150;
						EditorGUILayout.PropertyField(SerializedObject.FindProperty("Blueprint.ShowCheck.Type"), new GUIContent());
						EditorGUIUtility.fieldWidth = 20;
						EditorGUILayout.PropertyField(SerializedObject.FindProperty("Blueprint.ShowCheck.DC"), new GUIContent());
					}
					Profiler.EndSample();
				}

				if (Asset.SoulMarkShift.Value != 0)
				{
					Profiler.BeginSample("Alignment");
					using (GuiScopes.Horizontal())
					{
						EditorGUIUtility.fieldWidth = 150;
						EditorGUILayout.PropertyField(SerializedObject.FindProperty("Blueprint.SoulMarkShift.Direction"), new GUIContent());
						EditorGUIUtility.fieldWidth = 20;
						if (SerializedObject.FindProperty("Blueprint.SoulMarkShift.CheckByRank").boolValue)
							EditorGUILayout.PropertyField(SerializedObject.FindProperty("Blueprint.SoulMarkShift.Rank"), new GUIContent());
						else
							EditorGUILayout.PropertyField(SerializedObject.FindProperty("Blueprint.SoulMarkShift.Value"), new GUIContent());
					}
					Profiler.EndSample();
				}

#if UNITY_EDITOR && EDITOR_FIELDS
				Profiler.BeginSample("Find Text Property");
				var property = SerializedObject.FindProperty("Blueprint.Text");
				Profiler.EndSample();

				LocalizationEditorGUI.LocalizedStringField(property, Asset.Text, LocalizationManager.Instance.CurrentLocale, Graph.ShowTagButtons);
#endif
			}
		}

		public override IEnumerable<string> GetMarkers(bool extended)
		{
			if (Asset.ShowOnce)
				yield return "Once";

			if (Asset.HasShowCheck)
			{
				if (extended)
					yield return $"Show Check: {Asset.ShowCheck.Type} ({Asset.ShowCheck.GetDCString()})";
				else
					yield return "Show Check";
			}

			if (Asset.NextCue.Strategy == Strategy.Random)
				yield return "Random";
			if (Asset.CharacterSelection.SelectionType != CharacterSelection.Type.Clear)
				yield return "Selection: " + Asset.CharacterSelection.SelectionType;
			if (Asset.ShowConditions.HasConditions)
				yield return ElementsDescription.Conditions(extended, Asset.ShowConditions);
			if (Asset.SelectConditions.HasConditions)
				yield return ElementsDescription.Conditions(extended, Asset.SelectConditions, "Select Conditions");
			if (Asset.OnSelect.HasActions)
				yield return ElementsDescription.Actions(extended, Asset.OnSelect);
			if (!Asset.SoulMarkRequirement.Empty)
				yield return $"{Asset.SoulMarkRequirement.Direction.ToString()} + {Asset.SoulMarkRequirement.Value}";
#if UNITY_EDITOR && EDITOR_FIELDS
            if (Application.isPlaying && Game.Instance.Player.Dialog.SelectedAnswersContains(Asset))
                yield return "Selected";
            if (Application.isPlaying && Game.Instance.Player.Dialog.AnswerChecksContains(Asset))
                yield return Game.Instance.Player.Dialog.AnswerChecksGet(Asset).ToString();
#endif
        }

        protected override IEnumerable<SimpleBlueprint> GetAllReferencedAssetsInternal()
        {
            return Asset.NextCue.Cues.Dereference();
        }

		protected override SerializedProperty GetListProperty(Type type, SimpleBlueprint r = null)
		{
			if (typeof(BlueprintCueBase).IsAssignableFrom(type))
				return FindProperty("NextCue.Cues");
			return null;
		}

		public BlueprintScriptableObject ForceableAsset => Asset;
	}
}
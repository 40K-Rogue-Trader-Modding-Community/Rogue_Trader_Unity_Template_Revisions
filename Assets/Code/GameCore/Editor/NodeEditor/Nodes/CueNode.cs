using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.DialogSystem;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Editor.NodeEditor.Utility;
using Kingmaker.Editor.NodeEditor.Window;
using Kingmaker.ElementsSystem;
using Kingmaker.ElementsSystem.Interfaces;
using Kingmaker.Localization;
using Kingmaker.UI.Sound;
using Owlcat.Editor.Core.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Code.GameCore.Editor.Validation; // DO NOT REMOVE THIS, breaks ModTemplate

namespace Kingmaker.Editor.NodeEditor.Nodes
{
	public class CueNode : EditorNode<BlueprintCue>, IForceableConditionNode
	{
		private bool m_IsFromSequence;
		private bool m_IsVoiceOverButtonPressed;
		private static GameObject m_DebugCuePlayer;

		public CueNode(Graph graph, BlueprintCue asset) : base(graph, asset, new Vector2(200, 80))
		{
#if UNITY_EDITOR && EDITOR_FIELDS
			try
			{
				#if OWLCAT_MODS
				BlueprintCueValidationVisitor.UpdateSpeaker(asset);
				#else
				asset.UpdateSpeaker();
				#endif
			}
			catch (Exception e)
			{
				LocalizedString.Logger.Error(e);
			}
#endif
		}

		public bool IsFromSequence
		{
			get => m_IsFromSequence;

			set
			{
				m_IsFromSequence = value;

				// Mark all children as referenced from sequence as well
				foreach (var node in ReferencedNodes)
				{
					if (node is CueNode cueNode)
					{
						cueNode.IsFromSequence = m_IsFromSequence;
					}
				}
			}
		}

		private bool IsFinalCue
			=> !IsFromSequence
			   && !Asset.Answers.Any()
			   && !Asset.Continue.Cues.Any();

		public override Color GetWindowColor()
		{
			return IsFinalCue ? Colors.CueWindowFinal : Colors.CueWindow;
		}

		public override string GetText()
		{
#if UNITY_EDITOR && EDITOR_FIELDS
			return Asset.Text.GetText(LocalizationManager.Instance.CurrentLocale);
#else
			return "";
#endif
		}

		public override void BeforeDrawConnections()
		{
			base.BeforeDrawConnections();
			IsFromSequence = false;
		}

		protected override void DrawContent()
		{
			using (GuiScopes.Horizontal())
			{
				if (IsFinalCue)
				{
					DrawFunctions.NodeIcon(Icons.FinalNode, "This cue is final");
				}

				if (Application.isPlaying)
				{
					string voiceOver = Asset.Text.GetVoiceOverSound();
					if (!string.IsNullOrEmpty(voiceOver))
					{
						DrawFunctions.IconButton(
							Icons.Play,
							"Play voice over",
							() => PlayVoiceOver(voiceOver),
							ref m_IsVoiceOverButtonPressed);
					}
				}

			}

			using (GuiScopes.UpdateObject(SerializedObject))
			{
				if (Asset.SoulMarkShift.Value != 0)
				{
					Profiler.BeginSample("Alignment");
					using (GuiScopes.Horizontal())
					{
						EditorGUIUtility.fieldWidth = 150;
						EditorGUILayout.PropertyField(FindProperty("SoulMarkShift.Direction"), new GUIContent());
						EditorGUIUtility.fieldWidth = 20;
						EditorGUILayout.PropertyField(FindProperty("SoulMarkShift.Value"), new GUIContent());
					}
					Profiler.EndSample();
				}

				if (Asset.Speaker.NoSpeaker)
				{
					GUILayout.Label("No Speaker", GUI.skin.button);
				}
				else if (Asset.Speaker.Blueprint != null)
				{
					Profiler.BeginSample("Speaker Field");
					using (GuiScopes.LabelWidth(40))
					{
						EditorGUILayout.PropertyField(FindProperty("Speaker.m_Blueprint"), new GUIContent());
					}
					Profiler.EndSample();
				}

#if UNITY_EDITOR && EDITOR_FIELDS
				Profiler.BeginSample("Find Text Property");
				var property = FindProperty("Text");
				Profiler.EndSample();

				LocalizationEditorGUI.LocalizedStringField(property, Asset.Text, LocalizationManager.Instance.CurrentLocale, Graph.ShowTagButtons);
#endif
			}
		}

		private static void PlayVoiceOver(string voiceOver)
		{
			EditorApplication.ExecuteMenuItem("Window/General/Game");

			AkSoundEngine.StopAll();

			m_DebugCuePlayer ??= new GameObject("DebugCuePlayer");
			VoiceOverPlayer.PlayVoiceOver(voiceOver, m_DebugCuePlayer);
		}

		protected override IEnumerable<SimpleBlueprint> GetAllReferencedAssetsInternal()
		{
			return Asset.Continue.Cues.Dereference()
                .Cast<SimpleBlueprint>()
				.Concat(Asset.Answers.Dereference());
		}

		protected override SerializedProperty GetListProperty(Type type, SimpleBlueprint r = null)
		{
			if (typeof(BlueprintCueBase).IsAssignableFrom(type))
				return FindProperty("Continue.Cues");
			if (typeof(BlueprintAnswerBase).IsAssignableFrom(type))
				return FindProperty("Answers");
			return null;
		}

		public override IEnumerable<string> GetMarkers(bool extended)
		{
			if (Asset.ShowOnce)
				yield return "Once";
			if (Asset.Continue.Strategy == Strategy.Random)
				yield return "Random";
			if (Asset.Conditions.HasConditions)
				yield return ElementsDescription.Conditions(extended, Asset.Conditions);
			if (Asset.OnShow.HasActions || Asset.OnStop.HasActions)
				yield return ElementsDescription.Actions(extended, Asset.OnShow, Asset.OnStop);
#if UNITY_EDITOR && EDITOR_FIELDS
            if (Application.isPlaying && Game.Instance.Player.Dialog.ShownCuesContains(Asset))
                yield return "Seen";
#endif
		}

		public BlueprintScriptableObject ForceableAsset => Asset;
	}
}
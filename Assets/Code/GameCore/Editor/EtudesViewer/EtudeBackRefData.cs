using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.Blueprints;
using UnityEditor;
using UnityEngine;

#nullable enable

namespace Kingmaker.Assets.Code.Editor.EtudesViewer
{
	public class EtudeBackRefData
	{
		private const float LabelHeight = 20;
		private const float RefHeight = 20;
		private const float ButtonWidth = 64;

		private Rect LabelRect { get; set; }
		private readonly List<Rect> m_RefRects = new(16);

		private EtudeBackReference[]? m_BackReferences;
		private EtudeBackReference.Kind m_ReferenceKind = EtudeBackReference.Kind.Other;

		public void InitRects(float x, float y, float sizeX, ref float offsetY, bool foldout)
		{
			m_RefRects.Clear();

			int refCount = m_BackReferences?.Length ?? 0;
			if (refCount == 0)
			{
				return;
			}

			LabelRect = new Rect(x, y + offsetY, sizeX, LabelHeight);
			offsetY += LabelHeight;

			if (!foldout)
			{
				return;
			}

			for (int i = 0; i < refCount; i++)
			{
				m_RefRects.Add(new Rect(x, y + offsetY, sizeX, RefHeight));
				offsetY += RefHeight;
			}
		}

		public void InitReferences(IEnumerable<EtudeBackReference> refs, EtudeBackReference.Kind refKind)
		{
			m_BackReferences = refs
				.Where(r => r.ReferenceKind == refKind)
				.ToArray();
			m_ReferenceKind = refKind;
		}

		public void OnGui(ref bool foldout)
		{
			if (m_BackReferences is not {Length: > 0})
			{
				return;
			}

			foldout = EditorGUI.Foldout(LabelRect, foldout, $"{m_ReferenceKind} in ({m_BackReferences.Length})", EditorStyles.foldout);
			if (!foldout || m_RefRects.Count != m_BackReferences.Length)
			{
				return;
			}

			for (int i = 0; i < m_RefRects.Count; i++)
			{
				var rect = m_RefRects[i];
				var backRef = m_BackReferences[i];
				var bp = backRef.Blueprint?.GetBlueprint();

				var buttonRect = rect;
				buttonRect.xMax -= rect.width - ButtonWidth;
				if (GUI.Button(buttonRect, "Select", EditorStyles.miniButtonLeft))
				{
					if (backRef.SceneAsset != null)
					{
						Selection.activeObject = backRef.SceneAsset;
					}
					else if (bp != null)
					{
						Selection.activeObject = BlueprintEditorWrapper.Wrap(bp);
					}
				}

				var labelRect = rect;
				labelRect.xMin += ButtonWidth;
				if (backRef.SceneAsset != null)
				{
					string scenePath = AssetDatabase.GetAssetPath(backRef.SceneAsset);
					string sceneName = Path.GetFileNameWithoutExtension(scenePath);
					string labelText = $"{sceneName} [{backRef.ObjectName}.{backRef.ComponentType}]";
					GUI.Label(labelRect, new GUIContent(labelText, $"Scene path:\n{scenePath}"));
				}
				else if (bp != null)
				{
					string labelText = $"{bp.NameSafe()} [{bp.GetType().Name}]";
					GUI.Label(labelRect, new GUIContent(labelText, $"Property path:\n{backRef.ShortPath}\n\nPath with types:\n{backRef.VerbosePath}"));
				}
				else
				{
					GUI.Label(labelRect, "Error");
				}
			}
		}
	}
}
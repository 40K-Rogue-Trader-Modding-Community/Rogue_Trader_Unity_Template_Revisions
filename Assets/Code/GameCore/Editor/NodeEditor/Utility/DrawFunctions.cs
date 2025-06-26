using System;
using Kingmaker.Editor.NodeEditor.Nodes;
using Kingmaker.Editor.NodeEditor.Window;
using Owlcat.Editor.Core.Utility;
using UnityEditor;
using UnityEngine;

#nullable enable

namespace Kingmaker.Editor.NodeEditor.Utility
{
	public static class DrawFunctions
	{
		public static void Connection(CanvasView view, EditorNode n1, EditorNode n2, Color color)
		{
			Connection(view, n1, n1.Size.y / 2, n2, n2.Size.y / 2, color);
		}

		public static void Connection(CanvasView view, EditorNode n1, float y1, EditorNode n2, Color color)
		{
			Connection(view, n1, y1, n2, n2.Size.y / 2, color);
		}

		public static void Connection(CanvasView view, EditorNode n1, float y1, EditorNode n2, float y2, Color color)
		{
			Vector2 p1 = n1.Center;
			p1.x += n1.Size.x/2;
			p1.y -= n1.Size.y/2;
			p1.y += y1;
			Vector2 p2 = n2.Center;
			p2.x -= n2.Size.x/2;
			p2.y -= n2.Size.y / 2;
			p2.y += y2;

			Vector2 sp1 = view.ToScreen(p1);
			Vector2 sp2 = view.ToScreen(p2);
			Vector2 tg1 = sp1 + 0.5f * new Vector2(Math.Abs(p2.x - p1.x), 0);
			Vector2 tg2 = sp2 - 0.5f * new Vector2(Math.Abs(p2.x - p1.x), 0);

			float width = 5 / view.Scale;
			Handles.DrawBezier(sp1, sp2, tg1, tg2, color, null, width);
		}

		/// <summary>
		/// An icon to draw inside node's DrawContent()
		/// </summary>
		public static void NodeIcon(Texture2D? icon, string tooltip, Action? action = null)
		{
			if (icon == null)
			{
				return;
			}

			using (GuiScopes.Horizontal())
			{
				EditorGUILayout.Space();
				GUILayout.Box(new GUIContent("", tooltip), GUIStyle.none,
					GUILayout.Width(icon.width), GUILayout.Height(icon.height));

				var rect = GUILayoutUtility.GetLastRect();

				GUI.DrawTexture(rect, icon);
				EditorGUILayout.Space();
			}
		}

		/// <summary>
		/// A button with icon and pressed state to draw inside node's DrawContent()
		/// </summary>
		public static void IconButton(Texture2D? icon, string tooltip, Action action, ref bool isPressed)
		{
			if (icon == null)
			{
				return;
			}

			using (GuiScopes.Horizontal())
			{
				EditorGUILayout.Space();

				GUILayout.Box(new GUIContent("", tooltip), GUIStyle.none,
					GUILayout.Width(icon.width), GUILayout.Height(icon.height));

				bool drawButton = Event.current.type != EventType.Layout && Event.current.type != EventType.Used;

				if (drawButton)
				{
					var rect = GUILayoutUtility.GetLastRect();

					if (rect.Contains(Event.current.mousePosition))
					{
						switch (Event.current.type)
						{
							case EventType.MouseDown when Event.current.button == 0:
								isPressed = true;
								Event.current.Use();
								break;

							case EventType.MouseUp when Event.current.button == 0 && isPressed:
								isPressed = false;
								action();
								Event.current.Use();
								break;
						}
					}
					else
					{
						isPressed = false;
					}

					GUI.DrawTexture(rect, icon, ScaleMode.StretchToFill, true, 0.0f,
						isPressed ? Color.gray : Color.white,
						0.0f, 0.0f);
				}

				EditorGUILayout.Space();
			}
		}
	}
}
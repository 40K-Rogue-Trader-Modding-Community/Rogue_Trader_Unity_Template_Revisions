using UnityEditor;
using UnityEngine;

#nullable enable

namespace Kingmaker.Editor.NodeEditor.Utility
{
	public static class Icons
	{
		private static Texture2D? s_FinalNode;
		private static Texture2D? s_Play;

		public static Texture2D? FinalNode
		{
			get
			{
				if (s_FinalNode == null)
				{
					s_FinalNode = EditorGUIUtility.Load("Icons/final_node.png") as Texture2D;
				}
				return s_FinalNode;
			}
		}

		public static Texture2D? Play
		{
			get
			{
				if (s_Play == null)
				{
					s_Play = EditorGUIUtility.Load("Icons/play.png") as Texture2D;
				}
				return s_Play;
			}
		}
	}
}
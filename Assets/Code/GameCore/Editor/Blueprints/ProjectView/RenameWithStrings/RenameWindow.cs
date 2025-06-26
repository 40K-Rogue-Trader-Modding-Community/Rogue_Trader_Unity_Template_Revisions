using UnityEditor;
using UnityEngine;

#nullable enable

namespace Kingmaker.Editor.Localization
{
	public class RenameWindow : EditorWindow
	{
		private string? m_SrcName;
		private string? m_DstName;

		public delegate bool RenameDelegate(string srcName, string dstName, out string resultMessage);
		private RenameDelegate? m_RenameDelegate;

		public void Set(string windowTitle, string srcName, RenameDelegate renameDelegate)
		{
			titleContent = new GUIContent(windowTitle);
			minSize = new Vector2(720, 64);
			maxSize = new Vector2(1280, 64);

			m_SrcName = srcName;
			m_DstName = srcName;
			m_RenameDelegate = renameDelegate;
		}

		private void OnGUI()
		{
			using var verticalScope = new EditorGUILayout.VerticalScope();

			EditorGUIUtility.labelWidth = 64;
			string? srcValue = m_SrcName;
			EditorGUILayout.TextField("From", srcValue); // Provide dummy copy to disable editing
			m_DstName = EditorGUILayout.TextField("To", m_DstName);
			EditorGUIUtility.labelWidth = 0;

			var currentEvent = Event.current;
			bool isEscPressed = currentEvent is {type: EventType.KeyDown, keyCode: KeyCode.Escape};
			bool isReturnPressed = currentEvent is {type: EventType.KeyDown, keyCode: KeyCode.Return};
			bool isEnterPressed = currentEvent is {type: EventType.KeyDown, keyCode: KeyCode.KeypadEnter};

			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Rename") || isReturnPressed || isEnterPressed)
				{
					Close();
					DoRename();
				}
				if (GUILayout.Button("Cancel") || isEscPressed)
				{
					Close();
				}
			}
		}

		private void DoRename()
		{
			bool isSuccess = false;
			string result = "Rename failed.";
			if (m_RenameDelegate != null && m_SrcName != null && m_DstName != null)
			{
				isSuccess = m_RenameDelegate(m_SrcName, m_DstName, out string resultMessage);
				result = resultMessage;
			}

			// Delay call to avoid 'EndLayoutGroup: BeginLayoutGroup must be called first.' error
			EditorApplication.delayCall += () => EditorUtility.DisplayDialog(
				isSuccess ? "Rename complete" : "ERROR",
				result,
				"Ok");
		}
	}
}
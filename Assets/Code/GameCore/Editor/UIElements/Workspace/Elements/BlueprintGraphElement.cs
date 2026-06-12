using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using UnityEditor;

namespace Kingmaker.Editor.UIElements.Workspace.Elements
{
	public class BlueprintGraphElement : OwlcatWorkspaceGraphElement
	{
		public readonly BlueprintScriptableObject Blueprint;
		
		public BlueprintGraphElement(BlueprintScriptableObject blueprint) : 
			base(new SerializedObject(BlueprintEditorWrapper.Wrap(blueprint)))
		{
			Blueprint = blueprint;
		}
	}
}
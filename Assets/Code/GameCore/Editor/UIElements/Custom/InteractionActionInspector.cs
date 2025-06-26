using Kingmaker.View.MapObjects;
using UnityEditor;

namespace Kingmaker.Editor.UIElements.Custom
{
	[CustomEditor(typeof(InteractionAction))]
	public class InteractionActionInspector : CustomFieldsInspector<InteractionAction>
	{
		protected override string DataKeyRoot
			=> nameof(InteractionActionInspectorLayout);

		protected override string LayoutAssetPath
			=> "Assets/Editor/InspectorLayouts/InteractionActionInspectorLayout.asset";
	}
}
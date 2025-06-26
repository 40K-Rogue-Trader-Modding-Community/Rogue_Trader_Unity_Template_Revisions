using Kingmaker.View.MapObjects;
using UnityEditor;

#nullable enable

namespace Kingmaker.Editor.UIElements.Custom
{
	[CustomEditor(typeof(InteractionSkillCheck))]
	public class InteractionSkillCheckInspector : CustomFieldsInspector<InteractionSkillCheck>
	{
		protected override string DataKeyRoot
			=> nameof(InteractionSkillCheckInspectorLayout);

		protected override string LayoutAssetPath
			=> "Assets/Editor/InspectorLayouts/InteractionSkillCheckInspectorLayout.asset";
	}
}
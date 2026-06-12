using Kingmaker.Editor.UIElements.ValuePicker;
using Kingmaker.ResourceLinks;
using Kingmaker.View;
using UnityEditor;

namespace Kingmaker.Editor.Blueprints
{
	[CustomPropertyDrawer(typeof(UnitViewLink))]
	public class UnitLinkDrawer : WeakLinkDrawer<UnitEntityView>
	{
		protected override bool Filter(AssetPicker.HierarchyEntry he)
		{
			return
				he.Path.StartsWith("Assets/Mechanics/Bundles/Prefabs/Characters/") || 
				he.Path.StartsWith("Assets/Art/Characters/Creatures/") && !he.Path.Contains("VisualPrefabs");
		}
	}
}
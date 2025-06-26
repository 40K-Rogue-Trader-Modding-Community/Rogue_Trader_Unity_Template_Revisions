using Kingmaker.UnitLogic.Interaction;
using Kingmaker.View.MapObjects;
using UnityEditor;

namespace Kingmaker.Editor.UIElements.Custom
{
    [CustomEditor(typeof(SpawnerInteractionBark))]
    public class SpawnerInteractionBarkInspector : CustomFieldsInspector<SpawnerInteractionBark>
    {
        protected override string DataKeyRoot
            => nameof(SpawnerInteractionBarkInspector);

        protected override string LayoutAssetPath
            => "Assets/Editor/InspectorLayouts/SpawnerInteractionBarkInspectorLayout.asset";
    }
}
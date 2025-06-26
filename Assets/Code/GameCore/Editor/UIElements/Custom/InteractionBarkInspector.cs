using Kingmaker.View.MapObjects;
using UnityEditor;

namespace Kingmaker.Editor.UIElements.Custom
{
    [CustomEditor(typeof(InteractionBark))]
    public class InteractionBarkInspector : CustomFieldsInspector<InteractionBark>
    {
        protected override string DataKeyRoot
            => nameof(InteractionBarkInspectorLayout);

        protected override string LayoutAssetPath
            => "Assets/Editor/InspectorLayouts/InteractionBarkInspectorLayout.asset";
    }
}
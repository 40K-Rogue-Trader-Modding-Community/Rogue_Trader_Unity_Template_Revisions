#if UNITY_EDITOR && EDITOR_FIELDS
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Editor.AreaStatesWindow;
using UnityEngine.UIElements;

#nullable enable

namespace Kingmaker.Editor.Blueprints
{
    [BlueprintCustomEditor(typeof(BlueprintArea))]
    public class AreaInspector : AreaPartInspector
    {
        public override VisualElement OnBeforeComponentsElement(SimpleBlueprint bp)
        {
            if (bp is BlueprintArea area)
            {
                var states = new AreaStatesElement(area);
                states.UpdateStates();
                return states;
            }
            return new AreaStatesElement(null);
        }
    }
}
#endif
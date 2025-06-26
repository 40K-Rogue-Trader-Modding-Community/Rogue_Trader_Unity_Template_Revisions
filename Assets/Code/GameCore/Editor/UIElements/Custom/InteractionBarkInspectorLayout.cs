using System.Collections.Generic;
using System.Linq;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.View.MapObjects;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.UIElements.Custom
{
    [CreateAssetMenu(menuName = "ScriptableObjects/" + nameof(InteractionBarkInspectorLayout), fileName = nameof(InteractionBarkInspectorLayout))]
    public class InteractionBarkInspectorLayout : CustomFieldsInspectorLayout<InteractionBark>
    {
        public override List<string> GetAllPropertyPaths(SerializedObject so)
        {
            var allPaths = new List<string>();
            allPaths.AddRange(so.FindProperty(nameof(InteractionBark.Settings))
                .GetChildren()
                .Select(p => p.propertyPath));
            return allPaths;
        }
    }
}
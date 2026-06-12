using Kingmaker.Editor.Blueprints;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Utility
{
    [CustomPropertyDrawer(typeof(Bounds))]
    public class BoundsDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            PrototypedObjectEditorUtility.ShowPropertyRecursive(property);
        }
    }
}
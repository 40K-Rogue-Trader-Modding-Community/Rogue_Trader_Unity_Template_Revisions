using System;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.Editor;
using Kingmaker.Editor.UIElements.Custom;
using Owlcat.Runtime.Core.Utility;
using Owlcat.Utility.Attributes;
using UnityEditor;
using UnityEngine.UIElements;

namespace Owlcat.Editor.Framework.Code.EditorFramework.Drawers
{
    [CustomPropertyDrawer(typeof(Enum))]
    public class EnumDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var enumFilterAttribute = property.GetFieldInfo().GetAttribute<EnumFilterAttribute>();
            var filteredList = enumFilterAttribute?.GetFilteredList(property);

            if (enumFilterAttribute == null || filteredList == null || filteredList.Count == 0)
                return new OwlcatPropertyField(property);
            
            return new FilteredEnumProperty(property, filteredList);
        }
    }
}
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Code.Editor.Utility;
using UnityEditor;

namespace Kingmaker.Editor.UIElements.Custom.Properties
{
    public class GenericTypeOwlcatProperty : OwlcatProperty
    {
        public GenericTypeOwlcatProperty(SerializedProperty property, Layout layout) : base(property, layout)
        {
            CreateContent();
        }

        protected override void CreateContentInternal()
        {
            base.CreateContentInternal();
            
            foreach (var child in Property.GetChildren())
            {
                var field = UIElementsUtility.CreatePropertyElement(child, false);
                ContentContainer.Add(field);
            }
        }
    }
}
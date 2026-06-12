using Code.Framework.Editor.Utility;
using Kingmaker.Editor.UIElements.Custom.Base;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Properties
{
    public class WrappedOwlcatProperty : OwlcatProperty
    {
        private readonly VisualElement m_Element;
        
        public WrappedOwlcatProperty(SerializedProperty property, VisualElement element, Layout layout) : 
            base(property, layout)
        {
            m_Element = element;
            if (m_Element is IBindable bindable)
                bindable.BindProperty(property);
            
            if (element.IsBaseFieldSubclass())
                OwlcatPropertyField.ProcessBaseField(this, element);

            m_Element.AddToClassList("owlcat-inner-field");
            ContentContainer.Add(m_Element);
        }
    }
}
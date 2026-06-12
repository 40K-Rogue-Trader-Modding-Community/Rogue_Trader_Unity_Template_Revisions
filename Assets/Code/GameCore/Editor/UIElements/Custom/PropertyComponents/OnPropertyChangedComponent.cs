using System.Reflection;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Utility.Attributes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = System.Object;
 
namespace Kingmaker.Editor.UIElements.Custom.PropertyComponents
{
    public class OnPropertyChangedComponent: OwlcatPropertyComponent
    {
        private readonly OnValueChangedAttribute m_Attribute;
        private readonly VisualElement m_Element;
       
        private MethodInfo m_Method;
        private Object m_Target;
       
        public OnPropertyChangedComponent(VisualElement element, OnValueChangedAttribute attribute)
        {
            m_Attribute = attribute;
            m_Element = element;
        }
 
        protected override void OnAttached()
        {
            base.OnAttached();
           
            var targetObject = Property.Property.serializedObject.targetObject;
           
            if (targetObject is BlueprintEditorWrapper bew)
                m_Target = bew.Blueprint;
            else
                m_Target = targetObject;
           
            m_Method = m_Target?.GetType().GetMethod(m_Attribute.MethodName,
                BindingFlags.Public | BindingFlags.NonPublic | 
                BindingFlags.Instance | BindingFlags.Static);
           
            m_Element.TrackPropertyValue(Property.Property, OnPropertyChanged);
        }
 
        private void OnPropertyChanged(SerializedProperty obj)
        {
            m_Method?.Invoke(m_Target, null);
        }
    }
}
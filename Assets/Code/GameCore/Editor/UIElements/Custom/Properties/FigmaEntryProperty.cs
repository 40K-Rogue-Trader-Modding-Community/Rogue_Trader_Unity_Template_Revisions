using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Properties;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Editor.UIElements.Custom.PropertyComponents;
using UnityEditor;
using UnityEditor.UIElements;

namespace Kingmaker.Editor.UIElements.Custom
{
    public class FigmaEntryProperty : OwlcatProperty
    {
        private readonly SerializedProperty m_Name;
        private readonly SerializedProperty m_Sprite;
        private readonly SerializedProperty m_Removed;
        
        public FigmaEntryProperty(SerializedProperty property, SerializedProperty nameProp, SerializedProperty sprite,
            SerializedProperty removed) : base(property)
        {
            m_Name = nameProp;
            m_Sprite = sprite;
            m_Removed = removed;
            
            AddComponent(new FuncTitleProviderComponent(() => m_Name.stringValue));
            CreateContent();
        }

        protected override void CreateContentInternal()
        {
            base.CreateContentInternal();
            
            var objectField = new OwlcatObjectField(m_Name.stringValue, FieldFromProperty.GetFieldInfo(m_Sprite));
            objectField.bindingPath = m_Sprite.propertyPath;
            objectField.BindProperty(m_Sprite);
            objectField.SetValueWithoutNotify(m_Sprite.objectReferenceValue);
            objectField.SetEnabled(!m_Removed.boolValue);
            
            ContentContainer.Add(objectField);
        }
    }
}
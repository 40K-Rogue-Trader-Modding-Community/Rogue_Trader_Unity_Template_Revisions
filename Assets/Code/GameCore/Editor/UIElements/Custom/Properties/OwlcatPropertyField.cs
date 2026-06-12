using System;
using System.Linq;
using System.Reflection;
using Code.Framework.Editor.Utility;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Blueprints.Attributes;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.Editor.UIElements.Custom.PropertyComponents;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom
{
    public class OwlcatPropertyField : OwlcatProperty
    {
        protected PropertyField m_InnerField;
        
        public OwlcatPropertyField(SerializedProperty property) : base(property, GetLayout(property))
        {
            CreateContent();
        }

        protected override void CreateContentInternal()
        {
            base.CreateContentInternal();

            m_InnerField = new PropertyField(Property, Property.displayName) { name = Property.propertyPath };
            m_InnerField.BindProperty(Property);
            m_InnerField.AddToClassList("owlcat-inner-field");
            MethodInfo resetMethod = typeof(PropertyField).GetMethod("Reset",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                CallingConventions.HasThis,
                new[] { typeof(SerializedProperty) },
                null);
            
            // call reset to create child field immediately
            resetMethod?.Invoke(m_InnerField, new object[] { Property });
                    
            var children = m_InnerField.Children().ToList();
            if (children.Count > 0)
            {
                var child = children[0].ClassListContains("unity-decorator-drawers-container") ? 
                    children[1] : children[0];

                ProcessBaseField(this, child);
            }

            ContentContainer.Add(m_InnerField);
        }

        private static Layout GetLayout(SerializedProperty property)
        {
            if (property.propertyType is SerializedPropertyType.Bounds or SerializedPropertyType.BoundsInt)
                return Layout.VerticalNotExpandable;
            
            var info = property.GetFieldInfo();
            var layout = info != null && info.HasAttribute<VerticalLayoutAttribute>()
                ? Layout.VerticalNotExpandable
                : Layout.Horizontal;
            
            return layout;
        }

        public static void ProcessBaseField(OwlcatProperty property, VisualElement baseField)
        {
            var dragger = baseField.GetTextValueFieldDragger();
            if (dragger != null && !property.HasComponent<ReadOnlyComponent>())
            {
                dragger.SetDragZone(property.HeaderContainer);
                property.HeaderContainer.AddToClassList("unity-base-field__label--with-dragger");
                property.TitleLabel.AddToClassList("unity-base-field__label--with-dragger");
            }

            if (baseField is IMGUIContainer)
                property.HeaderContainer.style.display = DisplayStyle.None;
            
            property.HeaderContainer.RegisterRightClickMenu(property.Property);
            var validationValue = GetValidationValue(baseField);
            if (validationValue.Value != null)
            {
                baseField.RegisterCallback<AttachToPanelEvent>(e =>
                {
                    validationValue.FieldInfo.SetValue(baseField, validationValue.Value);
                });
            }
        }
        
        private static ValidationFieldData GetValidationValue(object baseFieldInstance)
        {
            Type type = baseFieldInstance.GetType();
            FieldInfo fieldInfo = null;
            while (type != null && fieldInfo == null)
            {
                fieldInfo = type.GetField("onValidateValue",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                type = type.BaseType;
            }

            if (fieldInfo != null)
            {
                return new ValidationFieldData
                    { FieldInfo = fieldInfo, Value = fieldInfo.GetValue(baseFieldInstance) };
            }

            return default;
        }

        private struct ValidationFieldData
        {
            public object Value;
            public FieldInfo FieldInfo;
        }
    }
}
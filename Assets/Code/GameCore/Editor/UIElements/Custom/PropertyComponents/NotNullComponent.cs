using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.PropertyComponents
{
    public class NotNullComponent : OwlcatPropertyComponent
    {
        private SerializedProperty m_Property;
        private VisualElement m_HeaderElement;
        private VisualElement m_ContentElement;
        private VisualElement m_NotNullLabel;

        public NotNullComponent() { }

        public NotNullComponent(SerializedProperty property, VisualElement headerElement, VisualElement contentElement)
        {
            m_Property = property;
            m_HeaderElement = headerElement;
            m_ContentElement = contentElement;
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            if (m_Property == null)
            {
                m_Property = Property.Property;
                m_HeaderElement = Property.HeaderContainer;
                m_ContentElement = Property.ContentContainer;
            }

            m_NotNullLabel = new Label("[NOT NULL]");
            m_NotNullLabel.style.flexShrink = 0;
            m_NotNullLabel.style.flexGrow = 0;
            m_NotNullLabel.style.width = StyleKeyword.Auto;
            m_NotNullLabel.style.height = StyleKeyword.Auto;
            m_NotNullLabel.style.alignSelf = Align.Center;
            m_NotNullLabel.focusable = false;

            m_HeaderElement.Add(m_NotNullLabel);
            Update();
        }

        protected override void OnDetached()
        {
            base.OnDetached();
            m_NotNullLabel.RemoveFromHierarchy();
        }

        public void Update()
        {
            object fieldValue = FieldFromProperty.GetFieldValue(m_Property);
            bool isNull = fieldValue == null ||
                          (fieldValue is string s && string.IsNullOrEmpty(s)) ||
                          (fieldValue is BlueprintReferenceBase refBase && refBase.IsEmpty()) ||
                          (fieldValue is Object obj && obj == null);

            m_NotNullLabel.style.display = isNull ? DisplayStyle.Flex : DisplayStyle.None;
            SetStyle(isNull);
        }

        private void SetStyle(bool isNull)
        {
            foreach (var ve in m_HeaderElement.Children())
            {
                if (isNull)
                    ve.AddToClassList("owlcat-header-null");
                else
                    ve.RemoveFromClassList("owlcat-header-null");
            }

            var display = m_ContentElement.Q(className: "unity-object-field-display");
            if (display == null || !Property.IsClosestParentOf(display))
                return;

            display.EnableInClassList("owlcat-content-null", isNull);
        }

        public void MoveLabel(int index)
        {
            m_NotNullLabel.parent.Insert(index, m_NotNullLabel);
        }
    }
}
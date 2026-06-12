using System;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Utility.EditorPreferences;
using UnityEditor;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Properties
{
    public class ExpressionCalculatorProperty : OwlcatProperty
    {
        private bool m_GuiFocused;
        private IMGUIContainer m_CustomGUI;

        public ExpressionCalculatorProperty(SerializedProperty property) : base(property, Layout.Vertical)
        {
            CreateContent();
        }

        protected override void CreateContentInternal()
        {
            base.CreateContentInternal();

            var root = Property.FindPropertyRelative("RootExpression");
            var variables = Property.FindPropertyRelative("Variables");

            if (EditorPreferences.Instance.Codewriter)
            {
                var field = UIElementsUtility.CreatePropertyElement(root, false);
                ContentContainer.Add(field);
            }

            var variablesProperty = UIElementsUtility.CreatePropertyElement(variables, false);
            ContentContainer.Add(variablesProperty);
        }

        public void CreateCustomGUI(Action onGUI)
        {
            m_CustomGUI = new IMGUIContainer(onGUI);
            ControlsContainer.Add(m_CustomGUI);
            HeaderContainer.style.flexGrow = 0;
            ControlsContainer.style.flexGrow = 1;
            m_CustomGUI.style.flexGrow = 1;
        }
    }
}
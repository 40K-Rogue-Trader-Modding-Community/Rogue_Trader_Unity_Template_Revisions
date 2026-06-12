using System;
using Code.Framework.Editor.Utility;
using Code.GameCore.Editor.Elements.Debug;
using Code.GameCore.ElementsSystem;
using JetBrains.Annotations;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Editor.Elements;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Properties;
using Kingmaker.UnitLogic.Progression;
using Kingmaker.Utility.EditorPreferences;
using Kingmaker.Utility.UnityExtensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.PropertyComponents
{
    public class ElementListTitleProvider : IOwlcatPropertyTitleProvider
    {
        private readonly SerializedProperty m_ListProperty;
        
        private OwlcatProperty m_Property;
        private Label m_Complexity;
        private string m_BaseTitle;
        
        public int Order { get; }

        public ElementListTitleProvider(SerializedProperty listProperty)
        {
            m_ListProperty = listProperty;
        }
        
        public void AttachToProperty(OwlcatProperty property)
        {
            m_Property = property;

            if (EditorPreferences.Instance.Codewriter)
            {
                m_Complexity = new Label();
                m_Property.ControlsContainer.Insert(0, m_Complexity);
            }
            
            SetResultAndColor();
            m_Property.schedule.Execute(SetResultAndColor).Every(1000).Until(() => m_Property?.Property == null || m_ListProperty == null);
            m_Property.TitleLabel.RegisterCallback<FocusEvent>(x => SetResultAndColor());
            m_Property.TitleLabel.RegisterCallback<BlurEvent>(x => SetResultAndColor());
        }
        
        public void DetachFromProperty() { }
        
        public string GetTitle()
        {
            if (m_Complexity != null)
            {
                if (FieldFromProperty.GetFieldValue(m_ListProperty) is ActionList complexity)
                {
                    m_Complexity.style.display = DisplayStyle.Flex;
                    //m_Complexity.text = complexity.GetAlgorithmicComplexity().ToString();
                }
                else
                {
                    m_Complexity.style.display = DisplayStyle.None;
                }
            }

            if (!m_ListProperty.isArray)
            {
                m_BaseTitle = $"{m_Property.Property.displayName}:";
            }
            else if (m_ListProperty.arraySize is 0 or > 1)
            {
                m_BaseTitle = $"{m_Property.Property.displayName}: ({m_ListProperty.arraySize})";
            }
            else
            {
                var value = FieldFromProperty.GetFieldValue(m_ListProperty.GetArrayElementAtIndex(0)) as Element;
                var pcp = m_Property.GetFirstAncestorWhere(e =>
                    e is OwlcatProperty op && op.Property.type == nameof(PropertyCalculator)) as OwlcatProperty;
                
                PropertyCalculator pc = null;
                if (pcp != null)
                    pc = FieldFromProperty.GetFieldValue(pcp.Property) as PropertyCalculator;

                string caption;
                using ((IDisposable) (pc != null ? FormulaTargetScope.Enter(pc.TargetType, false) : null))
                {
                    caption = value?.GetCaption();
                }

                m_BaseTitle = $"{m_Property.Property.displayName}: {caption}";
            }

            return m_BaseTitle;
        }

        private void SetResultAndColor()
        {
            if (m_Property?.Property == null || m_ListProperty?.serializedObject?.targetObject == null)
                return;
            
            object elementsListObject = FieldFromProperty.GetFieldValue(m_Property.Property);
            if (elementsListObject is not ElementsList elementsList)
                return;
            
            var listInfo = ElementsDebuggerDatabase.Get(elementsList);
            var captionSettings = GetResultAndColor(elementsList, listInfo?.LastResult, listInfo?.LastException);
            
            var copiedProp = m_ListProperty.type switch
                 {
                     nameof(ActionList) => m_ListProperty.FindPropertyRelative(nameof(ActionList.Actions)),
                     nameof(ConditionsChecker) => m_ListProperty.FindPropertyRelative(nameof(ConditionsChecker.Conditions)),
                     _ => m_ListProperty
                 };

            m_Property.TitleLabel.style.backgroundColor = CopyPasteController.IsThisCopied(copiedProp) ? 
                new Color(0.1f, 1, 0.1f, 0.1f) : StyleKeyword.Null;

            m_Property.TitleLabel.text = !captionSettings.result.IsNullOrEmpty() ? 
                $"<color=cyan>[{captionSettings.result}]</color> {m_BaseTitle}" : m_BaseTitle;

            m_Property.TitleLabel.style.color = OwlcatProperty.Focused == m_Property ? 
                StyleKeyword.Null : captionSettings.color;
        }
        
        private static (string result, Color color) GetResultAndColor(
            [NotNull] ElementsList list, int? result, [CanBeNull] Exception exception)
        {
            if (exception == null && result == null)
                return ("", GUI.color);
			
            if (exception != null)
                return ("exception", Color.red);

            if (list is ConditionsChecker or PrerequisitesList or PropertyCalculator {IsBool: true})
                return result == 0 ? ("false", Color.yellow) : ("true", Color.green);

            if (list is PropertyCalculator)
                return (result.ToString(), Color.green);

            return ("success", Color.green);
        }
    }
}
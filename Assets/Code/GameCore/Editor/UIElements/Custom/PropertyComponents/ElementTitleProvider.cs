using System;
using Kingmaker.Editor.UIElements.Custom.Base;
using Code.GameCore.Editor.Elements.Debug;
using JetBrains.Annotations;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.Editor.Elements;
using Kingmaker.ElementsSystem;
using Kingmaker.ElementsSystem.Interfaces;
using Kingmaker.EntitySystem.Properties;
using Kingmaker.EntitySystem.Properties.BaseGetter;
using Kingmaker.UnitLogic.Progression.Prerequisites;
using Kingmaker.Utility.UnityExtensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

namespace Kingmaker.Editor.UIElements.Custom.PropertyComponents
{
	public class ElementTitleProvider : OwlcatPropertyComponent, IOwlcatPropertyTitleProvider
	{
		public int Order { get; } = 0;
        
        private string m_BaseTitle;

		protected override void OnAttached()
		{
            SetResultAndColor();
            Property.schedule.Execute(SetResultAndColor).Every(1000).Until(() => Property == null);
            Property.TitleLabel.RegisterCallback<FocusEvent>(x => SetResultAndColor());
            Property.TitleLabel.RegisterCallback<BlurEvent>(x => SetResultAndColor());
		}

		public string GetTitle()
		{
            string caption = null;
            try
            {
                if (Property.Property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    var captionHolder = Property.Property.objectReferenceValue as IHaveCaption;
                    caption = captionHolder?.Caption;
                }
                else if (Property.Property.propertyType == SerializedPropertyType.ManagedReference)
                {
                    var pcProperty = Property.Property.GetFirstAncestorWhere(p => 
                        p.type == nameof(PropertyCalculator));
                    
                    PropertyCalculator pc = null;
                    if (pcProperty != null)
                        pc = FieldFromProperty.GetFieldValue(pcProperty) as PropertyCalculator;
                    
                    using ((IDisposable) (pc != null ? FormulaTargetScope.Enter(pc.TargetType, false) : null))
                    {
                        var captionHolder = FieldFromProperty.GetFieldValue(Property.Property) as IHaveCaption;
                        caption = captionHolder?.Caption;
                    }
                }
            }
            catch (Exception x)
            {
                PFLog.Default.Exception(x);
                caption = x.Message;
            }
            
            string pn = SerializedPropertyEx.IsArrayElement(Property.Property) ? 
                (Property.Property.GetIndexInParentArray() + 1).ToString() : Property.Property.displayName;
            m_BaseTitle = caption != null ? $"{pn}: {caption}" : pn;
            
            return m_BaseTitle;
        }

        public void SetResultAndColor()
        {
            if (Property?.Property == null)
                return;

            Object value = null;
            try
            {
                if (Property.Property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    value = Property.Property.objectReferenceValue;
                }
                else if (Property.Property.propertyType == SerializedPropertyType.ManagedReference)
                {
                    value = FieldFromProperty.GetFieldValue(Property.Property);
                }
            }
            catch { }

            string result = string.Empty;
            Color? targetColor = null;
            if (value is Element element)
            {
                var debugInfo = ElementsDebuggerDatabase.Get(element);

                Color color;
                if (debugInfo != null)
                {
                    var captionSettings = GetResultAndColor(element, debugInfo.LastResult, debugInfo.LastException);
                    result = captionSettings.result;
                    color = captionSettings.color;
                }
                else
                {
                    color = element.GetCaptionColor();
                }
                
                if (color != Color.clear)
                    targetColor = color;
            }
            
            var copiedProp = Property.Property.type switch
                 {
                     nameof(ActionList) => Property.Property.FindPropertyRelative(nameof(ActionList.Actions)),
                     nameof(ConditionsChecker) => Property.Property.FindPropertyRelative(nameof(ConditionsChecker.Conditions)),
                     _ => Property.Property
                 };

            Property.TitleLabel.style.backgroundColor = CopyPasteController.IsThisCopied(copiedProp) ? 
                new Color(0.1f, 1, 0.1f, 0.1f) : StyleKeyword.Null;

            Property.TitleLabel.text = !result.IsNullOrEmpty() ? 
                $"<color=cyan>[{result}]</color> {m_BaseTitle}" : m_BaseTitle;

            Property.TitleLabel.style.color = OwlcatProperty.Focused == Property || !targetColor.HasValue ? 
                StyleKeyword.Null : targetColor.Value;
        }
        
        private static (string result, Color color) GetResultAndColor(
            [NotNull] Element element, int? result, [CanBeNull] Exception exception)
        {
            if (exception == null && result == null)
                return ("", Color.clear);
            
            if (exception != null)
                return ("exception", Color.red);
            
            if (element is Condition or Prerequisite)
                return result == 0 ? ("false", Color.yellow) : ("true", Color.green);
            
            if (element is PropertyGetter or IntEvaluator)
                return (result.ToString(), Color.green);
            
            return ("success", Color.green);
        }
	}
}
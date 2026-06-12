using System;
using System.Collections.Generic;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Elements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Properties
{
	public class OwlcatTextAreaProperty : OwlcatProperty
	{
        private const int DefaultMinLines = 1;
        private const int DefaultMaxLines = 6;
        
		public OwlcatTextAreaProperty(SerializedProperty property, List<Attribute> attributes) : base(property, Layout.VerticalNotExpandable)
		{
			if (property.propertyType != SerializedPropertyType.String)
				throw new Exception("No valid value for TextAreaProperty");
            
            var textArea = GetTextArea(attributes);
            int minLines = textArea?.minLines ?? DefaultMinLines;
            int maxLines = textArea?.maxLines ?? DefaultMaxLines;
            
			var textField = new OwlcatTextField(multiline: true, scrollable: true)
			{
				value = property.stringValue,
                style =
                {
                    whiteSpace = WhiteSpace.PreWrap,
                    marginRight = 5
                }
            };
            
            textField.SetMinLines(minLines);
            textField.SetMaxLines(maxLines);

            textField.BindProperty(property);
			ContentContainer.Add(textField);
		}
		
		private static bool IsMultiline(List<Attribute> attributes)
		{
			foreach (var attribute in attributes)
			{
				if (attribute is MultilineAttribute)
					return true;
			}
            
			return false;
		}
        
		private static TextAreaAttribute GetTextArea(List<Attribute> attributes)
		{
			foreach (var attribute in attributes)
			{
				if (attribute is TextAreaAttribute areaAttribute)
					return areaAttribute;
			}
            
			return null;
		}
	}
}
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.UIElements.ValuePicker
{
	public class EnumPicker : ValuePicker<Enum>
	{
        protected override string GetValueName(Enum value)
 	    {
 	        var enumType = value.GetType();
 	        string valueName = Enum.GetName(enumType, value);
 	        string inspectorName = GetInspectorName(enumType, valueName);
 	        
 	        return string.IsNullOrEmpty(inspectorName) ? base.GetValueName(value) : inspectorName;
        }
 	    
 	    public static string GetInspectorName(Type enumType, string valueName)
 	    {
 	        if (string.IsNullOrEmpty(valueName))
 	            return null;
 	
 	        var field = enumType.GetField(valueName, BindingFlags.Public | BindingFlags.Static);
 	        return field?.GetCustomAttribute<InspectorNameAttribute>()?.displayName;
 	    }
        
		public static void Button(
			string buttonText,
			Func<IEnumerable<Enum>> valuesCollector,
			Action<Enum> callback,
			bool showNow = false,
			params GUILayoutOption[] options)
		{
			Button(
				GetWindow<EnumPicker>,
				buttonText,
				valuesCollector,
				callback,
				showNow,
				GUI.skin.button,
				options
			);
		}

		public static void ToolbarButton(
			string buttonText,
			Func<IEnumerable<Enum>> valuesCollector,
			Action<Enum> callback,
			bool showNow = false,
			params GUILayoutOption[] options)
		{
			Button(
				GetWindow<EnumPicker>,
				buttonText,
				valuesCollector,
				callback,
				showNow,
				EditorStyles.toolbarDropDown,
				options
			);
		}

		public static void Button(
			Rect rect,
			string buttonText,
			Func<IEnumerable<Enum>> valuesCollector,
			Action<Enum> callback,
			bool showNow = false,
			GUIStyle style = null)
		{
			Button(
				GetWindow<EnumPicker>,
				rect,
				buttonText,
				valuesCollector,
				callback,
				showNow,
				style
			);
		}
	}
}
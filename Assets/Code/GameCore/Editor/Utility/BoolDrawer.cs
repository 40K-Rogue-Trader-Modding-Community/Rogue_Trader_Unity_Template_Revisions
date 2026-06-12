using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Utility.EditorPreferences;
using Owlcat.Editor.Utility;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.Utility
{
	[CustomPropertyDrawer(typeof(bool))]
	public class BoolDrawer : PropertyDrawer
	{
		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			var result = new Toggle();
			if (EditorPreferences.Instance.BigCheckbox)
				result.Q("unity-checkmark")?.AddToClassList("owlcat-toggle");
            
			result.BindProperty(property);
			var wrapped = result.WrapToOwlcatProperty(property);
			wrapped.HeaderContainer.style.backgroundRepeat = new StyleBackgroundRepeat
				(new BackgroundRepeat(Repeat.Repeat, Repeat.NoRepeat));

			wrapped.HeaderContainer.style.backgroundSize = new StyleBackgroundSize(
				new BackgroundSize(
					new Length(24, LengthUnit.Pixel), 
					new Length(24, LengthUnit.Pixel)));
			
			wrapped.HeaderContainer.AddToClassList("owlcat-background-dot");
			
			return wrapped;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return GetHeight();
		}

		public static float GetHeight()
		{
			return EditorPreferences.Instance.BigCheckbox ? 
				OwlcatEditorStyles.Instance.BigCheckboxHeight : EditorGUIUtility.singleLineHeight;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (!EditorPreferences.Instance.BigCheckbox
			    // Do not try to alter checkbox style for any of MonoBehaviour's enable property
			    || (property.serializedObject.targetObject is MonoBehaviour && property.propertyPath == "m_Enabled"))
			{
				EditorGUI.PropertyField(position, property, label);
			}
			else
			{
				bool b = EditorGUI.Toggle(position, label, property.boolValue, OwlcatEditorStyles.Instance.BigCheckbox);
				// ReSharper disable once RedundantCheckBeforeAssignment
				if (b != property.boolValue) // this is in case property has multiple values: we do not want to accidentally reset those
				{
					property.boolValue = b;
				}
			}
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Utility.Attributes;
using Owlcat.Editor.Core.Utility;
using RectEx;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.Utility
{
    [CustomPropertyDrawer(typeof(EnumFlagsAsButtonsAttribute))]
    public class EnumFlagsAsButtonsDrawer : PropertyDrawer
    {
        private const int Spacing = 3;
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
	        => new EnumFlagsAsButtonsProperty(property, fieldInfo.FieldType, (EnumFlagsAsButtonsAttribute)attribute);

    #region IMGUI
        
		private int GetRowsCount(SerializedProperty property)
		{
			var flagAttribute = (EnumFlagsAsButtonsAttribute)attribute;
			int valuesCount = Enum.GetNames(fieldInfo.FieldType).Length;
			return valuesCount / flagAttribute.ColumnCount + (valuesCount % flagAttribute.ColumnCount != 0 ? 1 : 0);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			int rowsCount = GetRowsCount(property);
			return rowsCount * EditorGUIUtility.singleLineHeight + Math.Max(0, rowsCount - 1) * Spacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.hasMultipleDifferentValues)
            {
                var r = CutFromExtensions.SliceFromLeft(ref position, EditorGUIUtility.labelWidth);
                EditorGUI.LabelField(r, label);
                r = CutFromExtensions.SliceFromLeft(ref position, 70);
                GUI.Label(r, new GUIContent("- multiple -"));
                r = CutFromExtensions.SliceFromLeft(ref position, 50);
                r.height = EditorGUIUtility.singleLineHeight;
                if (GUI.Button(r, "Reset"))
                {
                    property.intValue = property.intValue;
                }
                return;
            }

            var enumNames = Enum.GetNames(fieldInfo.FieldType);
            var enumValues = Enum.GetValues(fieldInfo.FieldType);

			var attr = (EnumFlagsAsButtonsAttribute)attribute;
			
            int enumLength = enumNames.Length;
			int rowsCount = GetRowsCount(property);
			bool[] buttonPressed = new bool[enumLength];

			var labelAndFieldPositions = position.CutFromLeft(EditorGUIUtility.labelWidth);
			EditorGUI.LabelField(labelAndFieldPositions[0], label);

			var cells = labelAndFieldPositions[1].Grid(rowsCount, attr.ColumnCount, Spacing);
			bool isNone = property.intValue == 0;
			for (int j = 0, index = 1; j < rowsCount; ++j)
			{
				for (int k = 0; k < attr.ColumnCount; ++k, ++index)
				{
					if (index >= enumValues.Length)
						index = 0;

					int enumValueInt = (int)enumValues.GetValue(index);

					// Check if the button is/was pressed
					buttonPressed[index] =
						isNone && enumValueInt == 0 ||
						!isNone && enumValueInt != 0 && (property.intValue & enumValueInt) == enumValueInt;
					
					EditorGUI.BeginChangeCheck();
					buttonPressed[index] = GUI.Toggle(cells[j, k], buttonPressed[index], enumNames[index], "Button");
					if (EditorGUI.EndChangeCheck())
					{
						if (buttonPressed[index])
						{
							property.intValue |= enumValueInt;
							isNone = enumValueInt == 0;
						}
						else
						{
							property.intValue &= ~enumValueInt;
						}
					}

					if (index == 0)
						break;
				}
			}

			if (isNone)
			{
				property.intValue = 0;
			}
		}
        
    #endregion
	    
	    private sealed class EnumFlagsAsButtonsProperty : OwlcatProperty
	    {
		    private readonly Type _enumType;
		    private readonly VisualElement _grid;
		    private readonly Dictionary<int, ToolbarToggle> _buttons = new();
		    
		    public EnumFlagsAsButtonsProperty(SerializedProperty property, Type enumType,
			    EnumFlagsAsButtonsAttribute attribute) : base(property)
		    {
			    _enumType = enumType;
			    _grid = new VisualElement();
			    _grid.AddToClassList("owlcat-enum-flag-grid");
			    ContentContainer.Add(_grid);
			    
			    string[] enumNames = Enum.GetNames(_enumType);
			    int[] enumValues = Enum.GetValues(_enumType).Cast<int>().ToArray();

			    // float width = 100f / attribute.ColumnCount;
			    for (int i = 0; i < enumNames.Length; i++)
			    {
				    string enumName = enumNames[i];
				    int enumValue = enumValues[i];

				    var button = CreateButton(enumName);//, width);
				    button.RegisterValueChangedCallback(_ => Toggle(enumValue));
				    _grid.Add(_buttons[enumValue] = button);
			    }

			    if (!_buttons.ContainsKey(0))
			    {
				    var button = CreateButton("-None-");//, width);
				    button.RegisterValueChangedCallback(_ => Toggle(0));
				    _grid.Insert(0, _buttons[0] = button);
			    }

			    if (!_buttons.ContainsKey(-1))
			    {
				    var button = CreateButton("-Everything-");//, width);
				    button.RegisterValueChangedCallback(_ => Toggle(-1));
				    _grid.Add(_buttons[-1] = button);
			    }

			    UpdateButtons();

			    return;

			    static ToolbarToggle CreateButton(string buttonName) //, float buttonWidth)
			    {
				    var button = new ToolbarToggle {text = buttonName};
				    button.AddToClassList("owlcat-enum-flag-toggle");
				    return button;
			    }
		    }

		    private void Toggle(int mask)
		    {
			    using var _ = GuiScopes.UpdateObject(Property.serializedObject);
			    Property.intValue  = mask switch
			    {
				    0 => 0,
				    -1 => -1,
				    _ => Property.intValue ^ mask
			    };
			    UpdateButtons();
		    }

		    private void UpdateButtons()
		    {
			    foreach (var (i, button) in _buttons)
			    {
				    bool on = i switch
				    {
					    0 or -1 => Property.intValue == i,
					    _ => (Property.intValue & i) == i
				    };
				    
				    button.SetValueWithoutNotify(on);
			    }
		    }
	    }

    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.ValuePicker;
using Kingmaker.EntitySystem.Properties;
using Kingmaker.EntitySystem.Stats.Base;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Enums;
using Kingmaker.UnitLogic.Levelup.Selections;
using Kingmaker.UnitLogic.Mechanics.Damage;
using Kingmaker.Utility.Attributes;
using Kingmaker.Utility.DotNetExtensions;
using Kingmaker.Visual.HitSystem;
using Owlcat.Editor.Core.Utility;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.Utility
{
	[CustomPropertyDrawer(typeof(EnumPickerAttribute))]
	[CustomPropertyDrawer(typeof(StatType))]
	[CustomPropertyDrawer(typeof(ModifierDescriptor))]
	[CustomPropertyDrawer(typeof(FeatureGroup))]
	[CustomPropertyDrawer(typeof(ArmorProficiencyGroup))]
	[CustomPropertyDrawer(typeof(UnitTag))]
	[CustomPropertyDrawer(typeof(HitLevel))]
	[CustomPropertyDrawer(typeof(BloodType))]
	[CustomPropertyDrawer(typeof(DamageType))]
	[CustomPropertyDrawer(typeof(WeaponCategory))]
	[CustomPropertyDrawer(typeof(UnitCondition))]
	[CustomPropertyDrawer(typeof(EntityProperty))]
	[CustomPropertyDrawer(typeof(MechanicsFeatureType))]
	[CustomPropertyDrawer(typeof(DG.Tweening.Ease))]
	public class EnumPickerDrawer : PropertyDrawer
	{
		public override VisualElement CreatePropertyGUI(SerializedProperty property)
			=> new EnumPickerProperty(property, fieldInfo);

	#region IMGUI
		
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			Rect fieldRect;
			if (!string.IsNullOrEmpty(label.text))
			{
				var labelRect = new Rect(
					position.x, 
					position.y, 
					EditorGUIUtility.labelWidth, 
					position.height
				);
				fieldRect = new Rect(
					position.x + EditorGUIUtility.labelWidth,
					position.y,
					position.width - EditorGUIUtility.labelWidth,
					position.height
				);
				EditorGUI.LabelField(labelRect, label);
			}
			else
			{
				fieldRect = position;
			}

			var type = fieldInfo.FieldType;
			if (type.IsArray)
			{
				type = type.GetElementType();
			}

			var orderAttribute = property
				.GetAttributes()
				.FirstOrDefault(attr => attr.GetType().IsSubclassOf(typeof(EnumOrderAttribute))) as EnumOrderAttribute;

			var displayOrder = orderAttribute?.Order ?? (type == typeof(StatType)
				? StatTypeHelper.DisplayOrder
				: EnumUtils.GetValues(type));

			string currentName;
		    if (property.hasMultipleDifferentValues)
		    {
		        currentName = "[Multiple]";
            }
            else 
            {
                currentName = Enum.GetName(type, property.intValue)??$"<invalid> ({property.intValue})"; //valuesArray[property.intValue].ToString();
            }

			var p = new RobustSerializedProperty(property);
			EnumPicker.Button(
				fieldRect,
				currentName,
				() => displayOrder,
				e =>
				{
					using (GuiScopes.UpdateObject(p.serializedObject))
					{
						p.Property.intValue = Convert.ToInt32(e);
					}
				},
				style: EditorStyles.popup
			);
		}
		
	#endregion
		
		private sealed class EnumPickerProperty : OwlcatProperty
		{
			private readonly Type _enumType;
			private readonly EnumField _enumField;
			private readonly Enum[] _displayOrder;
			
			public EnumPickerProperty(SerializedProperty property, FieldInfo fieldInfo) : base(property)
			{
				_enumField = new EnumField {style = {flexGrow = 1}};
				ContentContainer.Add(_enumField);
				_enumField.BindProperty(Property);
				_enumField.RegisterCallback<PointerDownEvent>(e =>
				{
					e.StopPropagation();
					ShowPickerMenu();
				}, TrickleDown.TrickleDown);
				_enumField.RegisterCallback<KeyDownEvent>(e =>
				{
					if (e.keyCode is KeyCode.Space or KeyCode.Return or KeyCode.KeypadEnter)
					{
						e.StopImmediatePropagation();
						ShowPickerMenu();
					}
				}, TrickleDown.TrickleDown);
				
				_enumType = fieldInfo.FieldType.IsArray ? fieldInfo.FieldType.GetElementType() : fieldInfo.FieldType;
				
				var orderAttribute = fieldInfo.GetCustomAttributes<EnumOrderAttribute>().FirstOrDefault();
				_displayOrder = orderAttribute?.Order ?? (_enumType == typeof(StatType) ? 
					StatTypeHelper.DisplayOrder : EnumUtils.GetValues(_enumType)).ToArray();
			}

			private void ShowPickerMenu()
			{
				EnumPicker.ShowPickerMenu(
					_enumField,
					EditorWindow.GetWindow<EnumPicker>,
					GetName(),
					() => _displayOrder,
					e =>
					{
						using var _ = GuiScopes.UpdateObject(Property.serializedObject);
						Property.intValue = Convert.ToInt32(e);
					});
			}

			private string GetName()
			{
				if (Property.hasMultipleDifferentValues)
					return "[Multiple]";

				string valueName = Enum.GetName(_enumType, Property.intValue);
                string inspectorName = EnumPicker.GetInspectorName(_enumType, valueName);
                
                if (!string.IsNullOrEmpty(inspectorName))
                    return inspectorName;
                
                if (!string.IsNullOrEmpty(valueName))
                    return valueName;
                
                return $"<invalid> ({Property.intValue})";
			}
		}
	}
}
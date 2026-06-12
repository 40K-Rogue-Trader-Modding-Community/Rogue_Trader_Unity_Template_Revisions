using System;
using JetBrains.Annotations;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.RuleSystem.Rules.Modifiers;
using Kingmaker.UnitLogic.Mechanics;
using Owlcat.Editor.Core.Utility;
using RectEx;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.Utility
{
	[CustomPropertyDrawer(typeof(ContextValue), true)]
	public class ContextValueDrawer : PropertyDrawer
	{
		public override VisualElement CreatePropertyGUI(SerializedProperty property)
			=> new ContextValueProperty(property);
		
	#region IMGUI
		
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			DrawContextValueProperty(position, property, label);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight;
		}

		public static void DrawContextValueProperty(Rect position, SerializedProperty property, GUIContent label)
		{
		    if (property.hasMultipleDifferentValues)
		    {
		        EditorGUI.LabelField(position, label, new GUIContent("- multiple -"));
		        return;
		    }
			var type = property.FindPropertyRelative(nameof(ContextValue.ValueType));
			var value = property.FindPropertyRelative(nameof(ContextValue.Value));
			var rank = property.FindPropertyRelative(nameof(ContextValue.ValueRank));
			var shared = property.FindPropertyRelative(nameof(ContextValue.ValueShared));
			var unitProperty = property.FindPropertyRelative(nameof(ContextValue.Property));
			var customUnitProperty = property.FindPropertyRelative("m_CustomProperty");
			var propertyName = property.FindPropertyRelative(nameof(ContextValue.PropertyName));

			bool enabled = true;
			if (property.FindPropertyRelative(nameof(ContextValueModifier.Enabled)) is {} enabledProperty)
			{
				var parts = position.CutFromRight(15);
				position = parts[0];

				using (GuiScopes.FixedWidth(1f, 0f))
				{
					enabled = enabledProperty.boolValue = EditorGUI.Toggle(parts[1], GUIContent.none, enabledProperty.boolValue);
				}
			}

			bool prevEnabled = GUI.enabled;
			try
			{
				GUI.enabled = enabled;
				
				if (property.FindPropertyRelative(nameof(ContextValueModifierWithType.ModifierType)) is {} typeProperty)
				{
					var parts = position.CutFromRight(75);
					position = parts[0];

					using (GuiScopes.FixedWidth(1f, 0f))
					{
						typeProperty.enumValueIndex = (int)(ModifierType)EditorGUI.EnumPopup(
							parts[1], (ModifierType)typeProperty.enumValueIndex);
					}
				}

				Rect[] chunkPositions;
				if (label != GUIContent.none)
				{
					var labelAndFieldPositions = position.CutFromLeft(EditorGUIUtility.labelWidth);
					EditorGUI.LabelField(labelAndFieldPositions[0], label);
					chunkPositions = labelAndFieldPositions[1].Row(new[] {2f, 1f});
				}
				else
				{
					chunkPositions = position.Row(new[] {2f, 1f});
				}

				SerializedProperty p;
				switch ((ContextValueType)type.intValue)
				{
					case ContextValueType.Simple:
					case ContextValueType.CasterBuffRank:
					case ContextValueType.TargetBuffRank:
						p = value;
						break;
					case ContextValueType.Rank:
						p = rank;
						break;
					case ContextValueType.Shared:
						p = shared;
						break;
					case ContextValueType.CasterProperty:
					case ContextValueType.TargetProperty:
						p = unitProperty;
						break;
					case ContextValueType.CasterCustomProperty:
					case ContextValueType.TargetCustomProperty:
						p = customUnitProperty;
						break;
					case ContextValueType.CasterNamedProperty:
					case ContextValueType.TargetNamedProperty:
					case ContextValueType.ContextProperty:
						p = propertyName;
						break;
					default:
						p = null;
						break;
				}

				using (GuiScopes.FixedWidth(1f, 0f))
				{
					if (p != null)
					{
						EditorGUI.PropertyField(chunkPositions[0], p, GUIContent.none);
					}
					else
					{
						EditorGUI.LabelField(chunkPositions[0], "unsupported type");
					}

					EditorGUI.PropertyField(chunkPositions[1], type, GUIContent.none);
				}
			}
			finally
			{
				GUI.enabled = prevEnabled;
			}
		}
		
	#endregion

		private sealed class ContextValueProperty : OwlcatProperty
		{
			private readonly IntegerField _constValue;
			private readonly EnumField _valueType;
			private readonly EnumField _rankType;
			private readonly EnumField _sharedType;
			private readonly EnumField _propertyType;
			private readonly PropertyField _propertyRef;
			private readonly EnumField _propertyName;
			
			[CanBeNull]
			private readonly Toggle _enable;
			[CanBeNull]
			private readonly EnumField _modifierType;
			
			private SerializedProperty ValueTypeSerializedProperty
				=> Property.FindPropertyRelative(nameof(ContextValue.ValueType));
			
			private SerializedProperty ConstValueSerializedProperty
				=> Property.FindPropertyRelative(nameof(ContextValue.Value));
			
			private SerializedProperty RankValueSerializedProperty
				=> Property.FindPropertyRelative(nameof(ContextValue.ValueRank));
			
			private SerializedProperty SharedValueSerializedProperty
				=> Property.FindPropertyRelative(nameof(ContextValue.ValueShared));
			
			private SerializedProperty PropertyTypeSerializedProperty
				=> Property.FindPropertyRelative(nameof(ContextValue.Property));
			
			private SerializedProperty PropertyRefSerializedProperty
				=> Property.FindPropertyRelative("m_CustomProperty");
			
			private SerializedProperty PropertyNameSerializedProperty
				=> Property.FindPropertyRelative(nameof(ContextValue.PropertyName));
			
			[CanBeNull]
			private SerializedProperty EnabledSerializedProperty
				=> Property.FindPropertyRelative(nameof(ContextValueModifier.Enabled));
			
			[CanBeNull]
			private SerializedProperty ModifierTypeSerializedProperty
				=> Property.FindPropertyRelative(nameof(ContextValueModifierWithType.ModifierType));

			public ContextValueProperty(SerializedProperty property) : base(property)
			{
				_constValue = new IntegerField {style = { flexGrow = 1 }};
				_constValue.BindProperty(ConstValueSerializedProperty);
				
				_valueType = new EnumField {style = { flexGrow = 1 }};
				_valueType.BindProperty(ValueTypeSerializedProperty);
				_valueType.RegisterValueChangedCallback(_ => UpdateVisibility());
				
				_rankType = new EnumField { style = { flexGrow = 1 } };
				_rankType.BindProperty(RankValueSerializedProperty);
				
				_sharedType = new EnumField { style = { flexGrow = 1 } };
				_sharedType.BindProperty(RankValueSerializedProperty);
				
				_propertyType = new EnumField { style = { flexGrow = 1 } };
				_propertyType.BindProperty(PropertyTypeSerializedProperty);
				
				_propertyRef = new PropertyField(PropertyRefSerializedProperty) { style = { flexGrow = 1 } };
				_propertyRef.BindProperty(PropertyRefSerializedProperty);
				_propertyRef.AddToClassList("owlcat-inline-property");
				
				_propertyName = new EnumField { style = { flexGrow = 1 }};
				_propertyName.BindProperty(PropertyNameSerializedProperty);

				if (EnabledSerializedProperty is {} enabledProperty)
				{
					_enable = new Toggle();
					_enable.BindProperty(enabledProperty);
					_enable.RegisterValueChangedCallback(_ => UpdateEnabled());
				}

				if (ModifierTypeSerializedProperty is {} modifierTypeProperty)
				{
					_modifierType = new EnumField { style = { flexGrow = 1 } };
					_modifierType.BindProperty(modifierTypeProperty);
				}
			}
			
			protected override void CreateContentInternal()
			{
				base.CreateContentInternal();
				
				ContentContainer.Add(_constValue);
				ContentContainer.Add(_propertyRef);
				ContentContainer.Add(_rankType);
				ContentContainer.Add(_sharedType);
				ContentContainer.Add(_propertyName);
				ContentContainer.Add(_propertyType);
				ContentContainer.Add(_valueType);
				
				if (_modifierType != null)
					ContentContainer.Add(_modifierType);
				
				if (_enable != null)
					ContentContainer.Add(_enable);
				
				UpdateVisibility();
				UpdateEnabled();
			}

			private void UpdateVisibility()
			{
				var type = (ContextValueType) ValueTypeSerializedProperty.intValue;
				switch (type)
				{
					case ContextValueType.Simple:
					case ContextValueType.CasterBuffRank:
					case ContextValueType.TargetBuffRank:
						SetVisible(_constValue, true);
						SetVisible(_propertyRef, false);
						SetVisible(_propertyName, false);
						SetVisible(_propertyType, false);
						SetVisible(_rankType, false);
						SetVisible(_sharedType, false);
						break;
					case ContextValueType.Rank:
						SetVisible(_rankType, true);
						SetVisible(_constValue, false);
						SetVisible(_propertyRef, false);
						SetVisible(_propertyName, false);
						SetVisible(_propertyType, false);
						SetVisible(_sharedType, false);
						break;
					case ContextValueType.Shared:
						SetVisible(_sharedType, true);
						SetVisible(_constValue, false);
						SetVisible(_propertyRef, false);
						SetVisible(_propertyName, false);
						SetVisible(_propertyType, false);
						SetVisible(_rankType, false);
						break;
					case ContextValueType.CasterProperty:
					case ContextValueType.TargetProperty:
						SetVisible(_constValue, false);
						SetVisible(_propertyRef, false);
						SetVisible(_propertyName, false);
						SetVisible(_propertyType, true);
						SetVisible(_rankType, false);
						SetVisible(_sharedType, false);
						break;
					case ContextValueType.CasterCustomProperty:
					case ContextValueType.TargetCustomProperty:
						SetVisible(_constValue, false);
						SetVisible(_propertyRef, true);
						SetVisible(_propertyName, false);
						SetVisible(_propertyType, false);
						SetVisible(_rankType, false);
						SetVisible(_sharedType, false);
						break;
					case ContextValueType.CasterNamedProperty:
					case ContextValueType.TargetNamedProperty:
					case ContextValueType.ContextProperty:
						SetVisible(_constValue, false);
						SetVisible(_propertyRef, false);
						SetVisible(_propertyName, true);
						SetVisible(_propertyType, false);
						SetVisible(_rankType, false);
						SetVisible(_sharedType, false);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			private void UpdateEnabled()
			{
				bool enabled = _enable?.value != false;
				
				_constValue.SetEnabled(enabled);
				_propertyRef.SetEnabled(enabled);
				_propertyName.SetEnabled(enabled);
				_propertyType.SetEnabled(enabled);
				_valueType.SetEnabled(enabled);
				_rankType.SetEnabled(enabled);
				_sharedType.SetEnabled(enabled);

				_modifierType?.SetEnabled(enabled);
			}

			private static void SetVisible(VisualElement element, bool visible)
				=> element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
		}
	}

	[CustomPropertyDrawer(typeof(ContextDiceValue), true)]
	public class ContextDiceValueDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var diceCount = property.FindPropertyRelative(nameof(ContextDiceValue.DiceCountValue));
			var diceType = property.FindPropertyRelative(nameof(ContextDiceValue.DiceType));
			var bonus = property.FindPropertyRelative(nameof(ContextDiceValue.BonusValue));
			
			var labelAndFieldPositions = position.CutFromLeft(EditorGUIUtility.labelWidth);
			EditorGUI.LabelField(labelAndFieldPositions[0], label);

			var chunkPositions = labelAndFieldPositions[1].Row(new[] {2.5f, 1f, 2.5f });
			
			ContextValueDrawer.DrawContextValueProperty(chunkPositions[0], diceCount, GUIContent.none);
			using (GuiScopes.FixedWidth(13f, 0f))
			{
				EditorGUI.PropertyField(chunkPositions[1], diceType, new GUIContent("d"));
				ContextValueDrawer.DrawContextValueProperty(chunkPositions[2], bonus, new GUIContent("+"));
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight;
		}
	}
}
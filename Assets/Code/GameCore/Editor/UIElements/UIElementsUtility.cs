using JetBrains.Annotations;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.Editor.UIElements.Custom;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Base;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.Elements;
using Kingmaker.Editor.UIElements.Custom.Array;
using Kingmaker.Editor.Utility;
using Kingmaker.ElementsSystem;
using Kingmaker.Utility.Attributes;
using Kingmaker.Utility.EditorPreferences;
using Kingmaker.Utility.FlagCountable;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using Owlcat.Runtime.Core.Utility;
using Object = UnityEngine.Object;

namespace Kingmaker.Editor.UIElements
{
	public static class UIElementsUtility
	{
		public class InitializationProcessFlag : IDisposable
		{
			public static CountableFlag Flag { get; } = new CountableFlag();

			public static InitializationProcessFlag Require()
			{
				Flag.Retain();
				return new InitializationProcessFlag();
			}

			void IDisposable.Dispose()
			{
				Flag.Release();
			}
		}

		public static readonly VisualElement UseDefaultBehavior = new VisualElement {name = "use-default-behavior"};
		private static HashSet<string> m_ExpandedStateSet = new();
		
		public static OwlcatInspectorRoot CreateInspector(SerializedObject serializedObject, 
			[CanBeNull] Action<OwlcatInspectorRoot> initializer = null, 
			bool isHideScriptField = false, bool force = false)
		{
			using (InitializationProcessFlag.Require())
			{
				OwlcatInspectorRoot inspector;
				if (serializedObject.targetObject is BlueprintEditorWrapper)
					inspector = new BlueprintInspectorRoot(serializedObject, null);
				else
					inspector = new OwlcatInspectorRoot(serializedObject, isHideScriptField);

				initializer?.Invoke(inspector);

				return inspector;
			}
		}

		public static BlueprintInspectorRoot CreateBlueprintInspector(SimpleBlueprint bp, 
			BlueprintWrapperInspector blueprintInspector, 
			[CanBeNull] Action<OwlcatInspectorRoot> initializer = null, 
			bool force = false)
		{
			using (InitializationProcessFlag.Require())
			{
				var w = BlueprintEditorWrapper.Wrap(bp);
				var so = new SerializedObject(w);
				var inspector = new BlueprintInspectorRoot(so, blueprintInspector);
				initializer?.Invoke(inspector);
				
				return inspector;
			}
		}
		
		public static OwlcatProperty CreatePropertyElement(SerializedProperty property, bool isArrayElement)
		{
			Profiler.BeginSample($"Create UIElement");
			var info = property.GetFieldInfo();
			if (info != null &&
			    info.FieldType.IsEnum &&
			    property.enumValueIndex == -1)
			{
				FixEnum(info, property);
			}
			
			VisualElement visualElement;
            if (property.isArray && property.propertyType != SerializedPropertyType.String)
            {
                var elementType = PropertyToFieldMatcher.GetMatcher(property.serializedObject.targetObject)
                    .GetMatchingField(property).GetElementType();
                
                if (property.serializedObject.targetObject is not ScriptableWrapperBase ||
                    property.propertyType == SerializedPropertyType.ManagedReference ||
                    typeof(Element).IsAssignableFrom(elementType))
                {
                    var types = elementType == typeof(Object) ? null : TypeUtility.CollectValues(property, elementType).ToArray();
                    visualElement = new OwlcatListViewProperty(property, property, types);
                }
                else
                {
                    visualElement = new OwlcatListViewProperty(property, property);
                }
            }
            else
            {
                visualElement = TryCreateCustomVisualElement(property, info);
                if (visualElement == null || visualElement == UseDefaultBehavior)
                {
                    if ((property.propertyType == SerializedPropertyType.Generic && property.hasChildren ||
                         property.propertyType == SerializedPropertyType.ManagedReference))
                    {
                        visualElement = OwlcatProperty.CreateGeneric(property);
                    }
                    else if (property.propertyType == SerializedPropertyType.ObjectReference && info != null)
                    {
                        visualElement = new OwlcatObjectProperty(property);
                    }
                    else
                    {
	                    visualElement = OwlcatProperty.CreateDefault(property);
                        if (property.propertyPath == "m_Script" && property.serializedObject.targetObject != null)
                        {
                            visualElement.SetEnabled(false);
                        }
                    }
                }
            }
			
			Profiler.EndSample();

			var result = visualElement.WrapToOwlcatProperty(property);
			if (info != null && !isArrayElement)
			{
				result.Attributes = info.GetCustomAttributes().ToArray();
			}

			return result;
		}

		private static VisualElement TryCreateCustomVisualElement(SerializedProperty property, FieldInfo fieldInfo)
		{
			if (fieldInfo == null)
			{
				return null;
			}

			var attributes = fieldInfo.GetCustomAttributes().ToList();
			foreach (var att in attributes)
			{
				switch (att)
				{
					case TextAreaAttribute _:
					case MultilineAttribute _:
						return new OwlcatTextAreaProperty(property, attributes);
				}
			}

			// check for custom drawer attribute
			var attrs = fieldInfo.GetCustomAttributes<PropertyAttribute>();
			foreach (var attr in attrs)
			{
				if (attr is ITitleAttribute)
					continue;
				
				if (attr is InspectorReadOnlyAttribute or InspectorDisableAttribute)
					continue;

				var ve = UICustomDrawerUtility.TryGetCustomVisualElement(attr.GetType(), attr, fieldInfo, property);
				if (ve != null)
					return ve;
			}

			// check for custom drawer on type
			var type = fieldInfo.FieldType;
			if (!property.isArray)
			{
				var fieldType = fieldInfo.FieldType;
				if (fieldType.IsArray)
				{
					type = fieldType.GetElementType();
				}
				else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
				{
					type = fieldType.GetGenericArguments()[0];
				}
				else
				{
					type = fieldInfo.FieldType;
				}
			}

			return UICustomDrawerUtility.TryGetCustomVisualElement(type, null, fieldInfo, property);
		}

		private static void FixEnum(FieldInfo info, SerializedProperty prop)
		{
			if (info.FieldType.GetAttribute<FlagsAttribute>() != null)
			{
				if (!IsFlagsValid(info.FieldType, prop.intValue))
				{
					ResetEnumValue(info, prop);
				}
			}
			else if (!IsEnumValueDefined(info.FieldType, prop.intValue))
			{
				ResetEnumValue(info, prop);
			}
		}

		private static void ResetEnumValue(FieldInfo info, SerializedProperty prop)
		{
			var values = Enum.GetValues(info.FieldType);
			if (values.Length > 0)
			{
				prop.enumValueIndex = 0;
				prop.intValue = Convert.ToInt32(values.GetValue(0));
				prop.serializedObject.ApplyModifiedProperties();
			}
		}

		private static bool IsFlagsValid(Type enumType, int value)
		{
			bool valid = true;
			int maxBit = Convert.ToInt32(Math.Pow(2, Math.Ceiling(Math.Log(value) / Math.Log(2)))) >> 2;
			int i = 1;
			do
			{
				int ordinalValue = (1 << i);
				if (0 != (value & ordinalValue))
				{
					valid = (Enum.IsDefined(enumType, ordinalValue));
					if (!valid)
					{
						break;
					}
				}

				i++;
			} while (maxBit > i);

			return valid;
		}

		private static bool IsEnumValueDefined(Type enumType, int value)
		{
			object valueObject = null;
			var innerType = Enum.GetUnderlyingType(enumType);
			if (innerType == typeof(byte))
			{
				valueObject = (byte)value;
			}

			valueObject = valueObject ?? value;
			return Enum.IsDefined(enumType, valueObject);
		}

		public static void SetExpandedState(string path, bool isExpanded)
		{
			if (isExpanded)
				m_ExpandedStateSet.Add(path);
			else
				m_ExpandedStateSet.Remove(path);
		}

		public static bool GetExpandedState(string path)
		{
			return m_ExpandedStateSet.Contains(path);
		}

		public static bool TryGetVisualElement<T>(this VisualElement root, out T result) where T : VisualElement
		{
			foreach (var child in root.Children())
			{
				if (child is T visualElement)
				{
					result = visualElement;
					return true;
				}
			}

			foreach (var child in root.Children())
			{
				if (child.TryGetVisualElement(out result))
					return true;
			}

			result = default;
			return false;
		}
	}

	public class ErrorElement : VisualElement
	{
		private readonly string m_ErrorText;

		public ErrorElement(string label, string errorText)
		{
			style.flexDirection = FlexDirection.Row;
			Add(new Label(label));
			var btn = new Button();
			btn.text = "?ERROR?";
			btn.style.backgroundColor = Color.red;
			btn.style.unityTextAlign = TextAnchor.MiddleCenter;
			btn.style.width = 70;
			btn.clicked += ShowMenu;
			m_ErrorText = errorText;
			Add(btn);
			PFLog.Default.Error($"Fail create inspector field. [{label}] {errorText}");
		}

		private void ShowMenu()
		{
			EditorGUIUtility.systemCopyBuffer = m_ErrorText;
			Debug.LogError(m_ErrorText);
		}
	}
}
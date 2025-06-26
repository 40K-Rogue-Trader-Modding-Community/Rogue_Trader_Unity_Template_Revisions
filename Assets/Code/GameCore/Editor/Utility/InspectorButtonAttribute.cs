using UnityEngine;
using UnityEditor;
using System.Reflection;
using Kingmaker.Code.Editor.Utility;

#nullable enable

namespace Kingmaker.Editor.Utility
{
	/// <summary>
	/// A helper for simple button creation inside property drawers
	/// Just mimics affected serialized field with a button with
	/// a call back to a class instance this field belongs
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Field)]
	public class InspectorButtonAttribute : PropertyAttribute
	{
		public readonly string ButtonMethodName;
		public readonly string? ButtonLabel;

		public InspectorButtonAttribute(string buttonMethodName, string? buttonLabel = null)
		{
			ButtonMethodName = buttonMethodName;
			ButtonLabel = buttonLabel;
		}
	}

	[CustomPropertyDrawer(typeof(InspectorButtonAttribute))]
	public class InspectorButtonPropertyDrawer : PropertyDrawer
	{
		private MethodInfo? m_EventMethodInfo;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			InspectorButtonAttribute inspectorButtonAttribute = (InspectorButtonAttribute)attribute;
			string buttonLabel = inspectorButtonAttribute.ButtonLabel
			                     ?? (property.propertyType == SerializedPropertyType.String
				                     ? property.stringValue
				                     : label.text);

			if (GUI.Button(position, buttonLabel))
			{
				// Get an object this field is from
				var targetProperty = property.GetParent();
				object targetObject = targetProperty == null
					? property.serializedObject.targetObject
					: targetProperty.GetTargetObjectOfProperty();
				if (targetObject == null)
				{
					return;
				}

				System.Type targetType = targetObject.GetType();
				if (m_EventMethodInfo == null)
				{
					m_EventMethodInfo = targetType.GetMethod(inspectorButtonAttribute.ButtonMethodName,
						BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				}

				if (m_EventMethodInfo == null)
				{
					PFLog.Default.Warning("InspectorButton: " +
					                      $"Unable to find method {inspectorButtonAttribute.ButtonMethodName} " +
					                      $"in {targetType}");
				}
				else
				{
					m_EventMethodInfo.Invoke(targetObject, parameters:new object[]{});
				}
			}
		}
	}
}

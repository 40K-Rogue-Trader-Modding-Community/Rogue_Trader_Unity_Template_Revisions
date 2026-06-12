using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.Utility.Attributes;
using Owlcat.Runtime.Core.Utility;
using Owlcat.Utility.Attributes;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor
{
    public static class AttributeExtensions
    {
        public static bool IsVisible(SerializedProperty property)
        {
            var field = FieldFromProperty.GetFieldInfo(property);
            var attr = field?.GetAttribute<ConditionalAttribute>();
            bool visibilityCondition = attr?.CalculateCondition(property) ?? true;
            
            return attr == null || 
                   visibilityCondition && attr is ShowIfAttribute || 
                   !visibilityCondition && attr is HideIfAttribute;
        }

        public static bool IsFieldVisible(this ConditionalAttribute attribute, SerializedProperty property)
        {
            bool condition = attribute.CalculateCondition(property);
            return attribute.ValueForVisible ? condition : !condition;
        }

        public static bool CalculateCondition(this ConditionalAttribute attribute, SerializedProperty property)
        {
            bool result = false;
            
            try
            {
                var parent = property.GetParent();
                object v = parent == null ? 
                    property.serializedObject.targetObject : FieldFromProperty.GetFieldValue(parent);
                var field = FieldFromProperty.GetFieldMemberInfo(v.GetType(), attribute?.ConditionSource);
                
                result = (bool) FieldFromProperty.GetValue(field, v);
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("ConditionalAttribute: can't extract value {1} from '{0}' (look at exception below)",
                    property.propertyPath, attribute.ConditionSource);
                Debug.LogException(e);
            }
            
            return result;
        }
        
        public static List<Enum> GetFilteredList(this EnumFilterAttribute filterAttribute, SerializedProperty property)
        {
            var result = new List<Enum>();
            
            try
            {
                var parent = property.GetParent();
                object v = parent == null ? 
                    property.serializedObject.targetObject : FieldFromProperty.GetFieldValue(parent);
                var field = FieldFromProperty.GetFieldMemberInfo(v.GetType(), filterAttribute?.EnumValuesSource);
                object listObject = FieldFromProperty.GetValue(field, v);
                
                result = ((IEnumerable) listObject).Cast<Enum>().ToList();
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("EnumFilterAttribute: can't extract value {1} from '{0}' (look at exception below)",
                    property.propertyPath, filterAttribute?.EnumValuesSource);
                Debug.LogException(e);
            }
            
            return result;
        }
    }
}
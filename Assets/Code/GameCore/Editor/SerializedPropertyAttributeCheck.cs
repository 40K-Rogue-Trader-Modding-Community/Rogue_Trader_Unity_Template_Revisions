using System;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Utility.Attributes;
using Owlcat.QA.Validation;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;

namespace Code.Editor.Utility
{
    public static class SerializedPropertyAttributeCheck
    {
        public static bool HasNotNullAttribute(this SerializedProperty property)
        {
            var fi = FieldFromProperty.GetFieldInfo(property);
            if (fi == null)
                return false;

            return fi.HasAttribute<NotNullAttribute>() ||
                   fi.HasAttribute<ValidateNotNullAttribute>();
        }

        public static bool HasObsoleteAttribute(this SerializedProperty property, out string reason)
        {
            var fi = FieldFromProperty.GetFieldInfo(property);
            var actualType = FieldFromProperty.GetActualValueType(property);
            var fieldType = fi?.FieldType;

            if (fieldType is {IsGenericType: true} && typeof(BlueprintReferenceBase).IsAssignableFrom(fieldType))
                fieldType = fieldType.GetGenericArguments()[0];

            var fiObsoleteAttribute = fi?.GetAttribute<ObsoleteAttribute>();
 	        var actualTypeObsoleteAttribute = actualType?.GetAttribute<ObsoleteAttribute>();
 	        var fieldTypeObsoleteAttribute = fieldType?.GetAttribute<ObsoleteAttribute>();
 	
 	        reason = string.Empty;
 	        if (fiObsoleteAttribute != null)
 	            reason = fiObsoleteAttribute.Message;
 	        else if (actualTypeObsoleteAttribute != null)
 	            reason = actualTypeObsoleteAttribute.Message;
 	        else if (fieldTypeObsoleteAttribute != null)
 	            reason = fieldTypeObsoleteAttribute.Message;
 	
 	        return fiObsoleteAttribute != null || 
                   actualTypeObsoleteAttribute != null || 
                   fieldTypeObsoleteAttribute != null;
        }

        public static bool HasReadOnlyAttribute(this SerializedProperty property)
        {
            var fi = FieldFromProperty.GetFieldInfo(property);
            if (fi == null)
                return false;

            return fi.HasAttribute<InspectorReadOnlyAttribute>() || fi.HasAttribute<InspectorDisableAttribute>();
        }

        public static bool TryGetExpandAttribute(this SerializedProperty property, out ExpandInspectorAttribute attribute)
        {
            attribute = null;
            var fi = FieldFromProperty.GetFieldInfo(property);
            if (fi == null)
                return false;

            var classAttribute = fi.DeclaringType.GetAttribute<ExpandInspectorAttribute>();
            var fieldAttribute = fi.GetAttribute<ExpandInspectorAttribute>();
            attribute = fieldAttribute ?? classAttribute;
            return attribute != null;
        }
    }
}
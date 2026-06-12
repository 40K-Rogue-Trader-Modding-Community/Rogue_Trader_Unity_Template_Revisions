using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Editor.Blueprints;
using UnityEditor;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.PropertyComponents
{
    public class ListDragAndDropComponent : DragAndDropComponent
    {
        private readonly SerializedProperty m_Property;
        private readonly Type m_ElementType;
        private readonly Type m_BlueprintType;
        
        public ListDragAndDropComponent(VisualElement target, SerializedProperty arrayProperty) : 
            base(target, null, null)
        {
            m_Property = arrayProperty.Copy();
            var arrayType = FieldFromProperty.GetFieldInfo(arrayProperty)?.FieldType;
            
            if (arrayType is { IsGenericType: true } && arrayType.GetGenericTypeDefinition() == typeof(List<>))
                m_ElementType = arrayType.GetGenericArguments()[0];
            else if (arrayType is { IsArray: true })
                m_ElementType = arrayType.GetElementType();
            
            if (m_ElementType == null)
            {
                IsValidateFunc = () => false;
                return;
            }

            m_BlueprintType = GetGenericArgumentFromBase(m_ElementType, typeof(BlueprintReference<>));
            
            IsValidateFunc = IsValid;
            DropFunc = Drop;
        }

        private bool IsValid()
        {
            if (DragAndDrop.objectReferences.Length == 0)
                return false;

            if (m_BlueprintType != null)
            {
                return DragAndDrop.objectReferences.Any(x =>
                    m_BlueprintType.IsInstanceOfType((x as BlueprintEditorWrapper)?.Blueprint));
            }

            return DragAndDrop.objectReferences.Any(x => m_ElementType.IsInstanceOfType(x));
        }

        private void Drop()
        {
            bool changed = false;
            
            if (m_BlueprintType != null)
            {
                var validList = DragAndDrop.objectReferences.Where(x => 
                    m_BlueprintType.IsInstanceOfType((x as BlueprintEditorWrapper)?.Blueprint)).ToList();
                
                for (int i = 0; i < validList.Count; i++)
                {
                    changed = true;
                    
                    PrototypedObjectEditorUtility.AddArrayElement(m_Property, m_Property.arraySize, m_BlueprintType);
                    
                    var property = m_Property.GetArrayElementAtIndex(m_Property.arraySize - 1);
                    var guidProp = property.FindPropertyRelative("guid");
                    if (guidProp != null)
                    {
                        guidProp.stringValue = (validList[i] as BlueprintEditorWrapper)?.Blueprint.AssetGuid;
                        m_Property.serializedObject.ApplyModifiedProperties();
                    }
                }
            }
            else
            {
                var validList = DragAndDrop.objectReferences.Where(x => m_ElementType.IsInstanceOfType(x)).ToList();
                for (int i = 0; i < validList.Count; i++)
                {
                    changed = true;
                    m_Property.InsertArrayElementAtIndex(m_Property.arraySize);
                    m_Property.GetArrayElementAtIndex(m_Property.arraySize - 1).objectReferenceValue = validList[i];
                }
            }
            
            if (changed)
                m_Property.serializedObject.ApplyModifiedProperties();
        }
        
        private static Type GetGenericArgumentFromBase(Type type, Type genericBase)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == genericBase)
                    return type.GetGenericArguments()[0];

                type = type.BaseType;
            }

            return null;
        }
    }
}
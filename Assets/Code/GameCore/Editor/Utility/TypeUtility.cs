using Kingmaker.Editor.Elements.SmartElementPopulation;
using Kingmaker.Editor.Utility;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Persistence.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Attributes;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.UIElements.ValuePicker;
using UnityEngine;
using Owlcat.Runtime.Core.Utility;
using Owlcat.Editor.Core.Utility;
using Kingmaker.Utility.DotNetExtensions;
using Owlcat.Editor.Elements;
using UnityEditor;

namespace Kingmaker.Editor.Utility
{
	public static class TypeUtility 
	{
		public static IEnumerable<Type> CollectValues(RobustSerializedProperty rsp, Type elementType)
		{
			FieldInfo fieldInfo = FieldFromProperty.GetFieldInfo(rsp);
			object[] fieldAttrs = fieldInfo.GetCustomAttributes(true);
			
			if (fieldAttrs != null)
			{
				HashSet<Type> hashSet = fieldAttrs
					.OfType<ValidFieldTypeAttribute>()
					.Select(x => x.Type)
					.ToHashSet();
				
				if (hashSet is { Count: > 0 })
				{
					HashSet<Type> validTypesHash = new();
					foreach (Type bt in hashSet)
					{
						IEnumerable<Type> validTypes = GetValidTypes(rsp, bt);
						validTypesHash.AddRange(validTypes);
					}
					
					return validTypesHash.Where(t => !t.HasAttribute<HideInPickerAttribute>()).ToArray();
				}
			}

			return GetValidTypes(rsp, elementType, orderByShortName: true)
				.Where(t => !t.HasAttribute<HideInPickerAttribute>());
		}

		public static ValuesContainer<Type> CollectValuesWithFilter(RobustSerializedProperty rsp, Type elementType)
		{
			List<Type> rawTypes = CollectValues(rsp, elementType).ToList();
			if (elementType.IsOrSubclassOf<Evaluator>())
			{
				List<Type> filteredTypes = new ContextEvaluatorFilter().Process(rsp, rawTypes);
				return new ValuesContainer<Type>(rawTypes, filteredTypes);
			}
			else
			{
				return new ValuesContainer<Type>(rawTypes);
			}
		}
		
		public static IEnumerable<Type> GetValidTypes(RobustSerializedProperty rsp, Type elementType, bool orderByShortName = false)
		{
            var mainAsset = rsp.targetObject as ScriptableObject;
            var blueprintComponent = (mainAsset as BlueprintComponentEditorWrapper)?.Component;
            var blueprint = blueprintComponent?.OwnerBlueprint;
            var blueprintType = blueprint?.GetType();

            var mainAssetType = mainAsset?.GetWrappedType();

            if (mainAssetType != null && mainAssetType.HasAttribute<PlayerUpgraderFilterAttribute>())
            {
	            return GetDerivedTypesRecursively(elementType)
		            .Where(et => et.HasAttribute<PlayerUpgraderAllowedAttribute>() || et.IsOrSubclassOf<PlayerUpgraderOnlyAction>())
		            .Where(i => !i.HasAttribute<ObsoleteAttribute>())
		            .OrderBy(t => t.Name);
            }
            
            if (mainAssetType != null && mainAssetType.HasAttribute<UnitUpgraderFilterAttribute>())
            {
	            return GetDerivedTypesRecursively(elementType)
		            .Where(et => et.IsOrSubclassOf<UnitUpgraderOnlyAction>())
		            .Where(i => !i.HasAttribute<ObsoleteAttribute>())
		            .OrderBy(t => t.Name);
            }

            var result = TypeCache.GetTypesDerivedFrom(elementType).Where(t => !t.IsAbstract);

            if (!elementType.IsAbstract)
	            result = result.Concat(elementType);

            if (blueprintType != null)
            {
	            result = result.Where(et =>
	            {
		            object[] attrs = et.GetCustomAttributes(typeof(AllowedOnAttribute), true);
		            return !(attrs.Length > 0) && attrs.Cast<AllowedOnAttribute>()
			            .All(attr => !(blueprintType.IsSubclassOf(attr.Type) || (blueprintType == attr.Type)));
	            });
            }
            
            if (elementType.IsOrSubclassOf<GameAction>())
            {
	            result = result.Where(t => !t.IsOrSubclassOf<PlayerUpgraderOnlyAction>() && 
												!t.IsOrSubclassOf<UnitUpgraderOnlyAction>());
            }

            result = result.Where(t => !t.HasAttribute<ObsoleteAttribute>());

            return result.OrderBy(t => orderByShortName ? ClassNames.GetClassNameNoPrefix(t) : t.Name);
		}

		public static void AddElementFromMenu(RobustSerializedProperty property, Type type, int atIndex = -1)
		{
            // hmm, why is this method in TypeUtility exactly?
            var owner = BlueprintEditorWrapper.Unwrap<SimpleBlueprint>(property.serializedObject.targetObject);
            if (owner == null)
            {
                owner = (property.serializedObject.targetObject as BlueprintComponentEditorWrapper)?.Component
                    ?.OwnerBlueprint;
            }
            if (owner == null)
            {
                PFLog.Default.Error($"Cannot add element: {property.serializedObject.targetObject} is not an element owner");
                return;
            }
            
            var element = (Element)Activator.CreateInstance(type);
            owner.AddNewElement(element, atIndex);
            ElementWorkspaceContextualPopulationController.PrefillWithTargets(element, element.Owner);
            UpdateProperty(property, element, atIndex);
        }

        private static void UpdateProperty(RobustSerializedProperty property, Element element, int atIndex)
        {
            using (GuiScopes.UpdateObject(property.serializedObject))
            {
                if (property.Property.isArray)
                {
	                atIndex = atIndex < 0 || atIndex > property.Property.arraySize
		                ? property.Property.arraySize
		                : atIndex;

                    property.Property.InsertArrayElementAtIndex(atIndex);
                    property.serializedObject.ApplyModifiedProperties();
                    FieldFromProperty.SetFieldValue(property.Property.GetArrayElementAtIndex(atIndex), element);
                    property.serializedObject.Update();
                }
                else
                {
                    property.serializedObject.ApplyModifiedProperties();
                    FieldFromProperty.SetFieldValue(property.Property, element);
                    property.serializedObject.Update();
                }
            }
        }

        private static IEnumerable<Type> GetDerivedTypesRecursively(Type elementType)
        {
	        var result = new HashSet<Type>();
	        foreach (var type in TypeCache.GetTypesDerivedFrom(elementType))
	        {
		        if (type.IsAbstract)
			        GetDerivedTypesRecursively(type).ForEach(x=>result.Add(x));
		        else
			        result.Add(type);
	        }

	        return result;
        }

        private static Dictionary<string, Type> m_NameToTypeMap = new();
        public static bool TryGetTypeByName(string name, out Type type)
        {
			if (m_NameToTypeMap.Count == 0)
				FillNameToTypeMap();

			return m_NameToTypeMap.TryGetValue(name, out type);
        }

        private static void FillNameToTypeMap()
        {
	        Dictionary<string, Type> dictionary = new Dictionary<string, Type>();
	        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		        foreach (Type type in assembly.GetTypes())
			        dictionary[type.Name] = type;
	        
	        m_NameToTypeMap = dictionary;
        }
	}
}
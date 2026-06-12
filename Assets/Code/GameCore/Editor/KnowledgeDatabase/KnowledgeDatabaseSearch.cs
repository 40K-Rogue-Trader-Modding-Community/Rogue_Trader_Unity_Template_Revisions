using System;
using System.Text.RegularExpressions;
using Code.Editor.KnowledgeDatabase.Data;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.ElementsSystem;
using Kingmaker.Utility.DotNetExtensions;
using Kingmaker.Utility.UnityExtensions;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.KnowledgeDatabase
{
	public static class KnowledgeDatabaseSearch
	{
		private static readonly Regex BlueprintComponentsPathRegex = new(@"Blueprint\.Components\.Array\.data\[[0-9]+\]");
		
		public static Action<Type, string> OnDescriptionUpdated;
		
		[CanBeNull]
		private static KnowledgeDatabaseType GetTypeRecord([NotNull] Type type)
		{
			if (KnowledgeDatabaseCache.TypeGuidsPair.Empty())
			{
				KnowledgeDatabaseCache.UpdateTypeGuidCache();
			}

			if (!KnowledgeDatabaseCache.TypeGuidsPair.TryGetValue(type, out string guid) || guid == null)
				return null;

			if (KnowledgeDatabase.Instance == null) 
			{
				KnowledgeDatabase.ReloadDatabase();
			}

			return KnowledgeDatabase.Instance?.Records?.Get(guid);
		}
		
		[CanBeNull]
		public static KnowledgeDatabaseType GetTypeRecord([NotNull] string typeGuid)
		{
			if (KnowledgeDatabase.Instance.Records.ContainsKey(typeGuid))
			{
				return KnowledgeDatabase.Instance?.Records?.Get(typeGuid);
			}

			return null;
		}

		[CanBeNull]
		private static KnowledgeDatabaseField GetFieldRecord([NotNull] Type type, [CanBeNull] string fieldName)
		{
			var record = GetTypeRecord(type);

			if (fieldName == null || record == null || record.Fields.Empty() || !record.Fields.ContainsKey(fieldName))
			{
				return null;
			}

			return record.Fields[fieldName];
		}
		
		[CanBeNull]
		public static KnowledgeDatabaseField GetFieldRecord(KnowledgeDatabaseType type, [CanBeNull] string fieldName)
		{
			if (fieldName == null || type.Fields.Empty() || !type.Fields.ContainsKey(fieldName))
			{
				return null;
			}

			return type.Fields[fieldName];
		}

		[CanBeNull]
		public static string GetDescription([NotNull] Type type, [CanBeNull] string fieldName = null)
			=> fieldName != null ? GetFieldRecord(type, fieldName)?.Description : GetTypeRecord(type)?.Description;

		[CanBeNull]
		public static string GetCodeDescription([NotNull] Type type, [CanBeNull] string fieldName = null)
            => fieldName != null ? GetFieldRecord(type, fieldName)?.CodeDescription : GetTypeRecord(type)?.CodeDescription;
        
        [CanBeNull]
        public static string GetCodeDescription([NotNull] SerializedProperty property, bool tryTakeElementInside = true)
        {
            (var type, string fieldName) = property.GetTypeAndName(tryTakeElementInside);
            return GetCodeDescription(type, fieldName);
        }

        [CanBeNull]
		public static string GetDescription([NotNull] SerializedProperty property, bool tryTakeElementInside = true)
		{
			(var type, string fieldName) = property.GetTypeAndName(tryTakeElementInside);
			return GetDescription(type, fieldName);
		}

		public static void SetDescription(
			[NotNull] Type type, [CanBeNull] string fieldName, [NotNull] string description)
		{
			if (fieldName != null && GetFieldRecord(type, fieldName) is {} fieldRecord)
			{
				fieldRecord.Description = description;
			}
			else if (GetTypeRecord(type) is {} typeRecord)
			{
				typeRecord.Description = description;
			}
		}

		public static void SetDescription(
			[NotNull] KnowledgeDatabaseType type, [CanBeNull] string fieldName, [NotNull] string description)
		{
			var record = KnowledgeDatabase.Instance?.Records?.Get(type.Guid);
			if (record == null)
			{
				return;
			}

			if (fieldName != null && GetFieldRecord(type, fieldName) is {} fieldRecord)
			{
				fieldRecord.Description = description;
				return;
			}
			
			record.Description = description;
		}
		
		public static void SetFieldDescription(
			KnowledgeDatabaseType type, [CanBeNull] string fieldName, [NotNull] string description)
		{
			if (fieldName != null && !type.Fields.Empty() && type.Fields.ContainsKey(fieldName))
			{
				type.Fields[fieldName].Description = description;
			}
		}

		public static string GetKeywords([NotNull] KnowledgeDatabaseType type)
		{
			var record = KnowledgeDatabase.Instance?.Records?.Get(type.Guid);
			if (record == null)
			{
				return "";
			}

			return record.Keywords;
		}

		public static void AddKeywords([NotNull] KnowledgeDatabaseType type, [NotNull] string keywords)
		{
			var record = KnowledgeDatabase.Instance?.Records?.Get(type.Guid);
			if (record == null)
			{
				return;
			}
			record.Keywords = keywords;
		}

		public static (Type Type, string FieldName) GetTypeAndName([NotNull] this SerializedProperty property, 
            bool tryTakeElement = true)
		{
            string propertyPath = GetPropertyPath(property);
            int lastDotIndex = propertyPath.LastIndexOf('.');
            string fieldName = property.name is "data" or "m_Script" ? null : propertyPath[(lastDotIndex + 1)..];

            object target;
            if ((fieldName == null || tryTakeElement) && IsPropertyManagedReferenceNotNull(property))
            {
                target = property.managedReferenceValue;
                fieldName = null;
            }
            else
            {
                target = TryGetClosestManagedReferenceValue(property.GetParent()) ??
                         property.serializedObject.targetObject;
            }
            
            var objectType = GetObjectType(target);
            return (objectType, fieldName);
		}
        
        private static bool IsPropertyManagedReferenceNotNull([NotNull] SerializedProperty property)
        {
            return property is 
            { 
                propertyType: SerializedPropertyType.ManagedReference, 
                managedReferenceValue: SimpleBlueprint or BlueprintComponent or Element 
            };
        }
        
        private static Type GetObjectType(object target)
        {
            return target switch
            {
                BlueprintEditorWrapper wrapper => wrapper.Blueprint.GetType(),
                BlueprintComponentEditorWrapper componentWrapper => componentWrapper.Component.GetType(),
                _ => target.GetType()
            };
        }

		private static object TryGetClosestManagedReferenceValue(SerializedProperty property)
        {
			SerializedProperty currentProperty = property;
			while (currentProperty != null)
			{
                if (IsPropertyManagedReferenceNotNull(currentProperty))
                    return currentProperty.managedReferenceValue;

				currentProperty = currentProperty.GetParent();
			}

			return null;
		}

		[NotNull]
		private static string GetPropertyPath(SerializedProperty property)
		{
			// Commented out for UI Elements components
			// if (BlueprintComponentsPathRegex.Match(property.propertyPath).Success)
			// 	return string.Empty;

			if (property.propertyPath == "Blueprint")
				return string.Empty;

			return property.propertyPath;
		}

		public static string GetLink(SerializedProperty property)
		{
			(var type, string fieldName) = property.GetTypeAndName();
			return GetLink(type, fieldName);
		}
		
		public static string GetLink(Type type, [CanBeNull] string fieldName)
		{
			if (fieldName == null)
			{
				return GetTypeRecord(type)?.Link;
			}
			
			return GetFieldRecord(type, fieldName)?.Link ?? "";
		}
		
		[CanBeNull]
		public static string GetLink(KnowledgeDatabaseType type, [CanBeNull] string fieldName)
		{
			if (fieldName == null)
			{
				return GetTypeRecord(type.Guid)?.Link;
			}
			
			return GetFieldRecord(type, fieldName)?.Link ?? "";
		}

		public static void SetLink(Type type, [CanBeNull] string fieldName, [NotNull]string newLink)
		{
			if (fieldName == null)
			{
				var typeRecord = GetTypeRecord(type);
				if (typeRecord == null)
				{
					return;
				}
				typeRecord.Link = newLink;
				return;
			}
			
			var fieldRecord = GetFieldRecord(type, fieldName);
			if (fieldRecord == null)
			{
				return;
			}

			fieldRecord.Link = newLink;
		}
		
		public static void SetLink(KnowledgeDatabaseType type, [CanBeNull] string fieldName, string newLink)
		{
			if (fieldName == null)
			{
				var typeRecord = GetTypeRecord(type.Guid);
				if (typeRecord == null)
				{
					return;
				}
				typeRecord.Link = newLink;
				return;
			}

			var fieldRecord = GetFieldRecord(type, fieldName);
			if (fieldRecord == null)
			{
				return;
			}

			fieldRecord.Link = newLink;
		}

		public static bool HasLink(Type type, [CanBeNull] string fieldName)
		{
			if (fieldName == null)
			{
				var typeRecord = GetTypeRecord(type);
				return typeRecord != null && !typeRecord.Link.IsNullOrEmpty();
			}
			
			var fieldRecord = GetFieldRecord(type, fieldName);
			return fieldRecord != null && !fieldRecord.Link.IsNullOrEmpty();
		}
		
		
		public static void GoTo(string link)
		{
			Help.BrowseURL(link);
		}
	}
}
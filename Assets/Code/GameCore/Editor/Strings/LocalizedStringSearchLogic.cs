using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Kingmaker.Localization;
using Kingmaker.Localization.Enums;
using UnityEditor;
using UnityEngine;
using FieldInfo = System.Reflection.FieldInfo;

namespace Kingmaker.Editor.Strings
{
    [Flags]
    public enum SearchOption
    {
        None = 0,
        Path = 1 << 0,
        Name = 1 << 1,
        Group = 1 << 2,
        Text = 1 << 3,
        Regex = 1 << 4,
        TextRegex = Text | Regex,
    }

    public struct SearchQuery
    {
        public string TextQuery;
        public bool IsIgnoreCase;
        public SearchOption Option;
        public Locale Locale;
        public Regex Regex;
    }
    
    public class LocalizedStringSearchLogic
    {
        private struct SearchCacheItem
        {
            public string Path;
            public string Group;
            public string Name;
            public LocalizedString LocalizedString;
            public SerializedProperty SerializedProperty;
        }

        private List<SearchCacheItem> m_SearchCache;
        private readonly Dictionary<string, List<SerializedProperty>> m_SearchResult = new();
        
        private readonly SerializedObject m_Target;
        private readonly IEnumerable<FieldInfo> m_Fields;

        public LocalizedStringSearchLogic(SerializedObject target, IEnumerable<FieldInfo> fields)
        {
            m_Target = target;
            m_Fields = fields;
        }
        
        public bool Search(SearchQuery query, out IReadOnlyDictionary<string, IEnumerable<SerializedProperty>> result)
        {
            result = null;
            
            if (string.IsNullOrWhiteSpace(query.TextQuery)) return false;
            
            var predicate = GetSearchPredicateOrDefault(
                query.IsIgnoreCase, 
                query.Option, 
                query.Locale, 
                query.Regex);
            
            if (predicate == default) return false;

            var searchResult = GetSearchResultOrNull(query.TextQuery, predicate);
            
            //make a copy, so it won't get modified in the following searches
            result = searchResult?.ToDictionary(
                kvp => kvp.Key, 
                kvp => kvp.Value as IEnumerable<SerializedProperty>);
            
            return result != null;
        }

        private Dictionary<string, List<SerializedProperty>> GetSearchResultOrNull(
            string searchQuery, 
            Func<string, SearchCacheItem, bool> predicate)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return null;
            }

            searchQuery = searchQuery.Trim();
            m_SearchResult.Clear();
            PrepareSearchCache(m_Target, m_Fields);

            foreach (var item in m_SearchCache)
            {
                if (!predicate(searchQuery, item)) continue;

                if (!m_SearchResult.TryGetValue(item.Group, out var collection))
                {
                    collection = new List<SerializedProperty>();
                    m_SearchResult.Add(item.Group, collection);
                }
                
                collection.Add(item.SerializedProperty);
            }

            return m_SearchResult;
        }
        
        private void PrepareSearchCache(SerializedObject target, IEnumerable<FieldInfo> fields)
        {
            if (m_SearchCache != null) return;
            m_SearchCache = new List<SearchCacheItem>();
            
            CollectSearchCacheRecursive(
                target.targetObject, 
                target.targetObject.GetType().Name,
                m_SearchCache, 
                fields,
                target.FindProperty);
        }
        
        private void CollectSearchCacheRecursive(
            object targetObject,
            string parentName,
            ICollection<SearchCacheItem> items, 
            IEnumerable<FieldInfo> fields,
            Func<string, SerializedProperty> getSerializedProperty)
        {
            foreach (var field in fields)
            {
                var serializedProperty = getSerializedProperty(field.Name);

                if (TryProcessField(field, serializedProperty, targetObject, parentName, items))
                {
                    continue;
                }

                var childrenFields = GetSerializedFields(field.FieldType);
                var parentFieldObject = field.GetValue(targetObject);

                CollectSearchCacheRecursive(
                    parentFieldObject,
                    $"/{parentName}/{field.Name}",
                    items,
                    childrenFields,
                    serializedProperty.FindPropertyRelative);
            }
        }
        
        private bool TryProcessField(
            FieldInfo field, 
            SerializedProperty serializedProperty,
            object parentObject,
            string parentName,
            ICollection<SearchCacheItem> collection)
        {
            var fieldObject = field.GetValue(parentObject);

            if (field.FieldType == typeof(LocalizedString))
            {
                var localizedString = fieldObject as LocalizedString;
                AddSearchCacheItem(collection, localizedString, field.Name, parentName, serializedProperty);
                return true;
            }

            var isCollection = serializedProperty.isArray;
            if (!isCollection || fieldObject is not IEnumerable actualCollection)
            {
                return false;
            }
            
            var groupName = $"{parentName}/{field.Name}";

            //if field contains a collection of LocalizedString type
            if (serializedProperty.arrayElementType == nameof(LocalizedString))
            {
                var i = 0;
                foreach (var item in actualCollection)
                {
                    var element = serializedProperty.GetArrayElementAtIndex(i);
                    i++;

                    var localizedString = item as LocalizedString;
                    AddSearchCacheItem(collection, localizedString, element.displayName, groupName, element);
                }

                return true;
            }

            //if field contains a collection of some other than LocalizedString type
            if (serializedProperty.isArray)
            {
                var i = 0;
                foreach (var item in actualCollection)
                {
                    var element = serializedProperty.GetArrayElementAtIndex(i);
                    i++;
                    
                    if (element.isArray)
                    {
                        Debug.LogError("Search inside nested collections of second order currently is not supported");
                        return true;
                    }

                    var childFields = GetSerializedFields(item.GetType());

                    foreach (var childField in childFields)
                    {
                        TryProcessField(childField, element, item, groupName, collection);
                    }
                }

                return true;
            }
            
            return false;
        }

        private void AddSearchCacheItem(
            ICollection<SearchCacheItem> collection,
            LocalizedString localizedString, 
            string name, 
            string group,
            SerializedProperty serializedProperty)
        {
            collection.Add(new SearchCacheItem
            {
                Path = $"{group}/{name}",
                Group = group,
                Name = name,
                LocalizedString = localizedString,
                SerializedProperty = serializedProperty,
            });
        }
        
        private Func<string, SearchCacheItem, bool> GetSearchPredicateOrDefault(
            bool ignoreCase, 
            SearchOption option, 
            Locale locale,
            Regex regex)
        {
            return (query, item) =>
            {
                var key = GetKey(item, option, locale);
                
                if (ignoreCase)
                {
                    key = key.ToLowerInvariant();
                    query = query.ToLowerInvariant();
                }

                if (!option.HasFlag(SearchOption.Regex))
                {
                    return key.Contains(query);
                }
                
                return regex?.IsMatch(key) ?? default;
            };

            string GetKey(SearchCacheItem searchCacheItem, SearchOption searchOption, Locale targetLocale)
            {
                if (searchOption.HasFlag(SearchOption.Text))
                {
#if EDITOR_FIELDS
                    return searchCacheItem.LocalizedString.GetText(targetLocale);
#else
                    Debug.LogError("EDITOR_FIELDS is disabled");
                    return string.Empty;
#endif
                }
                
                return searchOption switch
                {
                    SearchOption.Path => searchCacheItem.Path,
                    SearchOption.Name => searchCacheItem.Name,
                    SearchOption.Group => searchCacheItem.Group,
                    _ => string.Empty,
                };
            }
        }
        
        private IEnumerable<FieldInfo> GetSerializedFields(Type type)
        {
            return type
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => Attribute.IsDefined(f.FieldType, typeof(SerializableAttribute)));
        }
    }
}
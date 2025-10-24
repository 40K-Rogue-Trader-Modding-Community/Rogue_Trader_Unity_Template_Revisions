using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Kingmaker.Blueprints.Root.Strings;
using Kingmaker.Editor.UIElements.Custom.Properties;
using Kingmaker.Localization;
using Kingmaker.Localization.Enums;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.Strings
{
    [CustomEditor(typeof(UIStrings), true)]
    public class UIStringsEditor : UnityEditor.Editor
    {
        private const string EditorPrefOptionKey = "UIStringsEditor.selectedGroup";
        private const string EditorPrefLocaleKey = "UIStringsEditor.searchLocale";
        private const string EditorPrefSearchOptionKey = "UIStringsEditor.searchOption";
        private const string EditorPrefIgnoreCaseKey = "UIStringsEditor.ignoreCase";
        private const string EditorPrefSearchQueryKey = "UIStringsEditor.searchQuery";
        private const string EditorPrefSearchRegex = "UIStringsEditor.searchRegex";

        private List<FieldInfo> m_Fields;
        
        private Dictionary<string, IEnumerable<SerializedProperty>> m_PropertiesByGroup;
        private List<string> m_GroupDropdownOptions;
        private string m_LastSelectedGroup;

        private static readonly List<SearchOption> m_SearchOptions = new ()
        {
            SearchOption.None,
            SearchOption.Path,
            SearchOption.Name,
            SearchOption.Group,
            SearchOption.Text,
            SearchOption.Regex, //should not show up in popup
            SearchOption.TextRegex,
        };
        
        private List<Locale> m_SearchLocales;
        private SearchOption m_SearchOption = SearchOption.Text;
        private Locale m_SearchLocale = Locale.dev;
        private string m_SearchQuery = string.Empty;
        private bool m_SearchIgnoresCase = true;
        private Regex m_Regex;

        private LocalizedStringSearchLogic m_Search;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            CollectFields();

            if (!TryGetDefaultGroup(out var defaultGroup) || !TryDrawPopup(out var popup, defaultGroup))
            {
                return root;
            }
            
            RestorePrefs();
            
            m_SearchLocales = Enum.GetValues(typeof(Locale)).Cast<Locale>().ToList();

            var groupContainer = new VisualElement();
            root.Add(popup);

            var search = DrawSearch(
                result => DrawProperties(groupContainer, result),
                () => DrawStringsGroup(groupContainer, m_LastSelectedGroup));
            
            root.Add(search);
            root.Add(groupContainer);
            
            popup.RegisterValueChangedCallback(evt =>
            {
                DrawStringsGroup(groupContainer, evt.newValue);
                EditorPrefs.SetString(EditorPrefOptionKey, evt.newValue);
            });

            if (!string.IsNullOrWhiteSpace(m_SearchQuery) && Search(out var searchResult))
            {
                DrawProperties(groupContainer, searchResult);
                return root;
            }

            DrawStringsGroup(groupContainer, defaultGroup);

            return root;
        }

        private void RestorePrefs()
        {
            m_SearchIgnoresCase = EditorPrefs.GetBool(EditorPrefIgnoreCaseKey, true);
            m_SearchQuery = EditorPrefs.GetString(EditorPrefSearchQueryKey, string.Empty);

            TryGetEditorPrefString(EditorPrefLocaleKey, out m_SearchLocale, str 
                => Enum.TryParse<Locale>(str, out var result)
                    ? result
                    : Locale.dev);
            
            TryGetEditorPrefString(EditorPrefSearchOptionKey, out m_SearchOption, str
                => Enum.TryParse<SearchOption>(str, out var result)
                    ? result
                    : SearchOption.Text);
            
            var regexPattern = EditorPrefs.GetString(EditorPrefSearchRegex, string.Empty);
            if (!string.IsNullOrEmpty(regexPattern))
            {
                m_Regex = new Regex(regexPattern);
            }
        }
        
        private void CollectFields()
        {
            m_Fields = GetSerializedFields(typeof(UIStrings)).ToList();

            m_PropertiesByGroup?.Clear();
            m_PropertiesByGroup ??= new();

            var so = serializedObject;

            foreach (var field in m_Fields)
            {
                var parent = so.FindProperty(field.Name);
                var properties = GetSerializedFields(field.FieldType)
                    .Select(p => parent.FindPropertyRelative(p.Name)).ToArray();
                
                var key = $"{field.Name.ToUpper()[0]}/{field.Name}";
     
                m_PropertiesByGroup.Add(key, properties);
            }
            
            m_PropertiesByGroup.Add("None", Array.Empty<SerializedProperty>());
            
            m_GroupDropdownOptions = m_PropertiesByGroup.Select(kvp => kvp.Key).ToList();
            m_GroupDropdownOptions.Sort((a, b) =>
            {
                if (a == "None") return -1;
                if (b == "None") return 1;
                
                return string.Compare(a, b, StringComparison.Ordinal);
            });
        }
        
        private IEnumerable<FieldInfo> GetSerializedFields(Type type)
        {
            return type
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => Attribute.IsDefined(f.FieldType, typeof(SerializableAttribute)));
        }

        private VisualElement DrawSearch(
            Action<IReadOnlyDictionary<string, IEnumerable<SerializedProperty>>> onSearch, 
            Action onSearchCleared)
        {
            var searchBox = DrawBox(Vector4.one * 5f, Color.clear);
            var searchFieldContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Auto,
                }
            };
            
            var filtersContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Auto,
                }
            };
            
            searchBox.Add(searchFieldContainer);
            searchBox.Add(filtersContainer);
            
            var ignoreCaseToggle = new Toggle("Ignore case");
            ignoreCaseToggle.RegisterValueChangedCallback(evt =>
            {
                m_SearchIgnoresCase = evt.newValue;
                EditorPrefs.SetBool(EditorPrefIgnoreCaseKey, evt.newValue);
            });
            ignoreCaseToggle.SetValueWithoutNotify(true);

            var queryTextField = new TextField { style = { flexGrow = 1 } };
            queryTextField.RegisterValueChangedCallback(evt =>
            {
                m_SearchQuery = evt.newValue;
                EditorPrefs.SetString(EditorPrefSearchQueryKey, m_SearchQuery);
            });
            queryTextField.SetValueWithoutNotify(m_SearchQuery);

            var localesPopup = new PopupField<Locale>(m_SearchLocales, m_SearchLocale);
            localesPopup.RegisterValueChangedCallback(evt =>
            {
                m_SearchLocale = evt.newValue;
                EditorPrefs.SetString(EditorPrefLocaleKey, evt.newValue.ToString());
            });
            localesPopup.tooltip = "Search in selected localization";

            var searchOptionsPopup = new PopupField<SearchOption>(m_SearchOptions, m_SearchOption);
            searchOptionsPopup.RegisterValueChangedCallback(evt =>
            {
                m_SearchOption = evt.newValue;
                EditorPrefs.SetString(EditorPrefSearchOptionKey, evt.newValue.ToString());
                searchOptionsPopup.tooltip = GetSearchOptionTooltipText(evt.newValue);

                if (ShowLocales(evt.newValue))
                {
                    filtersContainer.Add(localesPopup);
                }
                else
                {
                    try
                    {
                        filtersContainer.Remove(localesPopup);
                    } catch (ArgumentException){/*ignored*/}
                }

            });
            searchOptionsPopup.tooltip = GetSearchOptionTooltipText(m_SearchOption);

            //search button
            var searchButton = new Button(() =>
            {
                if (Search(out var searchResult))
                {
                    if (ShowRegex(m_SearchOption))
                    {
                        m_Regex = new Regex(m_SearchQuery);
                    }
                    
                    onSearch(searchResult);
                }
            });
            searchButton.text = "Search";
            searchButton.style.backgroundColor = new Color(0, 0.3f, 0);
            
            //clear search button
            var clearSearchButton = new Button(() =>
            {
                queryTextField.SetValueWithoutNotify(string.Empty);
                m_SearchQuery = string.Empty;
                EditorPrefs.DeleteKey(EditorPrefSearchQueryKey);
                onSearchCleared();
            });
            clearSearchButton.text = "Clear";

            searchFieldContainer.Add(queryTextField);
            searchFieldContainer.Add(searchButton);
            searchFieldContainer.Add(clearSearchButton);
            
            filtersContainer.Add(ignoreCaseToggle);
            filtersContainer.Add(searchOptionsPopup);
            
            if (ShowLocales(m_SearchOption))
            {
                filtersContainer.Add(localesPopup);
            }

            return searchBox;

            bool ShowLocales(SearchOption currentOption)
            {
                return currentOption.HasFlag(SearchOption.Text);
            }

            bool ShowRegex(SearchOption currentOption)
            {
                return currentOption.HasFlag(SearchOption.Regex);
            }
        }

        private bool TryDrawPopup(out PopupField<string> element, string defaultValue)
        {
            if (string.IsNullOrEmpty(defaultValue))
            {
                element = null;
                return false;
            }

            try
            {
                element = new PopupField<string>("Select Strings Group:", m_GroupDropdownOptions, defaultValue);
            }
            catch (Exception e)
            {
                element = null;
                Debug.LogException(e);
                return false;
            }
            
            return true;
        }

        private void DrawStringsGroup(VisualElement container, string selected)
        {
            if (string.IsNullOrWhiteSpace(selected) 
                || !m_PropertiesByGroup.TryGetValue(selected, out var properties))
            {
                container.Clear();
                return;
            }
            
            m_LastSelectedGroup = selected;
            DrawProperties(container, properties);
        }

        private void DrawProperties(VisualElement container, IEnumerable<SerializedProperty> properties)
        {
            container.Clear();
            
            foreach (var property in properties)
            {
                DrawProperty(container, property);
            }
            
            if (container.childCount > 0) return;

            container.Add(DrawSpacer(20f));
            var noItemsText = new Label("Nothing is found");
            container.Add(noItemsText);
        }
        
        private void DrawProperties(
            VisualElement container, 
            IReadOnlyDictionary<string, IEnumerable<SerializedProperty>> properties)
        {
            container.Clear();
            
            foreach (var (group, propertiesCollection) in properties)
            {
                container.Add(new Label(group)
                {
                    style = { unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold)}
                });
                
                foreach (var property in propertiesCollection)
                {
                    DrawProperty(container, property);
                }
                
                container.Add(DrawSpacer(25f));
            }
            
            if (container.childCount > 0) return;

            container.Add(DrawSpacer(20f));
            var noItemsText = new Label("Nothing is found");
            container.Add(noItemsText);
        }

        private void DrawProperty(VisualElement container, SerializedProperty property)
        {
            container.Add(DrawSpacer(5f));
                
            if (string.Equals(property.type, nameof(LocalizedString)) && !property.isArray)
            {
                var localizedString = new LocalizedStringProperty(property)
                {
                    style =
                    {
                        borderTopWidth = 1,
                        borderBottomWidth = 1,
                        borderLeftWidth = 1,
                        borderRightWidth = 1,
                            
                        borderTopColor = Color.black,
                        borderBottomColor = Color.black,
                        borderLeftColor = Color.black,
                        borderRightColor = Color.black,
                            
                        backgroundColor = new Color(0.3f, 0.3f, 0.3f),
                            
                        paddingTop = 5f,
                        paddingBottom = 5f,
                        paddingLeft = 5f,
                        paddingRight = 5f,
                    }
                };
                container.Add(localizedString);
                return;
            }

            var propertyField = new PropertyField(property);
            propertyField.Bind(serializedObject);
            container.Add(propertyField);
        }

        private VisualElement DrawSpacer(float height)
        {
            return new VisualElement
            {
                style = { height = height }
            };
        }

        private VisualElement DrawBox(Vector4 padding, Color backgroundColor)
        {
            return new VisualElement
            {
                style =
                {
                    paddingLeft = padding.x,
                    paddingRight = padding.y,
                    paddingTop = padding.z,
                    paddingBottom = padding.w,
                    
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                            
                    borderTopColor = Color.black,
                    borderBottomColor = Color.black,
                    borderLeftColor = Color.black,
                    borderRightColor = Color.black,
                    
                    backgroundColor = backgroundColor,
                }
            };
        }

        private bool Search(out IReadOnlyDictionary<string, IEnumerable<SerializedProperty>> result)
        {
            if (string.IsNullOrWhiteSpace(m_SearchQuery))
            {
                result = null;
                return false;
            }

            m_Search ??= new LocalizedStringSearchLogic(serializedObject, m_Fields);
            return m_Search.Search(new SearchQuery
            {
                TextQuery = m_SearchQuery,
                IsIgnoreCase = m_SearchIgnoresCase,
                Option = m_SearchOption,
                Locale = m_SearchLocale,
                Regex = m_Regex,
            }, out result);
        }

        private bool TryGetDefaultGroup(out string stringGroup)
        {
            if (m_PropertiesByGroup == null || m_PropertiesByGroup.Count < 1)
            {
                stringGroup = null;
                return false;
            }
            
            stringGroup = EditorPrefs.GetString(EditorPrefOptionKey);

            if (m_PropertiesByGroup.ContainsKey(stringGroup))
            {
                return true;
            }
            
            EditorPrefs.DeleteKey(EditorPrefOptionKey);
            stringGroup = m_GroupDropdownOptions?.First();
            return stringGroup != null && m_PropertiesByGroup.ContainsKey(stringGroup);
        }

        private bool TryGetEditorPrefString<T>(string key, out T pref, Func<string, T> convert)
        {
            if (!EditorPrefs.HasKey(key))
            {
                pref = default;
                return false;
            }

            var value = EditorPrefs.GetString(key);

            try
            {
                pref = convert(value);
                return !pref.Equals(default(T));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorPrefs.DeleteKey(key);
                pref = default;
                return false;
            }
        }

        private string GetSearchOptionTooltipText(SearchOption option)
        {
            return option switch
            {
                SearchOption.Name => "Search by string name",
                SearchOption.Group => "Search by string group",
                SearchOption.Path => "Search by string path (name/group)",
                SearchOption.Text => "Search by locale text",
                SearchOption.TextRegex => "Search by locale text using regex",
                _ => string.Empty,
            };
        }
    }
}
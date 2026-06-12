using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Editor.UIElements.Custom.Base;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom
{
    public class FilteredEnumProperty : OwlcatProperty
    {
        private readonly Dictionary<string, int> m_StringToInt = new Dictionary<string, int>();
        private readonly Dictionary<int, string> m_IntToString = new Dictionary<int, string>();

        public FilteredEnumProperty(SerializedProperty property, List<Enum> filteredList) : base(property)
        {
            var choices = new List<string>();
            for (int i = 0; i < filteredList.Count; i++)
            {                
                int enumIndex = System.Array.IndexOf(Property.enumNames, filteredList[i].ToString());
                string enumDisplayName = Property.enumDisplayNames[enumIndex];
                m_StringToInt.Add(enumDisplayName, enumIndex);
                choices.Add(enumDisplayName);
            }

            foreach (var kvp in m_StringToInt)
                m_IntToString.Add(kvp.Value, kvp.Key);

            if (!m_IntToString.ContainsKey(Property.enumValueIndex))
            {
                Property.enumValueIndex = m_IntToString.First().Key;
                Property.serializedObject.ApplyModifiedProperties();
                Property.serializedObject.Update();
            }

            var dropDown = new DropdownField { style = { flexGrow = 1, flexShrink = 1 } };
            dropDown.choices = choices;
            dropDown.value = m_IntToString[Property.enumValueIndex];

            dropDown.RegisterValueChangedCallback(e =>
            {
                Property.enumValueIndex = m_StringToInt[e.newValue];
                Property.serializedObject.ApplyModifiedProperties();
                Property.serializedObject.Update();
            });
            
            this.TrackPropertyValue(Property, x =>
            {
                dropDown.value = m_IntToString[Property.enumValueIndex];
            });

            ContentContainer.Add(dropDown);
        }
    }
}
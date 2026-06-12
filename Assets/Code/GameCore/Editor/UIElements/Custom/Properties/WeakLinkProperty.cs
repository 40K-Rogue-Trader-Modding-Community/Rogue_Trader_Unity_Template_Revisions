using System;
using System.Linq;
using System.Reflection;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Elements;
using Kingmaker.Editor.UIElements.Custom.PropertyComponents;
using Kingmaker.Editor.UIElements.ValuePicker;
using Kingmaker.Editor.Utility;
using Kingmaker.ResourceLinks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Kingmaker.Editor.UIElements.Custom.Properties
{
    public class WeakLinkProperty : OwlcatProperty
    {
        private readonly RobustSerializedProperty m_IdProperty;

        private readonly Type m_Type;
        private readonly FieldInfo m_FieldInfo;
        private readonly Func<AssetPicker.HierarchyEntry, bool> m_Filter;

        private OwlcatGenericObjectField m_ObjectField;
        private DragAndDropComponent m_DragAndDropComponent;

        public WeakLinkProperty(SerializedProperty property, Type type, FieldInfo fieldInfo,
            Func<AssetPicker.HierarchyEntry, bool> filter) : base(property, Layout.Horizontal)
        {
            m_IdProperty =
                new RobustSerializedProperty(property.FindPropertyRelative(nameof(WeakResourceLink.AssetId)));

            m_Type = type;
            m_FieldInfo = fieldInfo;
            m_Filter = filter;
            
            this.TrackPropertyValue(m_IdProperty.Property, _ => { UpdateView(); });
        }

        protected override void CreateContentInternal()
        {
            m_ObjectField = new OwlcatGenericObjectField(OnPickerClick, OnMouseClick, OnKeyDown);
            m_DragAndDropComponent = new DragAndDropComponent(m_ObjectField.Display, CanDragAndDrop, ApplyDragAndDrop);
            ContentContainer.Add(m_ObjectField);
            UpdateView();
        }

        private void OnPickerClick()
        {
            AssetPicker.ShowAssetPicker(m_Type, m_FieldInfo, OnPick, GetCurrentValue(), m_Filter);
        }

        private void OnPick(Object obj)
        {
            m_IdProperty.Property.stringValue = GenericWeakLinkDrawer.GetGuid(obj, m_Type);
            m_IdProperty.Property.serializedObject.ApplyModifiedProperties();
            m_IdProperty.Property.serializedObject.Update();
        }

        private void UpdateView()
        {
            string value = m_IdProperty.Property.stringValue;
            TitleLabel.text = string.IsNullOrEmpty(value) || !Property.IsArrayElement() ? Property.displayName : value;
            m_ObjectField.UpdateView(GetCurrentValue(), m_Type);
        }

        private Object GetCurrentValue()
        {
            return GenericWeakLinkDrawer.GetAsset(
                m_IdProperty.Property.hasMultipleDifferentValues ? null : m_IdProperty.Property.stringValue, m_Type);
        }
        
        private void OnMouseClick(MouseDownEvent evt)
        {
            if (evt.button == 0 && evt.clickCount == 1)
            {
                EditorGUIUtility.PingObject(GetCurrentValue());
            }
            else if (evt.button == 0 && evt.clickCount == 2)
            {
                AssetDatabase.OpenAsset(GetCurrentValue());
                evt.StopPropagation();
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
            {
                AssetDatabase.OpenAsset(GetCurrentValue());
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Delete)
            {
                m_IdProperty.Property.stringValue = null;
                m_IdProperty.Property.serializedObject.ApplyModifiedProperties();
                m_IdProperty.Property.serializedObject.Update();
                
                evt.StopPropagation();
            }
        }

        private bool CanDragAndDrop()
        {
            return DragAndDrop.objectReferences.Length == 1 &&
                    (DragAndDrop.objectReferences.FirstOrDefault() is GameObject go &&
                    go.GetComponents<Component>().Any(m_Type.IsInstanceOfType) || 
                    DragAndDrop.objectReferences.FirstOrDefault(o => m_Type.IsInstanceOfType(o)) != null);
        }

        private void ApplyDragAndDrop()
        {
            Object asset = null;

            if (DragAndDrop.objectReferences.FirstOrDefault(o => o) is GameObject go)
            {
                foreach (var component in go.GetComponents<Component>())
                {
                    if (m_Type.IsInstanceOfType(component))
                    {
                        asset = component;
                        break;
                    }
                }
            }

            if (asset == null)
                asset = DragAndDrop.objectReferences.FirstOrDefault(o => m_Type.IsInstanceOfType(o));

            OnPick(asset);
        }
    }
}
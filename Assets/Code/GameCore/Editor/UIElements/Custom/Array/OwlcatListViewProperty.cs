using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.PropertyComponents;
using Kingmaker.Editor.UIElements.ValuePicker;
using Kingmaker.Editor.Utility;
using Kingmaker.ElementsSystem;
using Kingmaker.PubSubSystem.Core;
using Owlcat.QA.Bebilith;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Array
{
    public class OwlcatListViewProperty : OwlcatProperty
    {
        private readonly Type[] m_ValidTypes;
        private readonly RobustSerializedProperty m_ArrayProperty;
        private readonly Dictionary<int, VisualElement> m_Cache = new();
        private readonly string m_Blueprint;

        private Color? m_BorderColor;
        private ListView m_ListView;
        private TextField m_ArraySizeField;
        private bool m_HideArraySizeField;

        public int MovedIndex { get; set; } = -1;
        
        public RobustSerializedProperty ArrayProperty => m_ArrayProperty;

        public static OwlcatListViewProperty ClickedList { get; private set; }

        private static Dictionary<string, Dictionary<string, HashSet<OwlcatListViewProperty>>> m_ActivePanels = new();
        
        private int m_UndoRedoSize = -1;
        
        public OwlcatListViewProperty(SerializedProperty holderProperty, SerializedProperty arrayProperty,
            Type[] validTypes = null,
            Color? borderColor = null,
            bool hideArraySizeField = false) : base(holderProperty, Layout.Vertical)
        {
            m_HideArraySizeField = hideArraySizeField;
            m_ArrayProperty = arrayProperty;
            m_Blueprint = BlueprintEditorWrapper.Unwrap<BlueprintScriptableObject>(
                m_ArrayProperty?.serializedObject?.targetObject)?.AssetGuid ?? string.Empty;
            m_ValidTypes = validTypes;
            m_BorderColor = borderColor;
            OverridableControl?.ChangeProperty(arrayProperty);
            //ContentContainer.style.paddingLeft = 0;
            //ContentContainer.style.marginLeft = -8;
            
            AddComponent(new ListDragAndDropComponent(this, arrayProperty));
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanelLocal);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanelLocal);
        }
        
        protected override void OnIsExpandedChanged()
        {
            if (Event.current == null || !Event.current.alt)
                return;

            m_Cache.Clear();
            for (int i = 0; i < m_ArrayProperty.Property.arraySize; i++)
            {
                var p = m_ArrayProperty.Property.GetArrayElementAtIndex(i);
                p.isExpanded = IsExpanded;
            }
            
            m_ArrayProperty.Property.serializedObject.ApplyModifiedProperties();
            m_ListView?.RefreshItems();
        }

        private void OnAttachToPanelLocal(AttachToPanelEvent evt)
        {
            if (!m_ActivePanels.ContainsKey(m_Blueprint))
                m_ActivePanels.Add(m_Blueprint, new Dictionary<string, HashSet<OwlcatListViewProperty>>());
            
            if (!m_ActivePanels[m_Blueprint].ContainsKey(m_ArrayProperty.Path))
                m_ActivePanels[m_Blueprint].Add(m_ArrayProperty.Path, new HashSet<OwlcatListViewProperty>());

            m_ActivePanels[m_Blueprint][m_ArrayProperty.Path].Add(this);
            
            Undo.undoRedoPerformed += UndoRedoPerformed;
            
            EventBus.Subscribe(this);
        }

        private void OnDetachFromPanelLocal(DetachFromPanelEvent evt)
        {
            if (m_ActivePanels.ContainsKey(m_Blueprint))
            {
                if (m_ActivePanels[m_Blueprint].ContainsKey(m_ArrayProperty.Path))
                {
                    m_ActivePanels[m_Blueprint][m_ArrayProperty.Path].Remove(this);
                    
                    if (m_ActivePanels[m_Blueprint][m_ArrayProperty.Path].Count == 0)
                        m_ActivePanels[m_Blueprint].Remove(m_ArrayProperty.Path);
                }
                
                if (m_ActivePanels[m_Blueprint].Count == 0)
                    m_ActivePanels.Remove(m_Blueprint);
            }
            
            EventBus.Unsubscribe(this);
            m_Cache.Clear();
            
            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }
        
        private void UndoRedoPerformed()
        {
            m_UndoRedoSize = m_ArrayProperty.Property.arraySize;
            EditorApplication.delayCall += () => m_UndoRedoSize = -1;
        }

        protected override void OnAfterAttachToPanelInternal()
        {
            base.OnAfterAttachToPanelInternal();
            
            m_ListView = CreateListView();
            ContentContainer.Add(m_ListView);

            if (!m_HideArraySizeField)
            {
                m_ArraySizeField = CreateSizeField();
                ControlsContainer.Insert(HasComponent<ElementListTitleProvider>() ? 1 : 0, m_ArraySizeField);
            }

            if (m_BorderColor.HasValue)
                SetBorderColor(m_BorderColor.Value);
        }

        private ListView CreateListView()
        {
            var info = Property.GetFieldInfo();
            bool nonReorderable = info?.HasAttribute<NonReorderableAttribute>() ?? false;

            var listView = new ListView
            {
                makeItem = MakeItem,
                bindItem = BindItem,
                unbindItem = UnbindItem,
                onAdd = OnAdd,
                onRemove = OnRemove,
                allowAdd = true,
                fixedItemHeight = EditorGUIUtility.singleLineHeight,
                showAddRemoveFooter = true,
                showFoldoutHeader = false,
                showBoundCollectionSize = false,
                showBorder = true,
                reorderable = !nonReorderable,
                reorderMode = ListViewReorderMode.Animated,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                selectionType = SelectionType.Multiple,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight
            };

            // Uncomment this to disable scroll
            // listView.style.maxHeight = float.MaxValue;
            
            listView.selectedIndicesChanged += OnSelectionChanged;
            listView.itemIndexChanged += ItemIndexChanged;
            listView.BindProperty(m_ArrayProperty);

            if (!m_HideArraySizeField)
            {
                this.TrackPropertyValue(m_ArrayProperty, _ => 
                { 
                    m_ArraySizeField?.SetValueWithoutNotify(m_ArrayProperty.Property.arraySize.ToString()); 
                    
                    if (m_UndoRedoSize >= 0 && m_UndoRedoSize != m_ArrayProperty.Property.arraySize)
                        m_Cache.Clear();
                    
                    m_UndoRedoSize = -1;
                });
            }

            return listView;
        }

        private void ItemIndexChanged(int arg1, int arg2)
        {
            MovedIndex = arg2;
            int max = Mathf.Max(arg1, arg2);
            int min = Mathf.Min(arg1, arg2);
            InvalidateCache(min, max);
            
            EditorApplication.delayCall += () =>
            {
                var panels = new HashSet<OwlcatListViewProperty>(m_ActivePanels[m_Blueprint][m_ArrayProperty.Property.propertyPath]);
                foreach (var list in panels)
                {
                    if (list != this)
                        list.m_ListView.RefreshItems();
                }
            };
        }

        private VisualElement MakeItem()
        {
            return new OwlcatListViewEntryWrapper(this);
        }

        private void BindItem(VisualElement element, int index)
        {
            var property = index < m_ArrayProperty.Property.arraySize ? 
                m_ArrayProperty.Property.GetArrayElementAtIndex(index) : null;
            
            (element as OwlcatListViewEntryWrapper)?.Bind(property, index);
        }

        private void UnbindItem(VisualElement element, int index)
        {
            (element as OwlcatListViewEntryWrapper)?.Unbind();
        }

        private void OnAdd(BaseListView view)
        {
            int oldCount = m_ArrayProperty.Property.arraySize;
            if (m_ValidTypes == null)
            {
                PrototypedObjectEditorUtility.AddArrayElement(m_ArrayProperty.Property,
                    m_ArrayProperty.Property.arraySize);
                ExpandNewElement(oldCount);
            }
            else
            {
                if (m_ValidTypes.Length == 1)
                {
                    AddElement(m_ValidTypes.First());
                    ExpandNewElement(oldCount);
                    return;
                }

                TypePicker.ShowPickerWindow(
                    this,
                    "Add Element",
                    () => m_ValidTypes,
                    type =>
                    {
                        AddElement(type);
                        ExpandNewElement(oldCount);
                    }
                );
            }
        }
        
        private void ExpandNewElement(int oldCount)
        {
            int newCount = m_ArrayProperty.Property.arraySize;
            if (newCount <= oldCount)
                return;

            var lastElement = m_ArrayProperty.Property.GetArrayElementAtIndex(newCount - 1);
            lastElement.isExpanded = true;
        }

        private void AddElement(Type elementType)
        {
            if (!typeof(Element).IsAssignableFrom(elementType))
            {
                PrototypedObjectEditorUtility.AddArrayElement(m_ArrayProperty.Property,
                    m_ArrayProperty.Property.arraySize, elementType);
            }
            else
            {
                TypeUtility.AddElementFromMenu(m_ArrayProperty.Property, elementType);
            }
        }
        
        public bool TryRemoveSelected()
        {
            if (m_ListView.selectedIndices.Any())
            {
                int min = m_ListView.selectedIndices.Min();
                InvalidateCache(min, m_ListView.viewController.GetItemsCount() - 1);
                m_ListView.viewController.RemoveItems(m_ListView.selectedIndices.ToList());
                m_ListView.ClearSelection();
                return true;
            }

            return false;
        }
        
        public void RemoveItemAt(int index)
        {
            int last = m_ListView.viewController.GetItemsCount() - 1;
            InvalidateCache(index, last);
            m_ListView.viewController.RemoveItem(index);
        }
        
        private void OnRemove(BaseListView view)
        {
            if (TryRemoveSelected())
                return;

            var itemsSource = view.itemsSource;
            if (itemsSource is { Count: > 0 })
                RemoveItemAt(itemsSource.Count - 1);
        }

        public void SetClicked()
        {
            ClickedList = this;
            EditorApplication.delayCall += () => ClickedList = null;
        }

        private void OnSelectionChanged(IEnumerable<int> indices)
        {
            if (ClickedList != this)
                return;

            foreach (var list in GetUpperRoot().Query<ListView>().ToList())
            {
                if (list != ClickedList.m_ListView)
                    list.ClearSelection();
            }
        }

        private TextField CreateSizeField()
        {
            TextField textField = new TextField();
            textField.AddToClassList("owlcat-readonly");
            textField.name = BaseListView.arraySizeFieldUssClassName;
            textField.focusable = false;
            textField.isReadOnly = true;
            textField.SetValueWithoutNotify(m_ArrayProperty.Property.arraySize.ToString());

            return textField;
        }
        
        public VisualElement GetFromCache(int index) => m_Cache.GetValueOrDefault(index);

        public void AddToCache(int index, VisualElement element) => m_Cache[index] = element;
        public void RemoveFromCache(int index) => m_Cache.Remove(index);

        private void InvalidateCache(int from, int to)
        {
            foreach (var list in m_ActivePanels[m_Blueprint][m_ArrayProperty.Property.propertyPath])
            {
                for (int i = from; i <= to; i++)
                    list.m_Cache.Remove(i);
            }
        }
        
        public void OnBeforeDiscard() => m_Cache.Clear();

        public void OnAfterDiscard() => m_ListView?.RefreshItems();

        public void SetConditionProperty(VisualElement enumField)
        {
            HeaderContainer.Add(enumField);
            OverridableControl.AddAdditional(enumField);
        }

        public void SetBorderColor(Color color)
        {
            m_BorderColor = color;

            if (m_ListView != null)
            {
                var viewport = m_ListView.Q("unity-content-viewport");
                viewport.style.borderLeftWidth = 2;
                viewport.style.borderLeftColor = m_BorderColor.Value;
            }
        }

        public void RefreshItemAt(int index)
        {
            m_ListView.RefreshItem(index);
        }
    }
}
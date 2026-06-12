using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Code.Editor.Utility;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.Blueprints.Creation;
using Kingmaker.Editor.Blueprints.Elements;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Elements;
using Kingmaker.Editor.UIElements.Custom.PropertyComponents;
using Kingmaker.Editor.Utility;
using Kingmaker.Utility.DotNetExtensions;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Properties
{
    public partial class BlueprintReferenceProperty : OwlcatProperty
    {
        private ReferenceField m_Field;

        private readonly RobustSerializedProperty m_GuidProp;
        
        private readonly VisualElement m_ExpandButton;
        private readonly VisualElement m_ClearButton;
        private readonly VisualElement m_NewButton;
        
        private readonly Action CreateNewCustom;

        private bool m_IsHaveNewButton;
        private bool m_IsNicolayButton;
        private string m_DefaultCreatedName;
        private string m_DefaultCreationPath;

        public VisualElement FieldLayout { get; private set; }
        
        public ReferenceField Field => m_Field;
        
        public event Func<string, bool> CanChangeValue;
        public event Action OnValueChangedEvent;

        public BlueprintReferenceProperty(SerializedProperty property, FieldInfo fieldInfo,
            RobustSerializedProperty guidProperty, bool inline, Type refType = null, Action createNewCustom = null) : 
            base(property, inline ? Layout.Vertical : Layout.Horizontal)
        {
            m_GuidProp = guidProperty;
            m_Inline = inline;
            CreateNewCustom = createNewCustom;
            
            m_ExpandButton = CreateExpandButton();
            m_ClearButton = CreateSmallTextButton("clear", OnClearButtonClicked);
            m_NewButton = CreateNewButton(fieldInfo);

            Init(fieldInfo, refType);
        }

        public BlueprintReferenceProperty(SerializedProperty property, FieldInfo fieldInfo, bool inline,
            string guidName = "guid", Type refType = null, Action createNewCustom = null) : 
            base(property, inline ? Layout.Vertical : Layout.Horizontal)
        {
            m_GuidProp = new RobustSerializedProperty(property.FindPropertyRelative(guidName));
            m_Inline = inline;
            CreateNewCustom = createNewCustom;
            
            m_ExpandButton = CreateExpandButton();
            m_ClearButton = CreateSmallTextButton("clear", OnClearButtonClicked);
            m_NewButton = CreateNewButton(fieldInfo);

            Init(fieldInfo, refType);
        }

        private void Init(FieldInfo fieldInfo, Type refType = null)
        {
            string guid = m_GuidProp.Property.stringValue;
            AddComponent(new FuncTitleProviderComponent(() =>
            {
                if (SerializedPropertyEx.IsArrayElement(Property))
                {
                    string title = m_GuidProp.Property.stringValue;
                    if (string.IsNullOrEmpty(title))
                        title = Property.displayName;
                
                    return title;
                }
                
                return Property.displayName;
            }));
            UpdateTitle();

            if (refType == null)
            {
                var type = BlueprintLinkDrawer.GetElementType(fieldInfo?.FieldType) ?? fieldInfo?.FieldType;
                
                if (type is { BaseType: { IsGenericType: true } })
                    refType = type.BaseType.GetGenericArguments()[0];
                else if (type is { IsGenericType: true })
                    refType = type.GetGenericArguments()[0];
            }

            m_Field = new ReferenceField(string.Empty)
            {
                value = guid,
                ObjectType = refType,
                style = { flexShrink = new StyleFloat(1), flexGrow = new StyleFloat(1) },
                CopyPasteProperty = Property
            };
            
            m_Field.RegisterValueChangedCallback(e =>
            {
                if (e.target == m_Field)
                {
                    bool canChange = CanChangeValue?.Invoke(e.newValue) ?? true;
                    string value = canChange ? e.newValue : e.previousValue;

                    if (!canChange)
                    {
                        m_Field.SetValueWithoutNotify(value);
                    }
                    else
                    {
                        m_GuidProp.Property.stringValue = value;
                        m_GuidProp.Property.serializedObject.ApplyModifiedProperties();
                        OnValueChangedEvent?.Invoke();
                    }
                    UpdateButtonsDisplay();
                }
            });

            UpdateButtonsDisplay();
            
            FieldLayout = new VisualElement
                { 
                    name = "PropertyFieldLayout", 
                    style = 
                    { 
                        flexDirection = FlexDirection.Row, 
                        flexGrow = 1 
                    } 
                };

            FieldLayout.Add(m_Field);
            FieldLayout.Add(m_ExpandButton);
            FieldLayout.Add(m_ClearButton);
            FieldLayout.Add(m_NewButton);
            
            m_Field.AddManipulator(new ContextualMenuManipulator(e => 
                OwlcatDescriptionButton.CopyPasteContextMenu(e, Property, null, false)));

            if (m_Inline)
            {
                HideArrow(string.IsNullOrEmpty(guid));
                HeaderContentContainer.Add(FieldLayout);
                HeaderAsPropertyLayout = true;
                OverridableControl?.AddAdditional(FieldLayout);
            }
            else
            {
                ContentContainer.Add(FieldLayout);
            }

            CreateContentManipulator();
            ContentContainer.style.flexGrow = 1;

            AddComponent(new CleanHandler(this));
            
            this.TrackPropertyValue(m_GuidProp.Property, _ =>
            {
                m_Field.SetValueWithoutNotify(m_GuidProp.Property.stringValue);
                UpdateButtonsDisplay();
                CreateInlineContent();
                GetComponent<NotNullComponent>()?.Update();
                UpdateTitle();
            });
        }
        
        private void UpdateButtonsDisplay()
        {
            m_ExpandButton.style.display = string.IsNullOrEmpty(m_GuidProp.Property.stringValue) ? 
                DisplayStyle.None : DisplayStyle.Flex;
            m_ClearButton.style.display = string.IsNullOrEmpty(m_GuidProp.Property.stringValue) ? 
                DisplayStyle.None : DisplayStyle.Flex;
            m_NewButton.style.display = string.IsNullOrEmpty(m_GuidProp.Property.stringValue) ? 
                DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnClearButtonClicked()
        {
            m_GuidProp.Property.stringValue = string.Empty;
            m_GuidProp.serializedObject.ApplyModifiedProperties();
            m_Field.value = string.Empty;
        }

        private void CreateContentManipulator()
        {
            this.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                bool hasSeparator = m_Field.Blueprint != null || m_IsHaveNewButton;
                
                if (hasSeparator)
                    evt.TryAddSeparator();

                if (m_Field.Blueprint != null)
                {
                    evt.menu.AppendAction("Select on Project", x => SelectObject());
                    evt.menu.AppendAction("Open on other Window", x => OpenOnOtherWindow());
                }

                if (m_IsHaveNewButton)
                {
                    evt.menu.AppendAction("New", x =>
                    {
                        if (CreateNewCustom != null)
                            CreateNewCustom();
                        else if (m_IsNicolayButton)
                            NicolayCreationLogic(m_DefaultCreatedName);
                        else
                            BaseCreationLogic(m_DefaultCreationPath, m_DefaultCreatedName);
                    });
                }
            }));
        }
        
        /// <summary>
        /// This is for creating small fixed-width buttons like 'new' and 'clear'
        /// in a unified style for different properties
        /// TODO: Make unified base property for blueprint and unity references with 'new' and 'clear' buttons
        /// </summary>
        public static OwlcatVisualElement CreateSmallTextButton(string label, Action onClicked, float width = 44)
        {
            return new OwlcatRegularButton(label, onClicked)
            {
                style =
                {
                    flexGrow = 0,
                    flexShrink = 0,
                    width = width
                }
            };
        }

        private VisualElement CreateExpandButton()
        {
            var btn = new Button
            {
                text = string.Empty, 
                style =
                {
                    paddingLeft = 1, 
                    paddingRight = 1,
                    backgroundColor = new Color(0.76f, 0.76f, 0.76f, 1)
                }
            };
            btn.clicked += OpenOnOtherWindow;

            var img = new Image { image = UIElementsResources.NewWindowIcon, scaleMode = ScaleMode.ScaleToFit };
            btn.Add(img);

            return btn;
        }

        private VisualElement CreateNewButton(FieldInfo fieldInfo)
        {
            VisualElement element = default;
            CheckNewButton(fieldInfo);
            if (m_IsHaveNewButton)
            {
                if (Property != null)
                {
                    m_DefaultCreatedName =
                        TextTemplates.ReplacePropertyNames(m_DefaultCreatedName ?? "", Property.serializedObject);
                    m_DefaultCreationPath =
                        TextTemplates.ReplacePropertyNames(m_DefaultCreationPath ?? "", Property.serializedObject);
                }

                Action onClicked;
                if (CreateNewCustom != null)
                    onClicked = CreateNewCustom;
                else if (m_IsNicolayButton)
                    onClicked = () => NicolayCreationLogic(m_DefaultCreatedName);
                else
                    onClicked = () => BaseCreationLogic(m_DefaultCreationPath, m_DefaultCreatedName);

                element = CreateSmallTextButton("new", onClicked);
            }

            return element ?? new VisualElement();
        }

        private void CheckNewButton(FieldInfo fieldInfo)
        {
            m_DefaultCreationPath = default;
            m_DefaultCreatedName = default;

            var createPathAttribute =
                fieldInfo.GetCustomAttributes(typeof(CreatePathAttribute), true)
                    .FirstOrDefault() as CreatePathAttribute
                ?? fieldInfo.FieldType.GetCustomAttributes(typeof(CreatePathAttribute), true)
                    .FirstOrDefault() as CreatePathAttribute;

            m_DefaultCreationPath = m_DefaultCreationPath ?? createPathAttribute?.Path;

            var createNameAttribute =
                fieldInfo.GetCustomAttributes(typeof(CreateNameAttribute), true)
                    .FirstOrDefault() as CreateNameAttribute
                ?? fieldInfo.FieldType.GetCustomAttributes(typeof(CreateNameAttribute), true)
                    .FirstOrDefault() as CreateNameAttribute;

            m_DefaultCreatedName = m_DefaultCreatedName ?? createNameAttribute?.Name;
            m_IsNicolayButton = fieldInfo.FieldType.HasAttribute<ShowCreatorAttribute>() ||
                                fieldInfo.HasAttribute<ShowCreatorAttribute>() ||
                                CreateNewCustom != null;

            m_IsHaveNewButton = m_DefaultCreationPath != null || m_IsNicolayButton;
        }

        private void NicolayCreationLogic(string defaultCreatedName)
        {
            var creator = CreatorPicker.GetCreatorForType(m_Field.ObjectType);
            if (creator)
            {
                creator.SetRootObject(Property.serializedObject.targetObject);
                NewAssetWindow.ShowWindow(creator, defaultCreatedName, created =>
                {
                    string guid = (created as BlueprintScriptableObject)?.AssetGuid;
                    m_Field.value = guid;
                    m_GuidProp.Property.stringValue = guid;
                    Property.serializedObject.ApplyModifiedProperties();
                });
            }
        }

        private void BaseCreationLogic(string defaultCreationPath, string defaultCreatedName)
        {
            var created =
                BlueprintLinkDrawer.CreateAsset(m_Field.ObjectType, defaultCreationPath, defaultCreatedName);
            m_Field.value = created.AssetGuid;
            m_GuidProp.Property.stringValue = (created as BlueprintScriptableObject)?.AssetGuid;
            created.Reset();
            Property.serializedObject.ApplyModifiedProperties();
        }

        private void OpenOnOtherWindow()
        {
            if (m_Field.Blueprint != null)
            {
                BlueprintInspectorWindow.OpenFor(m_Field.Blueprint);
            }
        }

        private void SelectObject()
        {
            if (m_Field.value != null)
            {
                Selection.activeObject = BlueprintEditorWrapper.Wrap(m_Field.Blueprint);
            }
        }

        private class CleanHandler : OwlcatPropertyComponent, IOwlcatPropertyInputHandler
        {
            private readonly BlueprintReferenceProperty m_Owner;

            int IOwlcatPropertyInputHandler.Order { get; } = 0;

            public CleanHandler(BlueprintReferenceProperty owner)
                => m_Owner = owner;

            void IOwlcatPropertyInputHandler.TryHandle(KeyDownEvent evt)
            {
                if (evt.keyCode == KeyCode.Delete && !string.IsNullOrEmpty(m_Owner.m_GuidProp.Property.stringValue))
                {
                    var oldGuid = m_Owner.m_GuidProp.Property.stringValue;
                    UndoManager.Instance.RegisterUndo($"Revert {m_Owner.m_GuidProp.Property.name} clear", () =>
                    {
                        m_Owner.m_GuidProp.Property.stringValue = oldGuid;
                        m_Owner.m_Field.value = oldGuid;
                        m_Owner.Property.serializedObject.ApplyModifiedProperties();
                    });

                    m_Owner.m_Field.value = default;
                    m_Owner.m_GuidProp.Property.stringValue = string.Empty;
                    m_Owner.Property.serializedObject.ApplyModifiedProperties();

                    evt.StopPropagation();
                }
            }
        }
    }
}
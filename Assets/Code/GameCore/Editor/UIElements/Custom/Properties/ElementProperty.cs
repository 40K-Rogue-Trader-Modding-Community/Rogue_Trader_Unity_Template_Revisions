using Kingmaker.Code.Editor.Utility;
using Kingmaker.Editor.Elements;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Elements;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Mechanics.Actions;
using System.Reflection;
using Code.Editor.Utility;
using Code.Framework.Editor.Utility;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Editor.UIElements.Custom.Array;
using Kingmaker.Editor.UIElements.Custom.PropertyComponents;
using Kingmaker.Editor.UIElements.ValuePicker;
using Kingmaker.Editor.Utility;
using Kingmaker.ElementsSystem.Interfaces;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Kingmaker.Utility.UnityExtensions;

namespace Kingmaker.Editor.UIElements.Custom
{
    public class ElementProperty : OwlcatProperty
    {
        #region Constructor

        public ElementProperty(SerializedProperty property, FieldInfo fieldInfo) : base(property, Layout.Vertical)
        {
            AddCreateButton();
            if (property.HasManagedReference())
            {
                CreateFillPart();
            }
            else
            {
                ContentContainer.style.display = DisplayStyle.None;
                HideArrow(true);
            }

            AddComponent(new ElementTitleProvider());
        }

        protected override void OnAttachToPanelInternal(AttachToPanelEvent evt)
        {
            base.OnAttachToPanelInternal(evt);
            m_TooltipManipulator.ChangeGetter(GetTooltip);
            
            if (parent is not OwlcatListViewEntryWrapper)
            {
                CreateRemoveBtn();
                
                //Requested by game designers. Array elements can not be replaced by drag and drop.
                CreateFactoryComponent();
                
                HeaderContainer.AddManipulator(new ContextualMenuManipulator(e =>
                {
                    if (Property.HasManagedReference())
                    {
                        e.TryAddSeparator();
                        e.menu.AppendAction($"Remove {GetElement().GetType().Name}", x => RemoveContent(true));
                    }
                }));
            }
            else
            {
                RemoveComponent<FactoryDragAndDropComponent>();
            }
        }
        
        protected override void OnAfterAttachToPanelInternal()
        {
            base.OnAfterAttachToPanelInternal();
            
            if (DescriptionButton != null)
                DescriptionButton.PastePostProcess = Recreate;
        }

        public void Recreate()
        {
            bool wasExpanded = IsExpanded;
            
            if (m_ContentRoot != null)
                RemoveContent(false);

            if (Property.HasManagedReference())
                CreateFillPart();
            
            IsExpanded = wasExpanded;
        }

        private void CreateRemoveBtn()
        {
            m_RemoveBtn = new OwlcatSmallButton { text = "X" };
            m_RemoveBtn.clicked += () => RemoveContent(true);
            m_RemoveBtn.AddToClassList("red-button");

            ControlsContainer.Add(m_RemoveBtn);
            m_RemoveBtn.style.display = !Property.HasManagedReference() ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void CreateFactoryComponent()
        {
            if (parent is OwlcatListViewEntryWrapper)
                return;
            
            var type = FieldFromProperty.GetDeclaredType(Property);
            AddComponent(new FactoryDragAndDropComponent(HeaderContainer, Property, 
                type, CreateFillPart, () => RemoveContent(true)));
        }

        #endregion Constructor
        #region Fiedls

        VisualElement m_ContentRoot;

        VisualElement m_CreateBtn;

        VisualElement m_ControlsBtn;

        VisualElement m_Mark;

        OwlcatSmallButton m_RemoveBtn;

        #endregion Fields
        #region Methods

        private void AddCreateButton()
        {
            var type = FieldFromProperty.GetDeclaredType(Property);
            m_CreateBtn = TypePicker.CreatePickerButton(
                "Create",
                () => TypeUtility.CollectValuesWithFilter(Property, type),
                sType =>
                {
                    TypeUtility.AddElementFromMenu(Property, sType);
                    CreateFillPart();
                });

            ControlsContainer.Add(m_CreateBtn);
        }

        private void RemoveContent(bool removeReference)
        {
            if (removeReference)
            {
                var element = GetElement();
                var serializedObject = Property.serializedObject;
                
                if (element != null)
                {
                    element.Delete();
                    serializedObject.Update();
                }
                
                Property.managedReferenceValue = null;
                serializedObject.ApplyModifiedProperties();
            }
            
            if (m_ContentRoot != null)
                ContentContainer.RemoveIfContains(m_ContentRoot);

            if (m_ControlsBtn != null)
                ControlsContainer.RemoveIfContains(m_ControlsBtn);
            
            if (m_Mark != null)
                HeaderContainer.RemoveIfContains(m_Mark);

            m_ControlsBtn = default;
            m_Mark = default;
            m_ContentRoot = default;
            m_CreateBtn.style.display = DisplayStyle.Flex;
            
            if (m_RemoveBtn != null)
                m_RemoveBtn.style.display = DisplayStyle.None;
            
            HideArrow(true);

            if (TitleLabel != null)
                TitleLabel.text = Property.displayName;

            RemoveComponent<FactoryDragAndDropComponent>();
            DescriptionButton?.UpdateView();
        }
        
        private Element GetElement()
        {
            return FieldFromProperty.GetFieldValue(Property) as Element;
        }

        private void CreateFillPart()
        {
            m_CreateBtn.style.display = DisplayStyle.None;
            if (m_RemoveBtn != null)
            {
                m_RemoveBtn.style.display = DisplayStyle.Flex;
            }

            m_ContentRoot = new VisualElement { name = "ElementContent" };

            string title = Property.displayName;

            var element = GetElement();
            if (element is IHaveCaption caption)
            {
                try
                {
                    title = element.GetCaptionSafe();
                }
                catch (System.Exception x)
                {
                    PFLog.Default.Exception(x);
                    title = x.Message;
                }
            }

            TitleLabel.text = title;
            m_ControlsBtn = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
            CreateConditionalOperator(Property);
            CreateGuid(element);
            CreateContextMark(element);
            CreateEditor();
            CreateFactoryComponent();

            ContentContainer.Add(m_ContentRoot);
            ControlsContainer.Insert(0, m_ControlsBtn);
            
            GetComponent<NotNullComponent>()?.Update();
            DescriptionButton?.UpdateView();
        }

        private void CreateGuid(Element element)
        {
            string guid = element.AssetGuidShort;
            if (!guid.IsNullOrEmpty())
            {
                var guidLabel = new Label(guid);
                guidLabel.style.width = 40;
                guidLabel.style.color = Color.black;
                guidLabel.style.marginTop = 1;
                guidLabel.style.marginBottom = 1;
                guidLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                guidLabel.style.backgroundColor = new StyleColor(new Color(150 / 255f, 150 / 255f, 150 / 255f));
                m_ControlsBtn.Add(guidLabel);
            }
        }

        private void CreateConditionalOperator(SerializedProperty prop)
        {
            var propNot = prop.FindPropertyRelative("Not");
            if (propNot != null)
            {
                var elementNot = new Toggle
                { 
                    text = "Not", 
                    value = propNot.boolValue, 
                    style = 
                    { 
                        flexShrink = 1, 
                        flexGrow = 0, 
                        marginRight = 3
                    } 
                };
                
                elementNot.BindProperty(propNot);
                m_ControlsBtn.Add(elementNot);
                elementNot.TrackPropertyValue(propNot, _ =>
                {
                    UpdateTitle();
                    GetComponent<ElementTitleProvider>()?.SetResultAndColor();
                });
            }
        }

        private void CreateContextMark(Element element)
        {
            string mark = element is ContextAction ? "[C]" : null;
            if (mark != null)
            {
                m_Mark = new Label(mark);
                HeaderContainer.Add(m_Mark);
            }
        }

        private void CreateEditor()
        {
            var editor = new OwlcatInspectorRoot(Property);
            
            if (editor.childCount == 0)
            {
                editor.contentContainer.style.display = DisplayStyle.None;
                ContentContainer.style.display = DisplayStyle.None;
                HideArrow(true);
            }
            else
            {
                HideArrow(false);
                UpdateExpandableVisibility();
            }

            m_ContentRoot.Add(editor);
        }

        private string GetTooltip()
        {
            var element = GetElement();
            string result = string.Empty;
            
            if (element is IHaveDescription description)
                result = element.GetDescriptionSafe();
            
            return result;
        }
    }

    #endregion Methods
}
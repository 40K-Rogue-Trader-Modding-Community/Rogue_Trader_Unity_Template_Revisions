using Code.Editor.Utility;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Elements;
using Kingmaker.Editor.UIElements.Custom.Properties;
using Kingmaker.Editor.Utility;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Array
{
    public class OwlcatListViewEntryWrapper : VisualElement
    {
        private readonly OwlcatListViewProperty m_ListView;
        
        private VisualElement m_CurrentElement;
        private int m_Index;
        private ContextualMenuManipulator m_ContextMenu;
        private SerializedProperty m_Property;
        private VisualElement m_DeleteButton;

        public OwlcatListViewEntryWrapper(OwlcatListViewProperty list)
        {
            m_ListView = list;
            m_ContextMenu = new ContextualMenuManipulator(ContextMenu);
            style.flexDirection = FlexDirection.Row;
        }

        private void ContextMenu(ContextualMenuPopulateEvent evt)
        {
            evt.TryRemoveItem("Delete Array Element");

            if (!evt.TryRemoveItem("Duplicate Array Element"))
                evt.TryAddSeparator();
            
            evt.menu.AppendAction("Delete Selected Array Elements", x =>
            {
                if (!m_ListView.TryRemoveSelected())
                    m_ListView.RemoveItemAt(m_Index);
            });
            
            //Not needed, OwlcatDescriptionButton do the trick
            // if (m_ListView.ArrayProperty.Property.propertyType == SerializedPropertyType.Generic)
            // {
            //     evt.menu.AppendAction("Copy Array Element", x =>
            //     {
            //         PrototypedObjectEditorUtility.CopyArrayElement(m_ListView.ArrayProperty, m_Index);
            //     });
            //
            //     evt.menu.AppendAction("Paste Array Element", x =>
            //     {
            //         m_ListView.RemoveFromCache(m_Index);
            //         PrototypedObjectEditorUtility.PasteArrayElement(m_ListView.ArrayProperty, m_Index);
            //         CopyPasteController.FixNestedPropertiesAfterPaste(m_Property);
            //         m_ListView.RefreshItemAt(m_Index);
            //     });
            // }
            
            evt.StopPropagation();
        }
        
        public void Bind(SerializedProperty property, int index)
        {
            m_Property = new RobustSerializedProperty(property);
            m_Index = index;
            if (property == null)
                return;

            var element = m_ListView.GetFromCache(index);
            if (element == null)
            {
                element = UIElementsUtility.CreatePropertyElement(property, true);
                if (element is OwlcatProperty owlcatProperty)
                {
                    if (property.isExpanded)
                        owlcatProperty.IsExpanded = true;
                    
                    // Сдвиг для кнопки удаления, т.к. она в абсолютной позиции, чтобы не сжирать доп. место справа в списке
                    // см. WH-454998
                    owlcatProperty.ControlsContainer.style.paddingRight = 22;
                }

                m_ListView.AddToCache(index, element);
            }
            else
            {
                element.Bind(property.serializedObject);
                // foreach (var bpRef in element.Query<BlueprintReferenceProperty>().Build())
                    // bpRef.Rebind();  // AMB-539483
            }

            if (m_DeleteButton == null)
            {
                m_DeleteButton = new OwlcatSmallButton(() => { m_ListView.RemoveItemAt(m_Index); })
                {
                    text = "X",
                    style = 
                        {
                            position = Position.Absolute,
                            right = 0,
                            top = 0
                        }
                };
                m_DeleteButton.AddToClassList("red-button");
            }

            m_CurrentElement = element;
            Add(element);
            Add(m_DeleteButton);
            
            parent.parent.RegisterCallback<MouseDownEvent>(OnClick);
            parent.parent.AddManipulator(m_ContextMenu);
            
            if (m_ListView.MovedIndex >= 0 && m_ListView.MovedIndex == index)
            {
                EditorApplication.delayCall += () => (element as OwlcatProperty)?.TitleLabel?.Focus();
                m_ListView.MovedIndex = -1;
            }
        }

        public void Unbind()
        {
            m_CurrentElement?.RemoveFromHierarchy();
            parent.parent.UnregisterCallback<MouseDownEvent>(OnClick);
            parent.parent.RemoveManipulator(m_ContextMenu);
        }

        private void OnClick(MouseDownEvent evt)
        {
            if (OwlcatListViewProperty.ClickedList == null)
                m_ListView.SetClicked();
            else
                evt.StopPropagation();
        }
    }
}
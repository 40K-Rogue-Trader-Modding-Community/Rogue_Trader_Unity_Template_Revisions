using System;
using Code.Editor.KnowledgeDatabase;
using Code.Editor.KnowledgeDatabase.Inspector;
using Code.Editor.Utility;
using JetBrains.Annotations;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.Blueprints.Elements;
using Kingmaker.Editor.Elements;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Prototypable;
using Kingmaker.Editor.Utility;
using Kingmaker.ElementsSystem;
using Kingmaker.ElementsSystem.Interfaces;
using Kingmaker.Utility.DotNetExtensions;
using UnityEngine;
using UnityEngine.UIElements;
using Kingmaker.Utility.UnityExtensions;
using UnityEditor;
using SerializedPropertyHelper = Kingmaker.Editor.Elements.SerializedPropertyHelper;

namespace Kingmaker.Editor.UIElements.Custom.Elements
{
    public class OwlcatDescriptionButton : Button
    {
        private static Color OnlyTooltipColor = new (0.784f, 0.949f, 0.533f);
        
        private readonly RobustSerializedProperty m_Property;
        private readonly OverridablePropertyControl m_OverridablePropertyControl;
        private readonly OwlcatPropertyLayout m_Layout;
        private TooltipManipulator m_TooltipManipulator;

        public Action PastePostProcess;

        public OwlcatDescriptionButton(RobustSerializedProperty property,
            OverridablePropertyControl overridablePropertyControl, 
            [NotNull] OwlcatPropertyLayout layout)
        {
            m_Property = property;
            name = m_Property.Property.propertyPath;
            m_OverridablePropertyControl = overridablePropertyControl;
            m_Layout = layout;
            style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(
                new Length(80, LengthUnit.Percent),
                new Length(80, LengthUnit.Percent)));

            AddToClassList("owlcat-description-button");
            
            UpdateView();

            RegisterCallback<AttachToPanelEvent>(AttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(DetachFromPanel);
            m_Layout?.ContextMenuHeader.AddManipulator(new ContextualMenuManipulator(PopulateContextMenu));

            var manipulator = new ContextualMenuManipulator(PopulateContextMenu);
            manipulator.activators.Add(new ManipulatorActivationFilter() {button = MouseButton.LeftMouse});
            this.AddManipulator(manipulator);
        }

        private void PopulateContextMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target == this && evt.currentTarget == m_Layout.ContextMenuHeader)
                return;

            var target = m_Property.serializedObject.targetObject;
            if (target is not (BlueprintEditorWrapper or BlueprintComponentEditorWrapper or MonoBehaviour))
                return;

            bool hasOverrideOption = m_OverridablePropertyControl?.HasOverrideOption() ?? false;

            bool drawSeparator = true;
            if (hasOverrideOption && !SerializedPropertyHelper.IsArrayElement(m_Property))
            {
                evt.TryAddSeparator();
                drawSeparator = false;
                bool overridden = m_OverridablePropertyControl.IsOverridden();
                evt.menu.AppendAction(
                    overridden ? "Revert" : "Override",
                    _ => m_OverridablePropertyControl.SwitchOverridenState());
            }

            if (m_Layout.Expandable)
            {
                evt.TryAddSeparator();
                evt.menu.AppendAction("Expand All", _ => { m_Property.Property.ExpandCollapseAll(m_Layout, true); });
                evt.menu.AppendAction("Collapse All", _ => { m_Property.Property.ExpandCollapseAll(m_Layout, false); });
            }

            var type = CopyPasteController.GetPasteableType(m_Property);
            if (type != null)
                CopyPasteContextMenu(evt, m_Property, PastePostProcess, drawSeparator);

            type = FieldFromProperty.GetActualValueType(m_Property);
            if (type != null)
            {
                DropdownMenuAction.Status status = Application.isPlaying ? 
                    DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
                
                if (type.IsSubclassOf(typeof(Condition)))
                {
                    evt.menu.AppendAction("Evaluate", _ =>
                        {
                            PrototypedObjectEditorUtility.DebugCondition(
                                FieldFromProperty.GetFieldValue(m_Property) as Condition);
                        }, status);
                }
                else if (type.GetInterfaces().FirstOrDefault(v => 
                             v.IsGenericType && v.GetGenericTypeDefinition() == typeof(IEvaluator<>)) is {} iFace)
                {
                    evt.menu.AppendAction("Evaluate", _ =>
                        {
                            PrototypedObjectEditorUtility.DebugEvaluator(iFace,
                                FieldFromProperty.GetFieldValue(m_Property) as Element);
                        }, status);
                }
            }

            AddKnowledgeDatabaseMenuItems(evt);
        }

        private void AddKnowledgeDatabaseMenuItems(ContextualMenuPopulateEvent evt)
        {
            (var propertyType, string propertyFieldName) = m_Property.Property.GetTypeAndName(false);
            (var elementType, string elementFieldName) = m_Property.Property.GetTypeAndName(true);

            AddDescriptionMenuItems(evt, propertyType, propertyFieldName, elementType, elementFieldName);
            AddLinkMenuItems(evt, propertyType, propertyFieldName, elementType, elementFieldName);
        }

        private static void AddDescriptionMenuItems(ContextualMenuPopulateEvent evt,
            Type propertyType, string propertyFieldName,
            Type elementType, string elementFieldName)
        {
            string propertyDescription = KnowledgeDatabaseSearch.GetDescription(propertyType, propertyFieldName);
            string elementDescription = KnowledgeDatabaseSearch.GetDescription(elementType, elementFieldName);
            string codePropertyDescription = KnowledgeDatabaseSearch.GetCodeDescription(propertyType, propertyFieldName);
            string codeElementDescription = KnowledgeDatabaseSearch.GetCodeDescription(elementType, elementFieldName);

            bool hasAnyDescription = propertyDescription != null || elementDescription != null ||
                                    codePropertyDescription != null || codeElementDescription != null;

            if (!hasAnyDescription)
                return;

            if (propertyType != elementType)
            {
                if (propertyDescription != null && elementDescription != null)
                {
                    evt.menu.AppendAction("Description (field)", _ =>
                        KnowledgeDatabaseEditWindow.Show(propertyType, propertyFieldName));
                    evt.menu.AppendAction("Description (element)", _ =>
                        KnowledgeDatabaseEditWindow.Show(elementType, elementFieldName));
                }
                else if (propertyDescription != null)
                {
                    evt.menu.AppendAction("Description", _ =>
                        KnowledgeDatabaseEditWindow.Show(propertyType, propertyFieldName));
                }
                else
                {
                    evt.menu.AppendAction("Description", _ =>
                        KnowledgeDatabaseEditWindow.Show(elementType, elementFieldName));
                }
            }
            else
            {
                evt.menu.AppendAction("Description", _ =>
                    KnowledgeDatabaseEditWindow.Show(propertyType, propertyFieldName));
            }
        }

        private static void AddLinkMenuItems(ContextualMenuPopulateEvent evt,
            Type propertyType, string propertyFieldName,
            Type elementType, string elementFieldName)
        {
            bool propertyHasLink = KnowledgeDatabaseSearch.HasLink(propertyType, propertyFieldName);
            bool elementHasLink = KnowledgeDatabaseSearch.HasLink(elementType, elementFieldName);

            if (!propertyHasLink && !elementHasLink)
                return;

            if (propertyType != elementType)
            {
                if (propertyHasLink && elementHasLink)
                {
                    evt.menu.AppendAction("Link (field)", _ =>
                        KnowledgeDatabaseSearch.GoTo(KnowledgeDatabaseSearch.GetLink(propertyType, propertyFieldName)));
                    evt.menu.AppendAction("Link (element)", _ =>
                        KnowledgeDatabaseSearch.GoTo(KnowledgeDatabaseSearch.GetLink(elementType, elementFieldName)));
                }
                else if (propertyHasLink)
                {
                    evt.menu.AppendAction("Link", _ =>
                        KnowledgeDatabaseSearch.GoTo(KnowledgeDatabaseSearch.GetLink(propertyType, propertyFieldName)));
                }
                else
                {
                    evt.menu.AppendAction("Link", _ =>
                        KnowledgeDatabaseSearch.GoTo(KnowledgeDatabaseSearch.GetLink(elementType, elementFieldName)));
                }
            }
            else
            {
                evt.menu.AppendAction("Link", _ =>
                    KnowledgeDatabaseSearch.GoTo(KnowledgeDatabaseSearch.GetLink(propertyType, propertyFieldName)));
            }
        }
        
        public static void CopyPasteContextMenu(ContextualMenuPopulateEvent evt, SerializedProperty property, Action onPaste, bool drawSeparator)
        {
            var type = CopyPasteController.GetPasteableType(property);
 
            if (type != null)
            {
                if (drawSeparator)
                    evt.TryAddSeparator();
 
                evt.menu.AppendAction("Copy (Owlcat)", _ => { CopyPasteController.CopyProperty(property, null); });
 
                string pasteType = CopyPasteController.ClipboardElements.Count > 1 ? 
                    CopyPasteController.ClipboardElements.Count + " items" : 
                    CopyPasteController.ClipboardElements.FirstItem()?.Type.Name;
 
                if (pasteType != null)
                    pasteType = $" ({pasteType})";
 
                DropdownMenuAction.Status pasteStatus = CopyPasteController.IsSuitableForPaste(type) ? 
                    DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
 
                evt.menu.AppendAction("Paste" + pasteType, _ =>
                    {
                        if (CopyPasteController.PasteProperty(type, property))
                        {
                            property.serializedObject.ApplyModifiedProperties();
                            property.serializedObject.Update();
                            onPaste?.Invoke();
                        }
                    }, pasteStatus);
            }
        }

        private string GetTooltip()
        {
            if (m_Property?.Property == null)
                return string.Empty;

            string propertyDescription = KnowledgeDatabaseSearch.GetDescription(m_Property, false);
            string elementDescription = KnowledgeDatabaseSearch.GetDescription(m_Property, true);
            string codePropertyDescription = KnowledgeDatabaseSearch.GetCodeDescription(m_Property, false);
            string codeElementDescription = KnowledgeDatabaseSearch.GetCodeDescription(m_Property, true);
            bool isObsolete = PrototypedObjectEditorUtility.IsObsolete(m_Property, out string obsoleteMessage);
            
            bool hasOvertipData = !propertyDescription.IsNullOrEmpty() || !elementDescription.IsNullOrEmpty() ||
                                  !codePropertyDescription.IsNullOrEmpty() || !codeElementDescription.IsNullOrEmpty() ||
                                  !m_Property.Property.GetTooltip().IsNullOrEmpty() || isObsolete;

            if (!hasOvertipData) 
                return string.Empty;
            
            using var psb = PooledStringBuilder.Request();
            var sb = psb.Builder;
                
            PrototypedObjectEditorUtility.TryGetFullDescription(codePropertyDescription, propertyDescription, isObsolete,
                obsoleteMessage, out string fullDescription);
            sb.Append(fullDescription);
                
            if (propertyDescription != elementDescription && !elementDescription.IsNullOrEmpty())
            {
                if (sb.Length > 0)
                    sb.Append("\n\n");

                sb.Append("Element description:\n");
                PrototypedObjectEditorUtility.TryGetFullDescription(codeElementDescription, elementDescription, isObsolete,
                    obsoleteMessage, out fullDescription);
                sb.Append(fullDescription);
            }

            string hardcodedTooltip = m_Property.Property.GetTooltip();

            if (!hardcodedTooltip.IsNullOrEmpty())
            {
                if (sb.Length > 0)
                    sb.Append("\n---\n");
                    
                sb.Append(hardcodedTooltip);
            }

            return sb.ToString();
        }

        private void AttachToPanel(AttachToPanelEvent evt)
        {
            KnowledgeDatabaseSearch.OnDescriptionUpdated += OnDescriptionChanged;

            if (m_TooltipManipulator == null)
            {
                m_TooltipManipulator = new TooltipManipulator(GetTooltip);
                this.AddManipulator(m_TooltipManipulator);
            }
        }

        private void DetachFromPanel(DetachFromPanelEvent evt)
        {
            KnowledgeDatabaseSearch.OnDescriptionUpdated -= OnDescriptionChanged;
        }

        private void OnDescriptionChanged(Type type, string fieldName)
        {
            UpdateView();
        }

        public void UpdateView()
        {
            (var propertyType, string propertyFieldName) = m_Property.Property.GetTypeAndName(false);
            (var elementType, string elementFieldName) = m_Property.Property.GetTypeAndName(true);
            
            string propertyDescription = KnowledgeDatabaseSearch.GetDescription(propertyType, propertyFieldName);
            string elementDescription = KnowledgeDatabaseSearch.GetDescription(elementType, elementFieldName);
            string codePropertyDescription = KnowledgeDatabaseSearch.GetCodeDescription(propertyType, propertyFieldName);
            string codeElementDescription = KnowledgeDatabaseSearch.GetCodeDescription(elementType, elementFieldName);
            bool propertyHasLink = KnowledgeDatabaseSearch.HasLink(propertyType, propertyFieldName);
            bool elementHasLink = KnowledgeDatabaseSearch.HasLink(elementType, elementFieldName);
            bool isObsolete = PrototypedObjectEditorUtility.IsObsolete(m_Property, out _);
            string propertyTooltip = m_Property.Property.GetTooltip();
            
            bool hasDatabaseDescription = !propertyDescription.IsNullOrEmpty() || !elementDescription.IsNullOrEmpty() || 
                                          !codePropertyDescription.IsNullOrEmpty() || !codeElementDescription.IsNullOrEmpty();

            bool hasDatabaseLink = propertyHasLink || elementHasLink;

            if (isObsolete)
                style.unityBackgroundImageTintColor = Color.red;
            else if (hasDatabaseDescription)
                style.unityBackgroundImageTintColor = Color.green;
            else if (hasDatabaseLink)
                style.unityBackgroundImageTintColor = Color.cyan;
            else if (!propertyTooltip.IsNullOrEmpty())
                style.unityBackgroundImageTintColor = OnlyTooltipColor;
            else
                style.unityBackgroundImageTintColor = Color.white;
        }
    }
}
using Kingmaker.Editor.UIElements.Custom;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.Blueprints;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Properties
{
    public partial class BlueprintReferenceProperty
    {
        private readonly bool m_Inline;
                
        private OwlcatContentContainer m_InlineContent;
        private VisualElement m_InlineBlueprintError;
        
        private const string m_CycleError = "Blueprint is already inlined! Will not draw.";
        private const string m_RootError = "Inline Blueprints are supported only in OwlcatInspectorRoot.";

        protected override void OnAfterAttachToPanelInternal()
        {
            base.OnAfterAttachToPanelInternal();
            if (!m_Inline)
                return;

            m_ContentCreated = true;
            
            if (m_InlineContent == null)
                CreateInlineContent();
        }

        private void CreateInlineContent()
        {
            if (!m_Inline)
                return;
            
            if (m_InlineContent == null)
            {
                m_InlineContent = new OwlcatContentContainer();
                m_InlineBlueprintError = new HelpBox("", HelpBoxMessageType.Error);
                ContentContainer.Add(m_InlineContent);
                ContentContainer.AddToClassList("owlcat-inline-editor");
            }
            
            var root = GetUpperRoot();
            m_InlineContent.Clear();
            
            if (root == null)
            {
                m_InlineContent.Add(m_InlineBlueprintError);
                m_InlineBlueprintError.Q<Label>().text = m_RootError;
                return;
            }
            
            if (root.InlinedBlueprints.Contains(m_Field.Blueprint))
            {
                m_InlineContent.Add(m_InlineBlueprintError);
                m_InlineBlueprintError.Q<Label>().text = m_CycleError;
                HideArrow(false);
            }
            else
            {
                var blueprint = m_Field.Blueprint;
                var wrapper = BlueprintEditorWrapper.Wrap(blueprint);
                if (wrapper == null)
                {
                    HideArrow(true);
                    return;
                }
                
                HideArrow(false);
                
                bool isInBlueprintRoot = root is BlueprintInspectorRoot;

                var editor = UnityEditor.Editor.CreateEditor(wrapper);
                if (isInBlueprintRoot)
                    (editor as BlueprintWrapperInspector)?.UnsubscribeFromEvents();

                var elementsEditor = editor.CreateInspectorGUI();

                // ToDo new_inspector: remove closure
                elementsEditor.RegisterCallback<AttachToPanelEvent>(_ => { root.InlinedBlueprints.Add(blueprint); });

                var thisBp = Property.serializedObject.targetObject as BlueprintEditorWrapper;
                elementsEditor.RegisterCallback<DetachFromPanelEvent>(_ =>
                {
                    if (thisBp?.WrappedInstance != blueprint)
                        root.InlinedBlueprints.Remove(blueprint);
                });

                m_InlineContent.Add(elementsEditor);
                BlueprintInspectorRoot inspector = elementsEditor as BlueprintInspectorRoot;
                inspector?.SetInline(isInBlueprintRoot);
            }
        }
        
        public void Rebind()
        {
            if (!m_Inline)
                return;

            var blueprint = m_Field.Blueprint;
            var wrapper = BlueprintEditorWrapper.Wrap(blueprint);
            if (wrapper == null)
                return;

            m_InlineContent?.Bind(new SerializedObject(wrapper));
        }
    }
}
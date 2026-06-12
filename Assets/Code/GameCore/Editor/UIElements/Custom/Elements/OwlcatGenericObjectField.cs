
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Kingmaker.Editor.UIElements.Custom.Elements
{
    public class OwlcatGenericObjectField : VisualElement
    {
        private readonly Label m_Label;
        private readonly Button m_SelectButton;
        private readonly Image m_Icon;
        private readonly VisualElement m_Display;
        
        public Label Label => m_Label;
        public Button SelectButton => m_SelectButton;
        public Image Icon => m_Icon;
        public VisualElement Display => m_Display; 
        
        public OwlcatGenericObjectField(Action onPickerClick, EventCallback<MouseDownEvent> onClick, EventCallback<KeyDownEvent> onKeyDown)
        {
            AddToClassList("unity-base-field");
            AddToClassList("unity-object-field");
            AddToClassList("unity-base-field__inspector-field");
            focusable = true;
            pickingMode = PickingMode.Position;
            style.flexGrow = 1;
            style.flexShrink = 1;
            
            var input = new VisualElement();
            input.style.flexGrow = 1;
            input.AddToClassList("unity-base-field__input");
            input.AddToClassList("unity-object-field__input");
            input.pickingMode = PickingMode.Ignore;
            
            m_Display = new VisualElement();
            m_Display.focusable = false;
            m_Display.style.flexDirection = FlexDirection.Row;
            m_Display.style.flexGrow = 1;
            m_Display.AddToClassList("unity-object-field-display");
            m_Display.AddToClassList("unity-object-field__object");
            input.Add(m_Display);
            
            m_Icon = new Image();
            m_Icon.scaleMode = ScaleMode.ScaleAndCrop;
            m_Icon.pickingMode = PickingMode.Ignore;
            m_Icon.AddToClassList("unity-object-field-display__icon");
            m_Icon.style.flexShrink = 0;
            m_Display.Add(m_Icon);

            m_Label = new Label();
            m_Label.pickingMode = PickingMode.Ignore;
            m_Label.AddToClassList("unity-object-field__text");
            m_Label.style.flexGrow = 1;
            m_Label.style.alignSelf = Align.Center;
            m_Display.Add(m_Label);

            m_SelectButton = new Button(onPickerClick);
            m_SelectButton.focusable = false;
            m_SelectButton.RemoveFromClassList("unity-text-element");
            m_SelectButton.RemoveFromClassList("unity-button");
            m_SelectButton.AddToClassList("unity-object-field__selector");
            m_SelectButton.text = "";
            input.Add(m_SelectButton);

            Add(input);
            
            if (onClick != null)
                m_Display.RegisterCallback(onClick);
            
            if (onKeyDown != null)
                RegisterCallback(onKeyDown);
        }

        public void UpdateView(Object value, Type type)
        {
            GUIContent guiContent = EditorGUIUtility.ObjectContent(value, type);
            m_Icon.image = guiContent.image;
            Label.text = guiContent.text;
            Label.EnableInClassList("unity-object-field-display__icon--value-null", value == null);
        }
    }
}
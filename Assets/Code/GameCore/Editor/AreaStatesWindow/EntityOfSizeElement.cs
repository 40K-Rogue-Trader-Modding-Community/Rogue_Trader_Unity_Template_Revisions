﻿using Kingmaker.Editor.UIElements.Custom.Base;
using UnityEngine;
using UnityEngine.UIElements;

#nullable enable

namespace Kingmaker.Editor.AreaStatesWindow
{
    public class EntityOfSizeElement : VisualElement
    {
        private readonly Toggle _toggle;

        private readonly IntegerField _sizeX;
        private readonly IntegerField _sizeY;

        public bool IsActive => _toggle.value;
        public Vector2Int Size => new (_sizeX.value, _sizeY.value);

        public EntityOfSizeElement(string? toggleLabel)
        {
            var content = new OwlcatContentContainer
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 1,
                },
            };

            _toggle = new Toggle(toggleLabel)
            {
                value = toggleLabel == null,
                style = { display = toggleLabel == null ? DisplayStyle.None : DisplayStyle.Flex},
            };

            _sizeX = CreateIntField("X");
            _sizeY = CreateIntField("Y");

            content.Add(_toggle);
            content.Add(_sizeX);
            content.Add(_sizeY);
            Add(content);
        }

        private static IntegerField CreateIntField(string label)
        {
            var field = new IntegerField("X")
            {
                value = 50,
                label = label,
                style = {minWidth = 128}
            };
            field.AddToClassList("unity-base-field__input");
            field.labelElement.style.minWidth = 24;
            field.labelElement.style.flexShrink = 1;
            field.labelElement.style.unityTextAlign = TextAnchor.MiddleRight;
            return field;
        }
    }
}
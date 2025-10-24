using System;
using System.Collections.Generic;
using Kingmaker.Editor.UIElements;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Elements;
using UnityEditor;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Kingmaker.Editor.ValidationErrorsWindow
{
    /// <summary>
    /// This element displays given validation error
    /// </summary>
    public class ValidationErrorElement : OwlcatInspectorStyle
    {
        private const string BaseKey = nameof(ValidationErrorElement);

        private readonly OwlcatInspectorFoldout root;

        private readonly Action<string> OnSkip;
        public ValidationErrorElement(string title, Action<string> onSkip)
        {
            OnSkip = onSkip;
            root = new OwlcatInspectorFoldout($"{BaseKey}.{title}")
            {
                TitleLabel =
                {
                    text = title
                },
            };
            Add(root);
        }

        public void UpdateErrorObjects(Dictionary<Object, string> objectsWithErrors)
        {
            root.ContentContainer.Clear();

            int index = -1; // For zebra-style
            foreach ((var errorObject, string path) in objectsWithErrors)
            {
                index++;
                root.ContentContainer.Add(GetStateElement(errorObject, path, index));
            }
        }

        private VisualElement GetStateElement(Object errorObject, string path, int index)
        {
            var sceneLayout = new OwlcatContentContainer
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    backgroundColor = UIElementsResources.GetZebra(index),
                }
            };
            var text = new TextField
            {
                value = errorObject.name,
                style = {flexGrow = 1},
                tooltip = path,
            };
            text.SetEnabled(false);
            sceneLayout.Add(text);
            sceneLayout.Add(new Button(() => Selection.activeObject = errorObject)
            {
                text = "Select",
                style = {width = 60}
            });
            sceneLayout.Add(new Button(() => OnSkip(path))
            {
                text = "Skip",
                style = {width = 40}
            });

            return sceneLayout;
        }
    }
}
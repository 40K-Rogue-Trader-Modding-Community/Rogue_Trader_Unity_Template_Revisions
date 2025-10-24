using System;
using Kingmaker.Editor.UIElements.Custom.Base;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.AreaStatesWindow
{
    public class NewStateElement : VisualElement
    {
        private readonly TextField _stateName;
        private readonly SceneNameElement _sceneNameElement;
        private readonly Toggle _useSateName;

        private bool isFirstTime = true;

        public NewStateElement(
            string areaName,
            Action<(string stateName, string sceneName, string sceneTemplatePath)> createState,
            Action closeWindow)
        {
            var content = new OwlcatContentContainer
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    flexGrow = 0,
                },
            };

            _stateName = new TextField
            {
                label = "State Name",
                multiline = false,
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Row,
                    marginBottom = 10,
                }
            };
            content.Add(_stateName);

            _useSateName = new Toggle
            {
                label = "Use State Name",
            };
            _useSateName.RegisterValueChangedCallback(UpdateSceneName);
            content.Add(_useSateName);

            _sceneNameElement = new SceneNameElement(string.Empty, areaName);
            content.Add(_sceneNameElement);

            var buttonsLayout = new OwlcatContentContainer
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginTop = 10,
                }
            };
            buttonsLayout.Add(new Button(() =>
            {
                createState((_stateName.value, _sceneNameElement.SceneName, _sceneNameElement.TemplatePath));
                closeWindow();
            })
            {
                text = "Create",
                tooltip = "Will create new etude and scene for the state.",
            });
            buttonsLayout.Add(new Button(closeWindow)
            {
                text = "Cancel",
            });
            content.Add(buttonsLayout);
            content.RegisterCallback<GeometryChangedEvent>(FirstUpdate);

            Add(content);
        }

        private void FirstUpdate(GeometryChangedEvent evt)
        {
            if (isFirstTime)
            {
                isFirstTime = false;
                _useSateName.value = true;
            }
        }

        private void UpdateSceneName(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                _sceneNameElement.OverrideSceneName(_stateName.value);
                _sceneNameElement.SetSceneNameReadonly(true);
                _stateName.RegisterCallback<ChangeEvent<string>>(_sceneNameElement.HookSceneName);
            }
            else
            {
                _stateName.UnregisterCallback<ChangeEvent<string>>(_sceneNameElement.HookSceneName);
                _sceneNameElement.SetSceneNameReadonly(false);
            }
        }
    }
}
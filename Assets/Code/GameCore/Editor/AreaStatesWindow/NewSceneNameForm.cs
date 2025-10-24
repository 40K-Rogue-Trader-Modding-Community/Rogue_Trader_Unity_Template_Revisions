using System;
using Kingmaker.Editor.UIElements.Custom.Base;
using UnityEngine.UIElements;

#nullable enable

namespace Kingmaker.Editor.AreaStatesWindow
{
    public class NewSceneNameForm : FormWindowBase<NewSceneNameForm>
    {
        private string? _areaName;
        private string? _defaultName;
        private Action<(string sceneName, string sceneTemplatePath)>? _createSceneAction;

        private SceneNameElement? _sceneNameElement;

        public static void Present(
            string? areaName,
            string? defaultName,
            Action<(string sceneName, string sceneTemplatePath)> createSceneAction)
        {
            Present("New scene", window =>
            {
                window._areaName = areaName;
                window._defaultName = defaultName;
                window._createSceneAction = createSceneAction;
            });
        }

        protected override void FillContent()
        {
            if (_content == null || _defaultName == null || _areaName == null)
            {
                return;
            }

            _sceneNameElement = new SceneNameElement(_defaultName, _areaName);
            _content.Add(_sceneNameElement);

            var buttonsLayout = new OwlcatContentContainer
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginTop = 10,
                }
            };
            buttonsLayout.Add(new Button(Create)
            {
                text = "Create state scene",
            });
            buttonsLayout.Add(new Button(Close)
            {
                text = "Cancel",
            });
            _content.Add(buttonsLayout);
        }

        private void Create()
        {
            if (_sceneNameElement != null)
            {
                string sceneName = _sceneNameElement.SceneName;
                string templatePath = _sceneNameElement.TemplatePath;
                _createSceneAction?.Invoke((sceneName, templatePath));
            }

            Close();
        }
    }
}
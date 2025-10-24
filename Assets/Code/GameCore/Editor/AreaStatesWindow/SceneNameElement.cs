using System;
using System.Linq;
using Kingmaker.Editor.Blueprints.Creation;
using Kingmaker.Editor.UIElements.Custom.Base;
using UnityEngine.UIElements;

#nullable enable

namespace Kingmaker.Editor.AreaStatesWindow
{
    public class SceneNameElement : VisualElement
    {
        private const string Mechanics = "Mechanics";
        private const string Static = "Static_ForArt";
        private const string Light = "Light";

        private static readonly string[] SceneTraits =
        {
            "Timeline",
            Mechanics,
            "Audio",
            Static,
            Light,
        };

        private readonly string _areaName;
        private readonly OwlcatContentContainer _content;
        private readonly TextField _sceneName;
        private readonly TextField _fullSceneName;

        private bool isFirstTime = true;

        public string SceneName => _fullSceneName.value;

        public string TemplatePath
        {
            get
            {
                string sceneName = SceneName ?? throw new ArgumentNullException(nameof(SceneName));
                string templatePath;
                if (sceneName.Contains($"_{Mechanics}"))
                {
                    templatePath = BlueprintAreaCreator.SceneTemplates.AddedMechanicsPath;
                }
                else if (sceneName.Contains($"_{Static}"))
                {
                    templatePath = BlueprintAreaCreator.SceneTemplates.StaticPath;
                }
                else if (sceneName.Contains($"_{Light}"))
                {
                    templatePath = BlueprintAreaCreator.SceneTemplates.LightPath;
                }
                else
                {
                    templatePath = BlueprintAreaCreator.SceneTemplates.AddedMechanicsPath;
                }

                return templatePath;
            }
        }

        public SceneNameElement(string defaultName, string areaName)
        {
            _areaName = areaName.Trim();

            _content = new OwlcatContentContainer
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    flexGrow = 0,
                },
            };

            // Scene name input
            _sceneName = new TextField
            {
                label = "Scene Name",
                value = defaultName,
                multiline = false,
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Row,
                }
            };
            _sceneName.RegisterValueChangedCallback(UpdateFullNameString);
            _content.Add(_sceneName);

            // Traits input
            foreach (string sceneTrait in SceneTraits)
            {
                var toggle = new Toggle
                {
                    name = sceneTrait,
                    label = sceneTrait,
                };
                toggle.RegisterValueChangedCallback(UpdateFullNameBool);
                _content.Add(toggle);
            }

            // Just to monitor full name built from area name, scene name and traits
            _fullSceneName = new TextField
            {
                label = "Full Name",
                isReadOnly = true,
                multiline = false,
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Row,
                    paddingTop = 10,
                }
            };
            _content.Add(_fullSceneName);
            _content.RegisterCallback<GeometryChangedEvent>(FirstUpdate);
            Add(_content);
        }

        /// <summary>
        /// Build full name from area name, scene name and traits,
        /// respecting them may be empty
        /// </summary>
        private void UpdateFullName()
        {
            string sceneName = _sceneName.value.Trim();
            string traits = string.Join("_", SceneTraits
                .Where(trait => _content.Q<Toggle>(trait).value));

            string fullName = string.Join("_", new[]{_areaName, sceneName, traits}
                .Where(part => !string.IsNullOrEmpty(part)));

            _fullSceneName.SetValueWithoutNotify($"{fullName}.unity");
        }

        private void UpdateFullNameBool(ChangeEvent<bool> evt)
        {
            UpdateFullName();
        }

        private void UpdateFullNameString(ChangeEvent<string> evt)
        {
            UpdateFullName();
        }

        private void FirstUpdate(GeometryChangedEvent evt)
        {
            if (isFirstTime)
            {
                isFirstTime = false;
                UpdateFullName();
            }
        }

        public void OverrideSceneName(string value)
        {
            _sceneName.value = value;
        }
        public void SetSceneNameReadonly(bool isReadonly)
        {
            _sceneName.isReadOnly = isReadonly;
        }
        public void HookSceneName(ChangeEvent<string> evt)
        {
            _sceneName.value = evt.newValue;
        }
    }
}
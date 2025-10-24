using System;
using System.Linq;
using Assets.Editor;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.UIElements;
using Kingmaker.View;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

#nullable enable

namespace Kingmaker.Editor.AreaStatesWindow
{
    public class AreaStatesWindow : EditorWindow
    {
        private BlueprintArea? _currentArea;

        private VisualElement? _content;

        private AreaStatesElement? _states;

        [MenuItem("Design/Area States")]
        public static void ShowWindow()
        {
            var window = GetWindow<AreaStatesWindow>();
            window.titleContent = new GUIContent("Area States");
            window.minSize = new Vector2(480, 320);
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is not BlueprintEditorWrapper {Blueprint: BlueprintArea area})
            {
                return;
            }
            _currentArea = area;
            UpdateStates();
        }

        private void UpdateStates()
        {
            if (_currentArea == null)
            {
                return;
            }

            _content?.Clear();

            var areaTitle = new Label(_currentArea.name);
            SetAreaTitleStyle(areaTitle.style);
            areaTitle.RegisterCallback<ClickEvent>(PingCurrentArea);
            _content?.Add(areaTitle);

            _states = new AreaStatesElement(_currentArea);
            _content?.Add(_states);
            _states.UpdateStates();
        }

        private void PingCurrentArea(ClickEvent evt)
        {
            BlueprintProjectView.Ping(_currentArea);
        }

        private void CreateGUI()
        {
            var scroll = new ScrollView
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.Auto,
                style = {flexGrow = 1},
            };

            _content = new VisualElement();
            SetContentStyle(_content.style);
            scroll.Add(_content);

            var corkLabel = new Label("No area selected");
            SetCorkLabelStyle(corkLabel.style);
            _content?.Add(corkLabel);

            var selectionLayout = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    paddingBottom = 5,
                    paddingLeft = 15,
                    paddingRight = 6,
                    paddingTop = 10,
                },
            };
            selectionLayout.style.paddingBottom = 5;

            var selectArea = CreateButton(
                "Load Area",
                "Select area blueprint and load it's base state",
                LoadArea);
            selectionLayout.Add(selectArea);

            var selectCurrentArea = CreateButton(
                "Current Area States",
                "Show currently loaded area states",
                ShowCurrentStates);
            selectionLayout.Add(selectCurrentArea);

            rootVisualElement.Add(selectionLayout);
            rootVisualElement.Add(scroll);

            UpdateStates();
        }

        private static Button CreateButton(string label, string tooltip, Action OnClick)
        {
            return new Button(OnClick)
            {
                text = label,
                tooltip = tooltip,
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 1,
                },
            };
        }
        private static void SetCorkLabelStyle(IStyle style)
        {
            style.fontSize = 12;
            style.marginTop = 20;
            style.unityTextAlign = TextAnchor.MiddleCenter;
        }

        private static void SetContentStyle(IStyle style)
        {
            style.flexGrow = 1;

            // Inspector-style paddings
            style.paddingBottom = 2;
            style.paddingLeft = 15;
            style.paddingRight = 6;
            style.paddingTop = 2;
        }

        private static void SetAreaTitleStyle(IStyle style)
        {
            style.fontSize = 12;
            style.unityTextAlign = TextAnchor.MiddleCenter;
            style.paddingRight = 15;
            style.backgroundColor = new Color(0, 0.3f, 0.5f, 1);
            StyleUtility.SetBorder(style, 1);
            StyleUtility.SetBorderRadius(style, 4);
            StyleUtility.SetBorderColor(style, Color.white * 0.8f);
        }

        private static BlueprintArea? GetCurrentArea()
        {
            return FindObjectsByType<AreaEnterPoint>(FindObjectsSortMode.None)
                .Select(ep => ep.Blueprint)
                .Where(ep => ep != null)
                .Select(ep => ep.Area)
                .FirstOrDefault(p => p);
        }

        private void ShowCurrentStates()
        {
            var currentArea = GetCurrentArea();
            if (currentArea != null)
            {
                _currentArea = currentArea;
                UpdateStates();
            }
        }

        /// <summary>
        /// Select area blueprint and load it's base state
        /// </summary>
        private void LoadArea()
        {
            BlueprintPicker.ShowAreaPicker((bp, _) =>
            {
                _currentArea = bp as BlueprintArea;
                if (_currentArea == null)
                {
                    return;
                }

                UpdateStates();
                _states?.LoadBaseState();
            });
        }

        [MenuItem("Design/Select Current Area %&r", false, 10010)]
        public static void SelectCurrentAreaItem()
        {
            var currentArea = GetCurrentArea();
            if (currentArea != null)
            {
                Selection.activeObject = null;
                EditorApplication.delayCall += () =>
                {
                    Selection.activeObject = BlueprintEditorWrapper.Wrap(currentArea);
                };
            }
        }
    }
}
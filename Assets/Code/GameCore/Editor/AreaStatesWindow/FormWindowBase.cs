using System;
using Kingmaker.Editor.UIElements;
using Kingmaker.Editor.UIElements.Custom.Base;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

#nullable enable

namespace Kingmaker.Editor.AreaStatesWindow
{
    /// <summary>
    /// Base class for simple forms to query user data in-place
    ///
    /// Contains some wierd stuff as I failed to find a straight way to properly resize
    /// window to fit it's content and place it under current mouse position.
    /// The cause is UIToolkit CreateGUI() is called after the window was already
    /// opened and positioned.
    /// </summary>
    public abstract class FormWindowBase<T> : EditorWindow where T : FormWindowBase<T>
    {
        private bool _wasFit;
        private bool _readyToFit;
        private Vector2 _expectedPosition;

        protected VisualElement? _content;

        // ReSharper disable once StaticMemberInGenericType
        private static Vector2? _lastMousePosition;
        public static Vector2? LastMousePosition
        {
            set => _lastMousePosition = value;
        }

        protected static void Present(string title, Action<T> init, Vector2? expectedSize = null, bool isAuxWindow = true)
        {
            if (HasOpenInstances<T>())
            {
                GetWindow<T>().Focus();
                return;
            }

            expectedSize ??= new Vector2(768, 768);

            // Create window
            var window = CreateInstance<T>(); // Used instead of GetWindow() because we need to perform init before CreateGUI() is hit
            window.titleContent = new GUIContent(title);
            init(window);

            // Try place window at current mouse position

            // Remember mouse position to place at it later
            if (Event.current == null)
            {
                // When form is created from some editor menu
                if (_lastMousePosition.HasValue)
                {
                    window._expectedPosition = GUIUtility.GUIToScreenPoint(_lastMousePosition.Value);
                    _lastMousePosition = null;
                }
                else
                {
                    window._expectedPosition = EditorGUIUtility.GetMainWindowPosition().center - expectedSize.Value / 2;
                }
            }
            else
            {
                window._expectedPosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
            }

            window._readyToFit = true;

            if (isAuxWindow)
            {
                window.ShowAuxWindow();
            }
            else
            {
                window.ShowUtility();
            }

            // Set expected window width and reserve some height to fit content later (as it fits only down somehow :\ )
            window.position = new Rect(Vector2.zero, expectedSize.Value);
        }

        /// <summary>
        /// Fill custom window content here
        /// </summary>
        protected abstract void FillContent();

        private void CreateGUI()
        {
            rootVisualElement.Clear();

            // To allow content fitting horizontally
            var rowContainer = new OwlcatContentContainer
            {
                style = {flexDirection = FlexDirection.Row, flexGrow = 0}
            };

            _content = new OwlcatContentContainer
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    flexGrow = 0,
                },
            };
            StyleUtility.SetPadding(_content.style, 8);

            FillContent();

            rowContainer.Add(_content);
            rootVisualElement.Add(rowContainer);

            rootVisualElement.RegisterCallback<GeometryChangedEvent>(Fit);
        }

        /// <summary>
        /// Fit window to content
        /// Runs only once, when all content is ready
        /// Once - to allow window to stay resizable
        /// </summary>
        private void Fit(GeometryChangedEvent evt)
        {
            if (!_readyToFit)
            {
                return;
            }

            if (_wasFit)
            {
                return;
            }

            float w = _content == null ? 512 : _content.resolvedStyle.width;
            float h = _content == null ? 512 : _content.resolvedStyle.height;
            var size = new Vector2(w, h);
            position = new Rect(_expectedPosition, size);

            _wasFit = true;
        }
    }
}
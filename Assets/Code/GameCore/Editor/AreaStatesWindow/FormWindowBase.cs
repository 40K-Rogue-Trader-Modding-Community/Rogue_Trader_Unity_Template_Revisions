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

        protected static void Present(string title, Action<T> init, int expectedWidth = 512)
        {
            // Try place window at current mouse position

            // Before that - try get parent window position as mouse position seems to be relative to it
            var parentWindow = focusedWindow;
            var parentWindowRect = focusedWindow == null
                ? EditorGUIUtility.GetMainWindowPosition() // Take main window instead
                : parentWindow.position;

            // Create window
            var window = GetWindow<T>(title);
            init(window);
            window.FillContent();

            // Set expected window width and reserve some height to fit content later (as it fits only down somehow :\ )
            window.position = new Rect(window.position.position, new Vector2(expectedWidth, 1024));

            // Remember mouse position to place at it later
            window._expectedPosition = parentWindowRect.min + Event.current.mousePosition;
            window._expectedPosition += new Vector2(0, 30); // Offset by window title bar

            window._readyToFit = true;

            window.ShowAuxWindow();
        }

        /// <summary>
        /// Fill custom window content here
        /// </summary>
        protected abstract void FillContent();

        private void CreateGUI()
        {
            rootVisualElement.Clear();

            _content = new OwlcatContentContainer
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    flexGrow = 0,
                },
            };
            StyleUtility.SetPadding(_content.style, 8);

            _content.RegisterCallback<GeometryChangedEvent>(Fit);

            rootVisualElement.Add(_content);
        }

        /// <summary>
        /// Fit window to content
        /// Runs only once, when all content is ready
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
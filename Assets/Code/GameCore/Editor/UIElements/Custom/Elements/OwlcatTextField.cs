using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Elements
{
	public class OwlcatTextField : TextField
    {
        private const int BottomOffset = 6;
        
		// ReSharper disable once InconsistentNaming
		public new bool multiline
		{
			get => base.multiline;
			private set
			{
				base.multiline = value;
                
				if (value)
				{
                    verticalScrollerVisibility = _scrollable ? ScrollerVisibility.Auto : ScrollerVisibility.Hidden;
					AddToClassList("owlcat-multiline");
					this.RegisterValueChangedCallback(OnValueChanged);
				}
				else
                {
                    verticalScrollerVisibility = ScrollerVisibility.Hidden;
					RemoveFromClassList("owlcat-multiline");
					this.UnregisterValueChangedCallback(OnValueChanged);
				}
			}
		}

        // ReSharper disable once InconsistentNaming
        public new bool isReadOnly
        {
            get => base.isReadOnly;
            set
            {
                base.isReadOnly = value;
                EnableInClassList("owlcat-readonly", value);
            }
        }

        private readonly bool _scrollable;

        private float _cachedCharacterHeight = -1;

        public OwlcatTextField(bool multiline = false, bool scrollable = false)
        {
            _scrollable = scrollable;
            this.multiline = multiline;

            AddToClassList("owlcat-text-area");

            schedule.Execute(CalculateCharacterHeight);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<ContextualMenuPopulateEvent>(OpenReadOnlyCopyMenu, TrickleDown.TrickleDown);
        }

        public void SetMinLines(int minLines)
        {
            if (_cachedCharacterHeight > 0)
                UpdateMinHeight(minLines);
            else
                schedule.Execute(() => UpdateMinHeight(minLines));
        }

        public void SetMaxLines(int maxLines)
        {
            if (_cachedCharacterHeight > 0)
                UpdateMaxHeight(maxLines);
            else
                schedule.Execute(() => UpdateMaxHeight(maxLines));
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (Mathf.Approximately(evt.oldRect.height, 0) && evt.newRect.height > 0)
                RecalculateHeight(value);
        }

        private void OnValueChanged(ChangeEvent<string> evt)
		{
		    RecalculateHeight(evt.newValue);
		}

        private void RecalculateHeight(string currentText)
        {
            if (!multiline || _cachedCharacterHeight < 0)
				return;

            if (string.IsNullOrEmpty(currentText))
            {
                style.height = _cachedCharacterHeight + BottomOffset;
            }
            else
            {
                var newSize = MeasureTextSize(currentText, resolvedStyle.width, MeasureMode.Exactly,
                    0, MeasureMode.Undefined);
                float newHeight = newSize.y;

                int newLines = Mathf.CeilToInt(newHeight / _cachedCharacterHeight);

                var contentCont = this.Q("unity-content-container");
                int lineCount = contentCont != null ?
                    (int) MathF.Ceiling((contentCont.resolvedStyle.height - BottomOffset) / _cachedCharacterHeight) :
                    1;

                style.height = Math.Max(lineCount, newLines) * _cachedCharacterHeight + BottomOffset;
            }
        }

        private void OpenReadOnlyCopyMenu(ContextualMenuPopulateEvent evt)
        {
            if (!isReadOnly)
                return;
                
            evt.StopImmediatePropagation();
            evt.menu.AppendAction("Copy", _ =>
            {
                int selStart = Math.Min(textSelection.selectIndex, textSelection.cursorIndex);
                int selEnd = Math.Max(textSelection.selectIndex, textSelection.cursorIndex);
                EditorGUIUtility.systemCopyBuffer = selEnd > selStart ? 
                    value.Substring(selStart, selEnd - selStart) : value;
            }, DropdownMenuAction.AlwaysEnabled);
        }

        private void CalculateCharacterHeight()
        {
            var size = MeasureTextSize("A", 0, MeasureMode.Undefined, 0, MeasureMode.Undefined);
            _cachedCharacterHeight = size.y;

            if (multiline)
            {
                var evt = ChangeEvent<string>.GetPooled(value, value);
                OnValueChanged(evt);
            }
        }

        private void UpdateMinHeight(int minLines)
        {
            style.minHeight = minLines * _cachedCharacterHeight + BottomOffset;
        }
        
        private void UpdateMaxHeight(int maxLines)
        {
            style.maxHeight = maxLines * _cachedCharacterHeight + BottomOffset;
        }
    }
}
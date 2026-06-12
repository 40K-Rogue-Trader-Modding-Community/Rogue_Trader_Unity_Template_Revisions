using System;
using Code.Framework.Editor.Utility;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.Blueprints.Elements
{
    public class TooltipManipulator : Manipulator
    {
        private IVisualElementScheduledItem m_TooltipSchedule;
        private Func<string> m_TooltipGetter;

        private OwlcatTooltip m_Tooltip
        {
            get 
            {
                s_Tooltip ??= new OwlcatTooltip();
                s_Tooltip.SetRoot(target);
                
                return s_Tooltip;
            }
        }

        private static OwlcatTooltip s_Tooltip;

        public TooltipManipulator(Func<string> tooltipGetter)
        {
            m_TooltipGetter = tooltipGetter;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerEnterEvent>(MouseIn);
            target.RegisterCallback<PointerLeaveEvent>(MouseOut);
            
            m_TooltipSchedule = target.schedule.Execute(() =>
            {
                m_Tooltip?.SetVisible(true);
            });
            m_TooltipSchedule.Pause();
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerEnterEvent>(MouseIn);
            target.UnregisterCallback<PointerLeaveEvent>(MouseOut);

            m_TooltipSchedule.Pause();
            m_Tooltip.SetVisible(false);
        }

        private void MouseIn(PointerEnterEvent e)
        {
            string text = m_TooltipGetter();
            if (string.IsNullOrEmpty(text))
                return;

            m_Tooltip.Label.text = text;

            float targetHeight = target.worldBound.height;
            var tagetCenter = target.worldBound.center;
            var center = m_Tooltip.Root.parent.WorldToLocal(tagetCenter);
            float maxWidth = Mathf.Min(m_Tooltip.Root.localBound.width, m_Tooltip.MaxWidth);
            m_Tooltip.SetWidth(maxWidth);

            var textSize = m_Tooltip.Label.MeasureTextSize(text, 0, VisualElement.MeasureMode.Undefined, 0,
                VisualElement.MeasureMode.Undefined);

            if (textSize.x > maxWidth)
            {
                textSize = m_Tooltip.Label.MeasureTextSize(text, maxWidth,
                    VisualElement.MeasureMode.Undefined, 0,
                    VisualElement.MeasureMode.Undefined);
            }

            float left = center.x - textSize.x / 2;
            float top = center.y + targetHeight / 2 + m_Tooltip.OffsetY;

            if (left < 0)
                left = 0;

            if (left + textSize.x > m_Tooltip.Root.localBound.width)
                left = m_Tooltip.Root.localBound.width - textSize.x - 10;

            if (top < 0)
                top = 0;

            if (top + textSize.y > m_Tooltip.Root.localBound.height)
                top = m_Tooltip.Root.localBound.height - textSize.y;

            m_Tooltip.style.left = left;
            m_Tooltip.style.top = top;

            m_TooltipSchedule.Resume();
            m_TooltipSchedule.ExecuteLater(500);
        }

        private void MouseOut(PointerLeaveEvent e)
        {
            m_Tooltip.SetVisible(false);
            m_TooltipSchedule.Pause();
        }

        public void ChangeGetter(Func<string> tooltipGetter)
        {
            m_TooltipGetter = tooltipGetter;
        }
    }

    public class OwlcatTooltip : HelpBox
    {
        private const float m_MaxWidth = 250;
        private const float m_TextSize = 12;
        private const float m_OffsetY = -20;

        public Label Label { get; }
        public float MaxWidth => m_MaxWidth;
        public float OffsetY => m_OffsetY;
        public VisualElement Root { get; private set; }

        private VisualElement m_Target;

        public OwlcatTooltip() : base(string.Empty, HelpBoxMessageType.None)
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.visibility = Visibility.Hidden;

            Label = this.Q<Label>();
            Label.pickingMode = PickingMode.Ignore;
            Label.style.fontSize = m_TextSize;
        }

        public void SetRoot(VisualElement target)
        {
            if (m_Target == target)
                return;
            
            m_Target = target;
            Root = VisualElementEx.GetInspectorRoot(target);
            
            if (parent != Root)
                Root.AddIfNotContains(this);
        }

        public void SetVisible(bool isVisible)
        {
            if (isVisible)
            {
                style.visibility = Visibility.Visible;
                BringToFront();
            }
            else
            {
                style.visibility = Visibility.Hidden;
            }
        }

        public void SetWidth(float width)
        {
            style.maxWidth = width;
            Label.style.maxWidth = width;
        }
    }
}
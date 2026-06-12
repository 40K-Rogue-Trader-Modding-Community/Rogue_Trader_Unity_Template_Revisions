using Kingmaker.Editor.UIElements.Custom.Base;
using System;
using Kingmaker.Editor.UIElements.Custom.PropertyComponents;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom
{
	[Obsolete("UI Elements tooltips don't work with enabled PlayMode. Use TooltipManipulator")]
	public class OwlcatPropertyTooltip : OwlcatPropertyComponent
    {
	    private readonly Func<string> m_TooltipGetter;

        public OwlcatPropertyTooltip(Func<string> tooltipGetter)
		{
            m_TooltipGetter = tooltipGetter;
        }

        protected override void OnAttached()
        {
	        Property.HeaderContainer.RegisterCallback<MouseEnterEvent>(OnEnter);
	        Property.HeaderContainer.RegisterCallback<MouseLeaveEvent>(OnExit);
        }

        private void OnEnter(MouseEnterEvent evt)
		{
            Property.TitleLabel.tooltip = m_TooltipGetter();
		}

        private void OnExit(MouseLeaveEvent evt)
		{
            Property.TitleLabel.tooltip = string.Empty;
        }
    }
}
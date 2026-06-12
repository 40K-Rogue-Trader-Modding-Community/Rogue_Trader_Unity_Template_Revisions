using Kingmaker.Editor.Blueprints.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.PropertyComponents
{
    public class ObsoleteComponent : OwlcatPropertyComponent
    {
        private readonly string m_Reason;
        
        private VisualElement m_ObsoleteLabel;

        public ObsoleteComponent(string reason)
 	    {
 	        m_Reason = reason;
 	    }
        
        protected override void OnAttached()
        {
            base.OnAttached();
            
            if (m_ObsoleteLabel != null)
                return;

            m_ObsoleteLabel = new Label { text = "[OBSOLETE]", style = { color = Color.red } };
            m_ObsoleteLabel.style.flexShrink = 1;
            m_ObsoleteLabel.style.flexGrow = 0;
            m_ObsoleteLabel.style.width = StyleKeyword.Auto;
            m_ObsoleteLabel.style.height = StyleKeyword.Auto;
            m_ObsoleteLabel.style.alignSelf = Align.Center;
            m_ObsoleteLabel.focusable = false;
            
            if (!string.IsNullOrEmpty(m_Reason))
                m_ObsoleteLabel.AddManipulator(new TooltipManipulator(() => m_Reason));
            
            Property.HeaderContainer.Add(m_ObsoleteLabel);

            // SetDisabled(Property.ContentContainer);
        }

        private void SetDisabled(VisualElement ve)
        {
            ve.AddToClassList("unity-disabled");
            ve.SetEnabled(false);
        }
    }
}
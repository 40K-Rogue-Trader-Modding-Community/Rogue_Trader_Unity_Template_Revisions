using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Utility.Attributes;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Elements
{
	public class ConditionalVisibilityElement
	{
		private readonly OwlcatProperty m_Property;
		private readonly ConditionalAttribute m_VisibilityAttribute;
		private readonly HeaderElement m_HeaderElement;
        
		private bool m_Visible;

		public ConditionalVisibilityElement(OwlcatProperty propertyElement, ConditionalAttribute conditionalVisibility, 
			HeaderElement headerElement)
		{
			m_Property = propertyElement;
			m_HeaderElement = headerElement;
			m_VisibilityAttribute = conditionalVisibility;
            
			propertyElement.schedule.Execute(CheckVisibility).Every(500).Until(() => 
				propertyElement.Property == null || conditionalVisibility == null);
			
			m_Visible = conditionalVisibility.IsFieldVisible(propertyElement.Property);
			UpdateVisibility();
		}
		
		private void CheckVisibility()
		{
			if (m_Property?.Property == null || m_VisibilityAttribute == null)
				return;
            
			bool value = m_VisibilityAttribute.IsFieldVisible(m_Property.Property);
			if (value != m_Visible)
			{
				m_Visible = value;
				UpdateVisibility();
			}
		}
        
		private void UpdateVisibility()
		{
			m_Property.style.display = m_Visible ? DisplayStyle.Flex : DisplayStyle.None;
                
			if (m_HeaderElement != null)
				m_HeaderElement.style.display = m_Property.style.display;
		}
	}
}
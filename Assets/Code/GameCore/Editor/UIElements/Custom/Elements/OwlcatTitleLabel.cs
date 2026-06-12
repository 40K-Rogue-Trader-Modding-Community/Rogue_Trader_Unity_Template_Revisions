using Kingmaker.Editor.UIElements.Custom.Base;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Elements
{
	public class OwlcatTitleLabel : Label
	{
		public readonly OwlcatProperty Property;
		
		public OwlcatTitleLabel(OwlcatPropertyLayout property) : this(property, string.Empty)
		{
			Property = property as OwlcatProperty;
		}

		public OwlcatTitleLabel(OwlcatPropertyLayout property, string text) : base(text)
		{
			Property = property as OwlcatProperty;
			AddToClassList("owlcat-title-label");
			focusable = true;
            
			RegisterCallback<FocusInEvent>(SetFocusedProperty);
			RegisterCallback<BlurEvent>(ClearFocusedProperty);
		}
		
		private void SetFocusedProperty(FocusInEvent evt)
		{
			if (Property != null)
				OwlcatProperty.Focused = Property;
		}

		private void ClearFocusedProperty(BlurEvent evt)
		{
			if (OwlcatProperty.Focused == Property)
				OwlcatProperty.Focused = null;
		}
	}

	public class OwlcatTitleLabelSizeControl : Label
	{
		public OwlcatTitleLabelSizeControl() : base(" ")
		{
			AddToClassList("owlcat-title-label-size-control");
		}
	}
}
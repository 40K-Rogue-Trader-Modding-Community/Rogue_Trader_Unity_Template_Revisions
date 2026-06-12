using System;
using Kingmaker.Editor.UIElements.Custom.Elements;
using Kingmaker.Utility.DotNetExtensions;
using Kingmaker.Utility.Attributes;
using Owlcat.Runtime.Core.Utility.EditorAttributes;
using UnityEngine;
using UnityEngine.UIElements;
using HelpBox = Kingmaker.Editor.UIElements.Custom.Elements.HelpBox;

namespace Kingmaker.Editor.UIElements.Custom.Base
{
	public class OwlcatContentContainer : OwlcatVisualElement
	{
		private ConditionalVisibilityElement m_ConditionalVisibilityElement;
		
		public new void Add(VisualElement element)
		{
			var property = element as OwlcatProperty;
            bool imguiDrawersNotUsed = property?.Attributes != null && !property.IsImguiWrapper();
            if (imguiDrawersNotUsed)
			{
				var header = GetAttribute<HeaderAttribute>(property);
				var visibility = GetAttribute<ConditionalAttribute>(property);
				var infoBox = GetAttribute<InfoBoxAttribute>(property);
				var space = GetAttribute<SpaceAttribute>(property);

				HeaderElement headerElement = null;
				if (header != null)
				{
					headerElement = new HeaderElement(header);
					base.Add(headerElement);
				}
				
				if (visibility != null)
					m_ConditionalVisibilityElement = new ConditionalVisibilityElement(property, visibility, headerElement);
				
				if (infoBox != null)
					base.Add(new HelpBox(infoBox.Text));
				
				if (space != null)
					base.Add(new VisualElement { name = "Space", style = { height = space.height } });
			}

			base.Add(element);
        }

		private static T GetAttribute<T>(OwlcatProperty property) where T : Attribute
			=> (T)property.Attributes?.FirstItem(a => a is T);
	}
}
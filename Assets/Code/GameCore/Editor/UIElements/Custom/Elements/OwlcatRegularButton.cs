using System;
using Kingmaker.Editor.UIElements.Custom.Base;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Elements
{
	public class OwlcatRegularButton : OwlcatVisualElement
	{
		private Button m_Button;
		public OwlcatRegularButton(
			string buttonText,
			Action clickEvent)
		{
			m_Button = new Button(clickEvent);
			m_Button.text = buttonText;
			Add(m_Button);
		}
	}
}
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Elements
{
	public class OwlcatRoClearTextField : TextField
	{
		public OwlcatRoClearTextField()
		{
			style.fontSize = 10;
			style.alignSelf = Align.Center;
			isReadOnly = true;
			var input = this.Q("unity-text-input");
			input.AddToClassList("owlcat-input-clear");
		}
	}
}
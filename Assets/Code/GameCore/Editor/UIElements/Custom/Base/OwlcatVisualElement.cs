using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Base
{
	public class OwlcatVisualElement : VisualElement
	{
		public OwlcatInspectorRoot Root
		{
			get
			{
				VisualElement p = this;
				while (p != null && !(p is OwlcatInspectorRoot))
				{
					p = p.parent;
				}

				return (OwlcatInspectorRoot)p;
			}
		}
		
		public OwlcatInspectorRoot GetUpperRoot()
		{
			var current = parent;
			OwlcatInspectorRoot root = null;
			
			while (current != null)
			{
				if (current is OwlcatInspectorRoot owlcatInspectorRoot)
					root = owlcatInspectorRoot;

				current = current.parent;
			}

			return root;
		}
	}
}
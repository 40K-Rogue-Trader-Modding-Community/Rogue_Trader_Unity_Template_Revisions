using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Kingmaker.Blueprints.Attributes;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.Editor.UIElements.Custom.Elements;
using Kingmaker.Editor.UIElements.Custom.Properties;
using Owlcat.Runtime.Core.Logging;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Owlcat.Runtime.Core.Utility;

namespace Kingmaker.Editor.UIElements.Custom.Base
{
	public static class OwlcatVisualElementHelper
	{
		private static readonly LogChannel Logger = LogChannelFactory.GetOrCreate("Inspector");
		public static IEnumerable<OwlcatProperty> GetAllProperties(this VisualElement root)
		{
			if (root is OwlcatProperty property)
			{
				yield return property;
			}

			if (root.style.display == DisplayStyle.None)
			{
				yield break;
			}

			foreach (var e in root.Children())
			{
				foreach (var ee in e.GetAllProperties())
				{
					yield return ee;
				}
			}
		}

		[CanBeNull]
		public static OwlcatProperty GetFocusedProperty(this FocusController focusController)
		{
			var p = focusController?.focusedElement as VisualElement;
			while (p != null && !(p is OwlcatProperty))
			{
				p = p.parent;
			}

			return (OwlcatProperty)p;
		}

		[CanBeNull]
		public static OwlcatTitleLabel GetFirstFocusableTitle(this VisualElement ve)
		{
			if (ve.style.display == DisplayStyle.None)
				return null;

			foreach (var child in ve.hierarchy.Children())
			{
				if (child.focusable && child is OwlcatTitleLabel titleLabel)
					return titleLabel;

				var hierarchy = child.hierarchy;
				int num;
				if (hierarchy.parent != null)
				{
					var contentContainer = child.hierarchy.parent.contentContainer;
					num = child == contentContainer ? 1 : 0;
				}
				else
				{
					num = 0;
				}

				bool flag = num != 0;
				if (!flag)
				{
					var firstFocusableChild = child.GetFirstFocusableTitle();
					if (firstFocusableChild != null)
						return firstFocusableChild;
				}
			}

			return null;
		}

		public static OwlcatProperty WrapToOwlcatProperty(this VisualElement element, SerializedProperty property)
		{
			if (element is OwlcatProperty result && result.PropertyPath == property.propertyPath)
				return result;
			
			if (element is PropertyField)
				return new OwlcatPropertyField(property);

			var info = property.GetFieldInfo();
			var layout = info != null && info.HasAttribute<VerticalLayoutAttribute>() ? 
				OwlcatPropertyLayout.Layout.VerticalNotExpandable : OwlcatPropertyLayout.Layout.Horizontal;
            
			return new WrappedOwlcatProperty(property, element, layout);
		}

		public static bool IsImguiWrapper(this VisualElement element)
			=> element.childCount == 1 && element[0] is IMGUIField;

		public static IEnumerable<VisualElement> GetAllChildren(this VisualElement element)
		{
			return element.Children().Concat(element.Children().SelectMany(c => c.GetAllChildren()));
		}

		public static void SetVisible(this VisualElement visualElement, bool visible)
			=> visualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
	}
}
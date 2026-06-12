using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Editor.Elements;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.Utility;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.PropertyComponents
{
	public class CopyHandlerComponent : OwlcatPropertyComponent, IOwlcatPropertyInputHandler
	{
		int IOwlcatPropertyInputHandler.Order { get; } = 1;

		void IOwlcatPropertyInputHandler.TryHandle(KeyDownEvent evt)
		{
			if (evt.keyCode == KeyCode.C && evt.ctrlKey)
			{
				if (OwlcatProperty.Focused != Property)
					return;
                
				var type = SerializableTypesCollection.GetType(Property.Property) ??
				           FieldFromProperty.GetActualValueType(Property.Property);
                
				if (type == null)
					return;
				
				CopyPasteController.CopyProperty(Property.Property, null);
				evt.StopPropagation();
			}
		}
	}
}
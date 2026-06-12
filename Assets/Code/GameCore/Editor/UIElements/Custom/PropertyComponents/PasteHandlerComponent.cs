using Kingmaker.Editor.Elements;
using Kingmaker.Editor.UIElements.Custom.Base;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.PropertyComponents
{
	public class PasteHandlerComponent : OwlcatPropertyComponent, IOwlcatPropertyInputHandler
	{
		int IOwlcatPropertyInputHandler.Order { get; } = 1;

		void IOwlcatPropertyInputHandler.TryHandle(KeyDownEvent evt)
		{
			if (evt.keyCode == KeyCode.V && evt.ctrlKey)
			{
				if (OwlcatProperty.Focused != Property)
					return;
                
				var type = CopyPasteController.GetPasteableType(Property.RobustProperty);

				if (type == null)
					return;

				if (!CopyPasteController.IsSuitableForPaste(type)) 
					return;

				if (!CopyPasteController.PasteProperty(type, Property.Property)) 
					return;
                
				Property.Property.serializedObject.ApplyModifiedProperties();
				Property.Property.serializedObject.Update();
				Property.DescriptionButton?.PastePostProcess?.Invoke();
				evt.StopPropagation();
			}
		}

	}
}
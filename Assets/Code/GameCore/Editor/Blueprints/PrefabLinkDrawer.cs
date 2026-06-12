using Kingmaker.Blueprints;
using Kingmaker.Editor.UIElements.ValuePicker;
using Kingmaker.ResourceLinks;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Blueprints
{
	[CustomPropertyDrawer(typeof(PrefabLink))]
	public class PrefabLinkDrawer : WeakLinkDrawer<GameObject>
	{		
		protected override bool Filter(AssetPicker.HierarchyEntry entry)
		{
			var allowedEntityTypeAttribute = fieldInfo.GetAttribute<AllowedEntityTypeAttribute>();
			if (allowedEntityTypeAttribute == null)
				return true;
			
			entry.FindAsset();

			if (entry.Asset is GameObject gameObject)
			{
				if (allowedEntityTypeAttribute.Type != typeof(GameObject) && 
				    gameObject.TryGetComponent(allowedEntityTypeAttribute.Type, out _))
				{
					return true;
				}
				
				return false;
			}

			return true;
		}
	}
}
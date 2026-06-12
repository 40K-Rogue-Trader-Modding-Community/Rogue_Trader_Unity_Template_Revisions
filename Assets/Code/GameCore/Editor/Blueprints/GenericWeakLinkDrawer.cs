using System;
using JetBrains.Annotations;
using Kingmaker.Editor.UIElements.Custom.Properties;
using Kingmaker.Editor.UIElements.ValuePicker;
using Kingmaker.Editor.Utility;
using Kingmaker.ResourceLinks;
using Owlcat.Editor.Core.Utility;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Kingmaker.Editor.Blueprints
{
    [CustomPropertyDrawer(typeof(WeakResourceLink<>), true)]
	public class GenericWeakLinkDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
            var type = GetAssetType();
			var idProperty = property.FindPropertyRelative(nameof(WeakResourceLink.AssetId));
			var idPropertySafe = new RobustSerializedProperty(idProperty);
			var currentValue = GetAsset(idProperty.hasMultipleDifferentValues?null:idProperty.stringValue, type);
			Action<Object> pickCallback = o =>
				{
					var p = idPropertySafe.Property;
					using (GuiScopes.UpdateObject(p.serializedObject))
					{
						idPropertySafe.Property.stringValue = GetGuid(o, type);
					}
				};

			AssetPicker.ShowPropertyField(
				position, property, fieldInfo,
				currentValue, pickCallback, 
				label, type
			);
		}

        private Type GetAssetType()
        {
            var genericType = fieldInfo.FieldType;
            while (true)
            {
                if (genericType.IsArray)
                {
                    var elementType = genericType.GetElementType();
                    if (elementType != null)
                    {
                        genericType = elementType;
                        continue;
                    }
                }

                if (genericType.IsList())
                {
                    var elementType = genericType.GenericTypeArguments[0];
                    if (elementType != null)
                    {
                        genericType = elementType;
                        continue;
                    }
                }
				
                break;
            }
            
            if (genericType.IsGenericType && 
                genericType.GetGenericTypeDefinition() == typeof(WeakResourceLink<>))
            {
                return genericType.GenericTypeArguments[0];
            }
            
            var baseType = genericType.BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType && 
                    baseType.GetGenericTypeDefinition() == typeof(WeakResourceLink<>))
                {
                    return baseType.GenericTypeArguments[0];
                }
                baseType = baseType.BaseType;
            }

            return genericType;
        }
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new WeakLinkProperty(property, GetAssetType(), fieldInfo, null);
        }

		[CanBeNull]
		public static Object GetAsset(string guid, Type type)
		{
			if (string.IsNullOrEmpty(guid))
				return null;

			string assetPath = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrEmpty(assetPath))
				return null;

			return AssetDatabase.LoadAssetAtPath(assetPath, type);
		}

        public static string GetGuid(Object asset, Type type)
		{
			if (typeof(MonoBehaviour).IsAssignableFrom(type))
			{
				if (!(asset != null && (asset.GetType() == type || asset.GetType().IsSubclassOf(type))))
                {
                    var go = asset as GameObject;
                    if (go == null)
                    {
                        return "";
                    }

                    if (go.GetComponent(type) == null)
                    {
                        return "";
                    }
                }
            }

			string assetPath = AssetDatabase.GetAssetPath(asset);
			return AssetDatabase.AssetPathToGUID(assetPath);
		}
	}
}
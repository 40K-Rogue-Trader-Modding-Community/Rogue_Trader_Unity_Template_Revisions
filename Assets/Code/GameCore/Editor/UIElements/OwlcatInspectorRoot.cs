using System;
using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.Editor.UIElements.Custom;
using Kingmaker.Editor.UIElements.Custom.Base;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements
{
	public class OwlcatInspectorRoot : OwlcatContentContainer
	{
		//Paths may change in nearest future. This is left to make search easier
#if OWLCAT_MODS
		public const string CommonPath = "Assets/Code/GameCore/Editor/UIElements/Styles/CommonStyle.uss";
		public const string ProPath = "Assets/Code/GameCore/Editor/UIElements/Styles/ProStyle.uss";
		public const string PersonalPath = "Assets/Code/GameCore/Editor/UIElements/Styles/PersonalStyle.uss";
#else
		public const string CommonPath = "Assets/Code/GameCore/Editor/UIElements/Styles/CommonStyle.uss";
		public const string ProPath = "Assets/Code/GameCore/Editor/UIElements/Styles/ProStyle.uss";
		public const string PersonalPath = "Assets/Code/GameCore/Editor/UIElements/Styles/PersonalStyle.uss";
#endif
		
		public readonly SerializedObject SerializedObject;
		public HashSet<BlueprintScriptableObject> InlinedBlueprints = new();

		public OwlcatInspectorRoot(SerializedObject serializedObject, bool isHideScriptProp)
		{
			SerializedObject = serializedObject;
			if (serializedObject.targetObject == null)
			{
				DrawMissingScript();
				return;
			}
			
			name = serializedObject.targetObject.name;

			LoadStyles();
			SetupContent(this, serializedObject, isHideScriptProp);
		}

        public OwlcatInspectorRoot(SerializedProperty property)
        {
			SerializedObject = property.serializedObject;
            name = property.FindPropertyRelative("name")?.stringValue ?? property.serializedObject.targetObject.name;

            LoadStyles();
            if (property.hasVisibleChildren)
            {
                property = property.Copy(); // this is a root property, but for the SetupContent we need its first child
                if (property.NextVisible(true))
                {
                    SetupContent(this, property, true);
                }
            }
        }
        
        /// <summary>
        /// To be able to create inspector for a pre-constructed elements
        /// </summary>
        public OwlcatInspectorRoot(SerializedObject so, IEnumerable<OwlcatVisualElement> elements)
        {
            SerializedObject = so;
            name = SerializedObject.targetObject.name;

            LoadStyles();
            foreach (var element in elements)
                Add(element);
        }

        private void LoadStyles()
        {
	        var styles = AssetDatabase.LoadAssetAtPath<StyleSheet>(EditorGUIUtility.isProSkin ? ProPath : PersonalPath);
	        styleSheets.Add(styles);
	        
	        styles = AssetDatabase.LoadAssetAtPath<StyleSheet>(CommonPath);
	        styleSheets.Add(styles);
        }

        public static void SetupContent(OwlcatContentContainer root, SerializedObject serializedObject, bool isHideScriptProp)
		{
			var iterator = serializedObject.GetIterator();
			if (iterator.NextVisible(true))
			{
				SetupContent(root, iterator, isHideScriptProp);
			}
		}
        
		public static void SetupContent(OwlcatContentContainer root, SerializedProperty rootProperty, bool isHideScriptProp)
        {
            int startDepth = rootProperty.depth; 
            do
            {
                if (rootProperty.propertyPath.Equals("m_Script"))
                {
                    if (!isHideScriptProp)
                    {
	                    var field = new OwlcatPropertyField(rootProperty);
	                    field.ContentContainer.SetEnabled(false);
	                    field.ControlsContainer.SetEnabled(false);
                        root.Add(field);
                    }

                    continue;
                }
                
                try
                {
	                var propField = UIElementsUtility.CreatePropertyElement(rootProperty, false);
	                if (propField != null)
		                root.Add(propField);
                }
                catch (Exception e)
                {
	                root.Add(new ErrorElement($"{rootProperty.displayName}: {e.Message}",
		                $"Mess: {e.Message}\nTrace: {e.StackTrace}\nInner: {e.InnerException?.Message}"));
                }
            } while (rootProperty.NextVisible(false) && rootProperty.depth>=startDepth); // if we did not start at root, exit the loop when we get out of the property
        }

		private void DrawMissingScript()
		{
			var objectField = new ObjectField("Script");
			objectField.objectType = typeof(MonoScript);
			objectField.SetEnabled(false);
			
			var infoBox = new HelpBox("Script is missing", HelpBoxMessageType.Error);
			
			Add(infoBox);
			Add(objectField);
		}
	}
}
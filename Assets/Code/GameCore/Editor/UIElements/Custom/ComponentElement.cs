using Code.GameCore.ElementsSystem;
using Kingmaker.Blueprints;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Elements;
using System;
using System.Linq;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Editor.Blueprints.Elements;
using Kingmaker.Editor.Utility;
using Kingmaker.EntitySystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Owlcat.Runtime.Core.Utility.EditorAttributes;
using Owlcat.Runtime.Core.Utility;
using UnityEngine.PlayerLoop;
using UnityHelpBox = UnityEngine.UIElements.HelpBox;
using HelpBox = Kingmaker.Editor.UIElements.Custom.Elements.HelpBox;

namespace Kingmaker.Editor.UIElements.Custom
{
	public class ComponentElement : OwlcatPropertyLayout
    {
	    public override bool IsExpanded
	    {
		    get => m_Property.Property.isExpanded;
		    set
            {
	            m_Property.Property.isExpanded = value;
	            OnIsExpandedChangedInternal();
            }
        }

	    private readonly RobustSerializedProperty m_Property;
	    private readonly RobustSerializedProperty m_FlagsProperty;
	    private readonly string m_Name;
	    private readonly bool m_IsObsolete;
	    private readonly VisualElement m_HasDuplicatesWarning;
		
		private VisualElement m_Content;
		private OwlcatRoClearTextField m_Comment;
		
        private bool m_Initialized;
        
        private bool m_HasDuplicates;
        
        private bool m_Disabled => m_FlagsProperty.Property.enumValueFlag == 1 << 0;
        
        public int Index { get; }
        public bool IsValid { get; set; } = true;
        
        public bool HasDuplicates
        {
	        get => m_HasDuplicates;
	        set
	        {
		        m_HasDuplicates = value; 
		        UpdateWarning();
	        }
        }
        
        public event Action<ComponentElement> OnMoveUpEvent = delegate { };
		public event Action<ComponentElement> OnMoveDownEvent = delegate { };
		public event Action<ComponentElement> OnRemoveEvent = delegate { };
		public event Action<ComponentElement> OnCopyEvent = delegate { };

		public ComponentElement(SerializedProperty property, int index) : base(Layout.Vertical)
		{
			Index = index;
			m_Property = new RobustSerializedProperty(property);
			m_FlagsProperty = m_Property.Property.FindPropertyRelative("m_Flags");
			
			var type = ((BlueprintComponent) FieldFromProperty.GetFieldValue(m_Property.Property)).GetType();
			m_Name = type.Name;

			AddToClassList("owlcat-component");
			AddToClassList("owlcat-box");

			HeaderContainer.AddToClassList("owlcat-component");
			ContentContainer.AddToClassList("owlcat-component");
			ControlsContainer.AddToClassList("owlcat-component");
			style.marginBottom = 10;

			TitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			
			var obsoleteAttribute = type.GetAttribute<ObsoleteAttribute>();
			m_IsObsolete = obsoleteAttribute != null;
            if (obsoleteAttribute is {Message: {} obsoleteMessage})
            {
                TitleLabel.AddManipulator(new TooltipManipulator(() => obsoleteMessage));
                ContentContainer.Add(new UnityHelpBox(obsoleteMessage, HelpBoxMessageType.Warning));
            }

            m_HasDuplicatesWarning = new Label("Multiple components of this type are not supported")
				{ style = { display = DisplayStyle.None, unityFontStyleAndWeight = FontStyle.Bold } };
			ContentContainer.Add(m_HasDuplicatesWarning);

			UpdateTitle();
			SetupHeaderContent();
			SetupControls();
			UpdateWarning();
		}
		
		private void SetupHeaderContent()
		{
			HeaderAsPropertyLayout = true;

			var content = new VisualElement
			{
				style =
				{
					flexGrow = 1,
					flexDirection = FlexDirection.Column
				}
			};

			m_Comment = new OwlcatRoClearTextField
			{
				name = "Comment",
				style = { alignSelf = Align.FlexStart }
			};

			var idLabel = new OwlcatRoClearTextField
			{
				name = "IdLabel",
			};
			idLabel.Q<TextElement>().style.opacity = 0.5f;
			string componentName = (FieldFromProperty.GetFieldValue(m_Property) as BlueprintComponent)?.name;
			idLabel.value = componentName?.Split('$').LastOrDefault()?.Replace("(Clone)", "");

			content.Add(m_Comment);
			content.Add(idLabel);
			HeaderContentContainer.Add(content);
		}

		private void SetupContent()
		{
			if (m_Initialized) 
				return;
            
			m_Initialized = true;
            
			var type = (FieldFromProperty.GetFieldValue(m_Property) as BlueprintComponent)?.GetType();
			var typeField = new BlueprintScriptProperty(type);
			ContentContainer.Add(typeField);
			
			var infoAttributes = type.GetAttributes<ClassInfoBox>();
            foreach (var info in infoAttributes)
            {
                ContentContainer.Add(new HelpBox(info.Text));
            }

            var prop = m_Property.Property.Copy();
            prop.Next(true);
            OwlcatInspectorRoot.SetupContent(ContentContainer, prop, true);

            foreach (var owlcatProperty in ContentContainer.Query<OwlcatProperty>().Build())
            {
	            if (owlcatProperty.PropertyPath.StartsWith(m_Property.Path))
		            owlcatProperty.OverridableControl?.SetComponent(m_Property);
            }
		}

		private void SetupControls()
		{
			var enabledToggle = new Toggle { style = { marginRight = 3 } };
			enabledToggle.SetValueWithoutNotify(!m_Disabled);
			
			var moveUp = new OwlcatSmallButton(() => OnMoveUpEvent(this)) { text = "↑" };
			var moveDown = new OwlcatSmallButton(() => OnMoveDownEvent(this)) { text = "↓" };
			var settings = new Button(ShowComponentContextMenu);
			settings.AddToClassList("owlcat-settings-button");

			var descriptionButton = new OwlcatDescriptionButton(m_Property, null, this);
			
			ControlsContainer.Add(enabledToggle);
			ControlsContainer.Add(moveUp);
			ControlsContainer.Add(moveDown);
			ControlsContainer.Add(descriptionButton);
			ControlsContainer.Add(settings);
			
			enabledToggle.RegisterValueChangedCallback(
				evt => 
				{
					if (evt.newValue)
					{
						m_FlagsProperty.Property.enumValueFlag &= ~(int)BlueprintComponent.Flags.Disabled;
					}
					else
					{
						m_FlagsProperty.Property.enumValueFlag |= (int)BlueprintComponent.Flags.Disabled;
					}

					m_FlagsProperty.serializedObject.ApplyModifiedProperties();
					m_FlagsProperty.serializedObject.Update();
                    
					UpdateTitle();
					UpdateExpanded();
				});
		}
		
		private void ShowComponentContextMenu()
		{
			var menu = new GenericMenu();
			
			menu.AddItem(new GUIContent("Remove"), false, () => { OnRemoveEvent(this); });
			menu.AddItem(new GUIContent("Copy"), false, () => { OnCopyEvent(this); });
			menu.AddItem(new GUIContent("Solo"), false,
				() =>
				{
					var component = FieldFromProperty.GetFieldValue(m_Property) as BlueprintComponent;
					var wrapper = BlueprintComponentEditorWrapper.Wrap(component);
					Selection.activeObject = wrapper;
				});
			
			menu.ShowAsContext();
		}

		private void UpdateTitle()
		{
			if (m_IsObsolete & m_Disabled)
				TitleLabel.text = $"DISABLED OBSOLETE {m_Name}";
			else if (m_IsObsolete)
				TitleLabel.text = $"OBSOLETE {m_Name}";
			else if (m_Disabled)
				TitleLabel.text = $"DISABLED {m_Name}";
			else
				TitleLabel.text = m_Name;
		}

		private void UpdateExpanded()
		{
			IsExpanded &= !m_Disabled;
		}

		protected override void OnIsExpandedChanged()
		{
			if (IsExpanded && m_Disabled)
			{
				IsExpanded = false;
				return;
			}

			base.OnIsExpandedChanged();
			
			if (m_Comment != null)
			{
				if (IsExpanded)
				{
					m_Comment.style.display = DisplayStyle.None;
				}
				else
				{
					string comment = m_Property.Property.FindPropertyRelative("Comment")?.stringValue;
					m_Comment.value = comment;
					m_Comment.style.display = string.IsNullOrEmpty(comment) ? DisplayStyle.None : DisplayStyle.Flex;
				}
			}
			
			if (IsExpanded)
				SetupContent();
		}
		
		private void UpdateWarning()
		{
			if (m_HasDuplicates)
				m_HasDuplicatesWarning.style.display = DisplayStyle.Flex;

			var borderColor = m_HasDuplicates || m_IsObsolete ?
				new StyleColor(new Color(0.75f, 0.4f, 0.4f, 1.0f)) : StyleKeyword.Null;
			style.backgroundColor = borderColor;
			m_HasDuplicatesWarning.style.display = m_HasDuplicates ? DisplayStyle.Flex : DisplayStyle.None;
		}
    }
}
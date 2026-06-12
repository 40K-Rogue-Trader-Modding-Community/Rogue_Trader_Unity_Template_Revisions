using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Attributes;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.Editor.UIElements.Custom.Prototypable;
using Kingmaker.Editor.Utility;
using System;
using System.Collections.Generic;
using Code.Editor.Utility;
using Kingmaker.Blueprints.Base;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.Blueprints.Elements;
using Kingmaker.Editor.UIElements.Custom.Elements;
using Kingmaker.Editor.UIElements.Custom.Properties;
using Kingmaker.Editor.UIElements.Custom.PropertyComponents;
using Kingmaker.Utility.Attributes;
using Kingmaker.Utility.DotNetExtensions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Owlcat.Runtime.Core.Utility;

namespace Kingmaker.Editor.UIElements.Custom.Base
{
	public class OwlcatProperty : OwlcatPropertyLayout
	{
		public static OwlcatProperty Focused;
		
		[CanBeNull]
		public Attribute[] Attributes { get; set; }

		private StyleColor m_PrevBackgroundColor;

		public readonly OverridablePropertyControl OverridableControl;

		public OwlcatDescriptionButton DescriptionButton { get; private set; }
		
		public RobustSerializedProperty RobustProperty { get; }

		public SerializedProperty Property
			=> RobustProperty.Property;

		public string PropertyPath
			=> RobustProperty.Path;

		public override bool canGrabFocus
			=> true;

		public override bool IsExpanded
		{
			get => Property?.isExpanded ?? false;
			set
			{
                if (Property != null)
                {
                    Property.isExpanded = value;
                    OnIsExpandedChangedInternal();
                }
            }
		}

		[CanBeNull]
		private IOwlcatPropertyTitleProvider m_TitleProvider;

		private readonly List<IOwlcatPropertyInputHandler> m_InputHandlers 
			= new List<IOwlcatPropertyInputHandler>();

		private readonly Dictionary<Type, IOwlcatPropertyComponent> m_Components 
			= new Dictionary<Type, IOwlcatPropertyComponent>();
		
		protected bool m_ContentCreated;
		protected TooltipManipulator m_TooltipManipulator;

		public OwlcatProperty(SerializedProperty property, Layout layout = Layout.Horizontal) : base(layout)
		{
			name = property.propertyPath;
			focusable = true;
			RobustProperty = new RobustSerializedProperty(property.Copy());
			UpdateTitle();

			AddToClassList("owlcat-property");

			OverridableControl = new OverridablePropertyControl(this);
			OverridableControlContainer.Add(OverridableControl);

			RegisterCallback<FocusEvent>(OnFocus);
			RegisterCallback<KeyDownEvent>(x => TryHandle(x));
            
			this.TrackPropertyValue(property, OnPropertyChanged);
            
			if (property.HasNotNullAttribute())
				AddComponent(new NotNullComponent());
			if (property.HasObsoleteAttribute(out string reason))
				AddComponent(new ObsoleteComponent(reason));
			if (property.HasReadOnlyAttribute())
				AddComponent(new ReadOnlyComponent());
            
            var onValueChangedAttribute = property.GetFieldInfo()?.GetAttribute<OnValueChangedAttribute>();
 	        if (onValueChangedAttribute != null)
 	            AddComponent(new OnPropertyChangedComponent(this, onValueChangedAttribute));
            
            if (property.TryGetExpandAttribute(out var inspectorExpanded))
            {
                if (inspectorExpanded.WithChildren)
                    property.ExpandCollapseAll(this, true);
                else
                    property.isExpanded = true;
            }
            
			AddComponent(new CopyHandlerComponent());
			AddComponent(new PasteHandlerComponent());
			
			if (Property.serializedObject.targetObject is ScriptableWrapperBase or MonoBehaviour)
	        {
		 		DescriptionButton = new OwlcatDescriptionButton(RobustProperty, OverridableControl, this);
		 		ControlsContainer.Add(DescriptionButton);
		    }
		}

		public static OwlcatProperty CreateDefault(SerializedProperty property)
		{
			return new OwlcatPropertyField(property);
		}

		public static OwlcatProperty CreateGeneric(SerializedProperty prop)
		{
			bool isExpandable = !prop.GetFieldInfo()?.HasAttribute<NonFoldoutAttribute>() ?? true;
			return new GenericTypeOwlcatProperty(prop, isExpandable ? Layout.Vertical : Layout.VerticalNotExpandable);
		}
		
		protected void CreateContent()
		{
			if (!Expandable || IsExpanded)
			{
				if (m_ContentCreated)
					return;
            
				m_ContentCreated = true;
				CreateContentInternal();
			}
		}
		
		protected virtual void CreateContentInternal() { }

		public void AddComponent([CanBeNull] IOwlcatPropertyComponent component)
		{
			if (component == null)
			{
				return;
			}

			var titleProvider = component as IOwlcatPropertyTitleProvider; 
			if (titleProvider != null)
			{
				if (m_TitleProvider == null || m_TitleProvider.Order > titleProvider.Order)
				{
					m_TitleProvider = titleProvider;
				}
			}

			if (component is IOwlcatPropertyInputHandler handler)
			{
				m_InputHandlers.Add(handler);
				m_InputHandlers.Sort(OwlcatPropertyInputHandlerSorter.Instance);
			}
            
            if (m_Components.ContainsKey(component.GetType())) 
                m_Components[component.GetType()].DetachFromProperty();

			m_Components[component.GetType()] = component;
			component.AttachToProperty(this);

			if (titleProvider != null)
			{
				UpdateTitle();
			}
		}

		[CanBeNull]
		public T GetComponent<T>() where T : OwlcatPropertyComponent
			=> m_Components.GetValueOrDefault(typeof(T)) as T;

		public bool HasComponent<T>() where T : IOwlcatPropertyComponent
			=> m_Components.ContainsKey(typeof(T));
		
		public void RemoveComponent<T>() where T : IOwlcatPropertyComponent
		{
			if (!HasComponent<T>())
				return;

			var component = m_Components[typeof(T)];
			m_Components.Remove(typeof(T));
			component.DetachFromProperty();
		}

		public void UpdateTitle()
		{
			TitleLabel.text = m_TitleProvider?.GetTitle() ?? Property.displayName;
		}

		private void OnFocus(FocusEvent evt)
		{
			var focusDelegate = this.GetFirstFocusableTitle();
			focusDelegate?.Focus();
		}

		protected override void OnAttachToPanelInternal(AttachToPanelEvent evt)
		{
			base.OnAttachToPanelInternal(evt);
			
			m_TooltipManipulator = new TooltipManipulator(Property.GetTooltip);
			TitleLabel.AddManipulator(m_TooltipManipulator);
			
			if (ContentContainer.IsImguiWrapper())
				HeaderContainer.style.display = DisplayStyle.None;
		}

		protected override void OnAfterAttachToPanelInternal()
		{
			base.OnAfterAttachToPanelInternal();
            
			CreateContent();

			DescriptionButton?.BringToFront();
			GetComponent<NotNullComponent>()?.Update();
		}

		protected virtual void OnPropertyChanged(SerializedProperty property)
		{
			UpdateTitle();
			GetComponent<NotNullComponent>()?.Update();
            
			var currentParent = parent;
			while (currentParent != null)
			{
				if (currentParent is OwlcatProperty propertyParent)
					propertyParent.OnChildPropertyChanged(this, property);
                
				currentParent = currentParent.parent;
			}
		}

		protected virtual void OnChildPropertyChanged(OwlcatProperty caller, SerializedProperty property)
		{
			UpdateTitle();
		}

		protected override void OnIsExpandedChanged()
		{
			base.OnIsExpandedChanged();
			CreateContent();
		}
		
		public bool TryHandle(KeyDownEvent evt)
		{
			foreach (var handler in m_InputHandlers)
			{
				handler.TryHandle(evt);
				if (evt.isPropagationStopped)
					return true;
			}

			return false;
		}

		public bool IsClosestParentOf(VisualElement element)
		{
			var currentParent = element.parent;
			while (currentParent != null)
			{
				if (currentParent is OwlcatProperty owlcatParent && owlcatParent != this)
					return false;
                
				if (currentParent == this)
					return true;
                
				currentParent = currentParent.parent;
			}
            
			return false;
		}
	}
}
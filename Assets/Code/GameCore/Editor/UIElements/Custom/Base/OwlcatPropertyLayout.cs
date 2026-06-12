using System;
using Code.Framework.Editor.Utility;
using Kingmaker.Editor.UIElements.Custom.Elements;
using Kingmaker.Utility.EditorPreferences;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Base
{
	public class OwlcatPropertyLayout : OwlcatVisualElement
	{
		public enum Layout
		{
			Horizontal = 0,
			Vertical = 1,
			VerticalNotExpandable = 2
		}

		public readonly VisualElement HeaderContainer;
		public readonly VisualElement HeaderContentContainer;  // To be able to use header space in vertical layout
		public readonly OwlcatContentContainer ContentContainer;
		public readonly VisualElement ControlsContainer;

		public readonly OwlcatTitleLabel TitleLabel;
		
		public bool Expandable { get; private set; }
        
		private readonly Layout m_Layout;
		
		private Image m_Collapsed;
		private Image m_Expanded;
		
		private bool m_IsExpanded;
		private bool m_ArrowHidden;
		
		private VisualElement m_HeaderAndControls;
		
		public VisualElement ContextMenuHeader => m_HeaderAndControls ?? HeaderContainer;
		
		public virtual bool IsExpanded
		{
			get => m_IsExpanded;
			set
			{
				m_IsExpanded = value;
				OnIsExpandedChangedInternal();
			}
		}
		
		public VisualElement OverridableControlContainer { get; private set; }

		public override bool canGrabFocus
			=> true;

		/// <summary>
		/// Enable HeaderContent as label and HeaderContentContainer as content,
		/// matching property layout of label:40% and content:60%
		/// Common use is to make foldout header act as a property to save space
		/// </summary>
		public bool HeaderAsPropertyLayout
		{
			set
			{
				if (value)
				{
					HeaderContainer.AddToClassList("owlcat-title-label");
					HeaderContentContainer.style.display = DisplayStyle.Flex;
				}
				else
				{
					HeaderContainer.RemoveFromClassList("owlcat-title-label");
					HeaderContentContainer.style.display = DisplayStyle.None;
				}
			}
		}
		
		public OwlcatPropertyLayout(Layout layout, bool isExpanded = false)
		{
			m_Layout = layout;
			m_IsExpanded = isExpanded;
			focusable = true;
			
			AddToClassList("owlcat-property");

			HeaderContainer = new VisualElement { name = "header" };
			HeaderContentContainer = new VisualElement
			{
				name = "header_content",
				style =
				{
					flexDirection = FlexDirection.Row,
					alignItems = Align.Center,
					width = Length.Percent(60),
				}
			};
			ContentContainer = new OwlcatContentContainer { name = "content" };
			ControlsContainer = new VisualElement { name = "controls" };
			
			HeaderContainer.AddToClassList("owlcat-property-header");
			HeaderContentContainer.AddToClassList("owlcat-property-content");
			HeaderAsPropertyLayout = false;
			ContentContainer.AddToClassList("owlcat-property-content");
			ControlsContainer.AddToClassList("owlcat-property-controls");
			
			TitleLabel = new OwlcatTitleLabel(this);
			TitleLabel.style.flexGrow = 1;
			TitleLabel.style.flexShrink = 1;
			TitleLabel.AddToClassList("focusable-title-label");
			TitleLabel.RemoveFromClassList("owlcat-title-label");
			HeaderContainer.Add(TitleLabel);
			HeaderContainer.Add(new OwlcatTitleLabelSizeControl());

			switch (layout)
			{
				case Layout.Horizontal:
					MakeHorizontalLayout();
					break;
				case Layout.Vertical:
				case Layout.VerticalNotExpandable:
					MakeVerticalLayout();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			
			RegisterCallbackOnce<AttachToPanelEvent>(OnAttachToPanel);
			RegisterCallbackOnce<DetachFromPanelEvent>(OnDetachFromPanel);
		}
		
		private void CreateExpander()
		{
			Image CreateExpandImage(Texture2D texture)
				=> new Image
				{
					image = texture, 
					//tintColor = Color.black,
					scaleMode = ScaleMode.StretchToFill,
					focusable = true,
					style = { maxHeight = 14, maxWidth = 14}
				};

			m_Collapsed = CreateExpandImage(UIElementsResources.FoldoutCollapsed);
			HeaderContainer.hierarchy.Insert(0, m_Collapsed);
			
			m_Expanded = CreateExpandImage(UIElementsResources.FoldoutExpanded);
			HeaderContainer.hierarchy.Insert(0, m_Expanded);

			m_Collapsed.RegisterCallback<MouseDownEvent>(evt => SwitchExpanded(evt, false),
				VisualElementEx.InvokePolicy.IncludeDisabled);
                
			m_Expanded.RegisterCallback<MouseDownEvent>(evt => SwitchExpanded(evt, false),
				VisualElementEx.InvokePolicy.IncludeDisabled);
            
			if (!EditorPreferences.Instance.Scriptwriter)
			{
				HeaderContainer.RegisterCallback<MouseDownEvent>(evt => SwitchExpanded(evt, true),
					VisualElementEx.InvokePolicy.IncludeDisabled);
			}
		}

		private void SetExpandableStyle()
		{
			AddToClassList("owlcat-expandable");
			HeaderContainer.AddToClassList("owlcat-expandable");
			ContentContainer.AddToClassList("owlcat-left-border-highlight");
			ContentContainer.AddToClassList("owlcat-expandable");
			ControlsContainer.AddToClassList("owlcat-expandable");
		}

		private void SwitchExpanded(MouseDownEvent evt, bool doubleClick)
		{
			bool clickCountCheck = doubleClick ? evt.clickCount == 2 : evt.clickCount == 1;
			if (evt.button == 0 && clickCountCheck)
			{
				IsExpanded = !IsExpanded;
				if (m_ArrowHidden) 
					IsExpanded = false;
                
				if (evt.altKey)
				{
					SetExpandedToChildren(this, IsExpanded);
				}
				
				evt.StopPropagation();
			}
		}

		private void SetExpandedToChildren(VisualElement visualElement, bool isExpanded)
		{
			foreach (var ve in visualElement.Children())
			{
				if (ve is OwlcatPropertyLayout owlcatPropertyLayout)
				{
					owlcatPropertyLayout.IsExpanded = isExpanded;
				}
				
				SetExpandedToChildren(ve, isExpanded);
			}
		}

		private void MakeHorizontalLayout()
		{
			TitleLabel.style.flexGrow = 0;
			HeaderContainer.AddToClassList("owlcat-title-label");
			ContentContainer.style.alignItems = Align.Center;
			ContentContainer.style.width = Length.Percent(60);
			Add(HeaderContainer);
			Add(ContentContainer);
			Add(ControlsContainer);
			Add(OverridableControlContainer = new VisualElement { name = "OverridableControlContainer" });
		}

		private void MakeVerticalLayout()
		{
			if (m_Layout == Layout.Vertical)
			{
				Expandable = true;
				SetExpandableStyle();
				CreateExpander();
			}
			
			style.flexDirection = FlexDirection.Column;
			ContentContainer.style.flexDirection = FlexDirection.Column;
			m_HeaderAndControls = new VisualElement
			{
				name = "HeaderAndControls",
				style = { flexDirection = FlexDirection.Row }
			};
			Add(m_HeaderAndControls);
			
			m_HeaderAndControls.Add(HeaderContainer);
			m_HeaderAndControls.Add(HeaderContentContainer);
			m_HeaderAndControls.Add(ControlsContainer);
			m_HeaderAndControls.Add(OverridableControlContainer = new VisualElement { name = "OverridableControlContainer" });
			
			Add(ContentContainer);

			ControlsContainer.style.flexGrow = 1;
			TitleLabel.style.width = new StyleLength(StyleKeyword.Auto);
		}

		private void OnAttachToPanel(AttachToPanelEvent evt)
		{
			OnIsExpandedChangedInternal();
			OnAttachToPanelInternal(evt);
			OnAfterAttachToPanelInternal();
		}

		protected virtual void OnAttachToPanelInternal(AttachToPanelEvent evt) { }
		
		protected virtual void OnAfterAttachToPanelInternal() { }

		protected virtual void OnDetachFromPanel(DetachFromPanelEvent evt) { }

		protected virtual void OnIsExpandedChanged() { }

		protected void OnIsExpandedChangedInternal()
		{
			if (!Expandable)
			{
				return;
			}

			UpdateExpandableVisibility();
			OnIsExpandedChanged();
		}

		protected void UpdateExpandableVisibility()
		{
			ContentContainer.style.display = IsExpanded ? DisplayStyle.Flex : DisplayStyle.None;
			m_Expanded.style.display = IsExpanded ? DisplayStyle.Flex : DisplayStyle.None;
			m_Collapsed.style.display = IsExpanded ? DisplayStyle.None : DisplayStyle.Flex;
		}

		protected void HideArrow(bool hide)
		{
			if (m_Expanded == null || m_Collapsed == null)
				return;

			m_ArrowHidden = hide;
            
			if (hide)
			{
				IsExpanded = false;
				m_Expanded.style.display = DisplayStyle.None;
				m_Collapsed.style.display = DisplayStyle.None;
			}
			else
			{
				m_Expanded.style.display = IsExpanded ? DisplayStyle.Flex : DisplayStyle.None;
				m_Collapsed.style.display = IsExpanded ? DisplayStyle.Flex : DisplayStyle.None;
			}
		}
	}
}
using System.Collections.Generic;
using Code.Editor.Utility;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Elements;
using Kingmaker.Editor.UIElements.Custom.PropertyComponents;
using Kingmaker.Editor.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Prototypable
{
	public class OverridablePropertyControl : OwlcatVisualElement
	{
		private readonly OwlcatProperty m_PropertyElement;
		private readonly List<VisualElement> m_Additional = new();

		private readonly Button m_OverrideButton;
		private readonly Button m_RevertButton;
		private bool m_Disabled;
		private RobustSerializedProperty m_Rsp;
		private RobustSerializedProperty m_ComponentProperty;

		private SerializedProperty m_Property
		{
			get
			{
				if (m_ComponentProperty == null)
				{
					return m_Rsp.Property;
				}
				else
				{
					var value = FieldFromProperty.GetFieldValue(m_ComponentProperty) as BlueprintComponent;
					var wrapper = BlueprintComponentEditorWrapper.Wrap(value);
					var so = new SerializedObject(wrapper);
					return so.FindProperty("Component" + m_Rsp.Property.propertyPath.Substring(
						m_ComponentProperty.Property.propertyPath.Length));
				}
			}
		}

		public OverridablePropertyControl(OwlcatProperty property)
		{
			m_PropertyElement = property;
			m_Rsp = property.Property;

			m_OverrideButton = new OwlcatSmallButton(SwitchOverridenState) { text = string.Empty };
			m_OverrideButton.Add(new Image
				{ image = UIElementsResources.OverrideIcon, scaleMode = ScaleMode.ScaleToFit });

			m_RevertButton = new OwlcatSmallButton(SwitchOverridenState) { text = string.Empty };
			m_RevertButton.Add(new Image
				{ image = UIElementsResources.RevertOverrideIcon, scaleMode = ScaleMode.ScaleToFit });

			Add(m_OverrideButton);
			Add(m_RevertButton);

			UpdateView();

			property.AddManipulator(new ContextualMenuManipulator(evt =>
			{
				if (HasOverrideOption())
				{
					string label = !IsOverridden() ? "Override" : "Revert";
					evt.TryAddSeparator();
					evt.menu.AppendAction(label, x => SwitchOverridenState());
					evt.StopPropagation();
				}
			}));
		}

		public void OnOverrideStateChanged()
		{
			UpdateView();
		}

		public void SwitchOverridenState()
		{
			SetOverridden(!IsOverridden());
			UpdateView();
		}

		private void UpdateView()
		{
			bool overriden = IsOverridden();
			bool isOverridable = HasOverrideOption();

			m_RevertButton.style.display = overriden ? DisplayStyle.Flex : DisplayStyle.None;
			m_OverrideButton.style.display = overriden ? DisplayStyle.None : DisplayStyle.Flex;

			style.display = isOverridable ? DisplayStyle.Flex : DisplayStyle.None;

			bool enabled;
			if (isOverridable)
				enabled = overriden && !m_PropertyElement.HasComponent<ReadOnlyComponent>();
			else
				enabled = !m_PropertyElement.HasComponent<ReadOnlyComponent>();

			m_PropertyElement.ContentContainer.SetEnabled(enabled);
			m_PropertyElement.ControlsContainer.SetEnabled(enabled);
			foreach (var additional in m_Additional)
				additional.SetEnabled(enabled);
		}

		private void SetOverridden(bool overridden)
		{
			PrototypedObjectEditorUtility.SetOverridden(overridden, m_Property);
		}

		public bool HasOverrideOption()
		{
			if (m_Disabled)
				return false;

			if (m_PropertyElement.name == "Blueprint")
				return false;

			if (m_Property.IsArrayElement())
				return false;

			if (PrototypedObjectEditorUtility.IsOverrideDraw(m_Property))
				return PrototypedObjectEditorUtility.HasOverrideOption(m_Property);

			return false;
		}

		public bool IsOverridden()
		{
			return HasOverrideOption() && PrototypedObjectEditorUtility.IsOverridden(m_Property);
		}

		public void AddAdditional(VisualElement additional)
		{
			m_Additional.Add(additional);
			UpdateView();
		}

		public void ChangeProperty(SerializedProperty property)
		{
			m_Rsp = property;
			UpdateView();
		}

		public void SetComponent(SerializedProperty componentProperty)
		{
			m_ComponentProperty = componentProperty;
			UpdateView();
		}

		public void Disable()
		{
			m_Disabled = true;
			UpdateView();
		}
	}
}
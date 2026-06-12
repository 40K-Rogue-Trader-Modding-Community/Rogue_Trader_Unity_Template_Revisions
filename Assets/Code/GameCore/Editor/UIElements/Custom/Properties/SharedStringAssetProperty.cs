using Kingmaker.Code.Editor.Utility;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.Utility;
using Kingmaker.Localization;
using Kingmaker.Localization.Shared;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Properties
{
	public class SharedStringAssetProperty : OwlcatProperty
	{
		private readonly OwlcatVisualElement m_newButton;
		private readonly OwlcatVisualElement m_clearButton;

		private readonly ObjectField m_RefField;
		private LocalizedStringProperty m_LocProp;

		public SharedStringAssetProperty(SerializedProperty property) : base(property, Layout.Vertical)
		{
			var asset = PropertyResolver.GetPropertyObject<SharedStringAsset>(property);
			m_RefField = new ObjectField
			{
				objectType = typeof(SharedStringAsset),
				value = asset,
				style =
				{
					flexGrow = 1,
					flexShrink = 1,
				},
				bindingPath = property.propertyPath,
			};
			m_RefField.RegisterValueChangedCallback(changeEvent => UpdateView(changeEvent.newValue as SharedStringAsset));
			HeaderContentContainer.Add(m_RefField);

			m_newButton = BlueprintReferenceProperty.CreateSmallTextButton("new", CreateNew);
			m_clearButton = BlueprintReferenceProperty.CreateSmallTextButton("clear", ClearRef);
			HeaderContentContainer.Add(m_newButton);
			HeaderContentContainer.Add(m_clearButton);
			HeaderAsPropertyLayout = true;

			this.Bind(property.serializedObject);
			
			UpdateView(asset);
			UpdateExpanded();
		}
		
		private void ClearRef()
		{
			m_RefField.value = null;
		}

		private void CreateNew()
		{
#if UNITY_EDITOR && EDITOR_FIELDS
			var createAttr = Property.GetFieldInfo().GetAttribute<StringCreateWindowAttribute>();
			SharedStringAssetPropertyDrawer.ShowCreator(Property, createAttr, shared =>
			{
				m_RefField.value = shared;
			});
#endif
		}

		private void UpdateView(SharedStringAsset newValue)
		{
			ContentContainer.Clear();  // Just because there only m_LocProp there for now
			m_LocProp = null;

			m_newButton.style.display = newValue == null ? DisplayStyle.Flex : DisplayStyle.None;
			m_clearButton.style.display = newValue == null ? DisplayStyle.None : DisplayStyle.Flex;

			if (newValue == null)
			{
				HideArrow(true);
				return;
			}
			HideArrow(false);

			var strProp = new SerializedObject(newValue).FindProperty(nameof(SharedStringAsset.String));
			m_LocProp = new LocalizedStringProperty(strProp);
			ContentContainer.Add(m_LocProp);

		}

		protected override void OnIsExpandedChanged()
		{
			base.OnIsExpandedChanged();
			UpdateExpanded();
		}

		private void UpdateExpanded()
		{
			if (m_LocProp != null)
				m_LocProp.style.display = IsExpanded ? DisplayStyle.Flex : DisplayStyle.None;
		}
	}
}
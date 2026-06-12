using System;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints.Base;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.Localization;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Elements;
using Kingmaker.Editor.UIElements.ValuePicker;
using Kingmaker.Editor.Utility;
using Kingmaker.Localization;
using Kingmaker.Localization.Enums;
using Kingmaker.Localization.Shared;
using Kingmaker.Utility.EditorPreferences;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Properties
{
	public class LocalizedStringProperty : OwlcatProperty
	{
        private const bool DefaultCommentUnfoldState = false;
        private const bool DefaultVOCommentUnfoldState = false;
        
        private const int DefaultMinLines = 1;
        private const int DefaultMaxLines = 6;
        
		private LocalizedString LocString => PropertyResolver.GetPropertyObject<LocalizedString>(Property);

		private OwlcatTextField m_TextField;

		private VisualElement m_SharedPart;

		private VisualElement m_SharedBtn;
		private VisualElement m_NotSharedPart;
		private VisualElement m_FixUpPart;

		private ObjectField m_SharedField;
		private LocalizedStringCommentProperty m_Comment;
		private LocalizedStringVOCommentProperty m_VOComment;
		private Foldout m_CommentFoldout;
		private Foldout m_VOCommentFoldout;
		private readonly TraitsPartElement m_TraitsPart;

        private bool _isLocaleTextSet;

		public LocalizedStringProperty(SerializedProperty property) : base(property, Layout.Vertical)
		{
#if UNITY_EDITOR && EDITOR_FIELDS
			name = Property.displayName;
			m_TraitsPart = new TraitsPartElement(Property);
			m_TraitsPart.UpdateData();

			AddToClassList("owlcat-box");
			AddToClassList("localizedString");
			TitleLabel.name = "loc_string_title";
			TitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			HeaderContainer.Add(LabelPart());
			ContentContainer.Add(TextField());
			ContentContainer.Add(m_TraitsPart);
			ContentContainer.Add(CommentField());
			ContentContainer.Add(VOCommentField());
			ContentContainer.Add(ButtonsPart());
			CheckSharedState();
#endif
		}

#if UNITY_EDITOR && EDITOR_FIELDS
		private VisualElement CommentField()
		{
            m_Comment = new LocalizedStringCommentProperty(Property, DefaultCommentUnfoldState);
            return m_Comment;
		}
		
		private VisualElement VOCommentField()
        {
            m_VOComment = new LocalizedStringVOCommentProperty(Property, DefaultVOCommentUnfoldState);
            return m_VOComment;
		}

		private static VisualElement LabelPart()
		{
			var root = new VisualElement { style = { flexDirection = FlexDirection.Row } };
			var names = Enum.GetValues(typeof(Locale)).Cast<Locale>().ToList();
			var locPopup = new PopupField<Locale>(names, LocalizationManager.Instance.CurrentLocale);
			locPopup.binding = new PropertyBind<Locale>(
				() => LocalizationManager.Instance.CurrentLocale,
				val => LocalizationManager.Instance.CurrentLocale = val,
				locPopup);

			root.Add(locPopup);
			return root;
		}

		private VisualElement TextField()
		{
			var locale = LocalizationManager.Instance.CurrentLocale;
			string oldText = LocString.GetText(locale);
			m_TextField = new OwlcatTextField(multiline: true, scrollable: true)
			{
				name = "loc_string_text",
				value = oldText, 
				style =
				{
					whiteSpace = WhiteSpace.Normal
				}
			};
            m_TextField.SetMinLines(DefaultMinLines);
            m_TextField.SetMaxLines(DefaultMaxLines);
            
            m_TextField.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
			m_TextField.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanelEvent);

			return m_TextField;
		}

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            _isLocaleTextSet = false;
            m_TextField.RegisterValueChangedCallback(OnTextChanged);
            LocalizationManager.Instance.LocaleChanged += UpdateLocText;
        }
        
        private void OnDetachFromPanelEvent(DetachFromPanelEvent evt)
        {
            LocalizationManager.Instance.LocaleChanged -= UpdateLocText;
        }

        private void OnTextChanged(ChangeEvent<string> evt)
        {
            if (_isLocaleTextSet)
            {
                _isLocaleTextSet = false;
                return;
            }

            if (LocString.UpdateText(Property, LocalizationManager.Instance.CurrentLocale, evt.newValue))
            {
                string propertyName = Property.serializedObject.targetObject.name + "_" + Property.propertyPath;
                string previousValue = evt.previousValue;
                UndoManager.Instance.RegisterUndo(propertyName + " edit", () => 
                {
                    LocString.UpdateText(Property, LocalizationManager.Instance.CurrentLocale, previousValue);
                    m_TextField.SetValueWithoutNotify(previousValue);
                    m_VOComment.UpdateCommentHeader();
                });
					
                m_TraitsPart.UpdateData();
            }
				
            m_VOComment.UpdateCommentHeader();
        }

        private void UpdateLocText(Locale newLoc)
        {
            _isLocaleTextSet = true;
			m_TextField.value = LocString.GetText(newLoc);
            
            if (EditorPreferences.Instance.GdDesigner && newLoc != Locale.dev)
            {
                m_TextField.isReadOnly = true;
                TitleLabel.text = $"{Property.displayName} <color=red>Not editable for GD in {newLoc}</color>";
            }
            else
            {
                m_TextField.isReadOnly = false;
                m_TextField.textSelection.isSelectable = true;
                TitleLabel.text = $"{Property.displayName}";
            }
            
			m_Comment.UpdateCommentHeader();
			m_VOComment.UpdateCommentHeader();
		}

		private VisualElement ButtonsPart()
		{
			var fieldInfo = FieldFromProperty.GetFieldInfo(Property);
			var root = new VisualElement {name = "Button Part", style = {flexDirection = FlexDirection.Row}};

			var sharedBtn = new Button { text = "Set Shared" };
			sharedBtn.clicked += () =>
			{
				AssetPicker.ShowAssetPicker(
					typeof(SharedStringAsset),
					fieldInfo,
					shared =>
					{
						LocString.SetShared(Property, (SharedStringAsset)shared);
						CheckSharedState();
						m_SharedField.value = LocString.Shared;
					}
				);
			};
			m_SharedBtn = sharedBtn;

			m_NotSharedPart = GetNotSharedPart(Property, fieldInfo);
			m_SharedPart = GetSharedPart(Property);

			root.Add(sharedBtn);
			root.Add(m_NotSharedPart);
			root.Add(m_SharedPart);

			var fixButton = new Button {text = "String is broken. Try to fix"};
			fixButton.clicked += () =>
			{
				LocString.Fix(Property);
				CheckSharedState();
			};
			m_FixUpPart = fixButton;
			root.Add(m_FixUpPart);
            
            var openFileButton = new Button {text = "Show File"};
            openFileButton.clicked += () =>
            {
	            string path = LocString.Shared?.String.JsonPath ?? LocString.JsonPath;
                EditorUtility.RevealInFinder(path);
            };
            root.Add(openFileButton);

			return root;
		}

		private VisualElement GetNotSharedPart(SerializedProperty prop, FieldInfo fieldInfo)
		{
			var root = new VisualElement() { name = "NotSharedPart", style = { flexDirection = FlexDirection.Row } };
			var makeShareBtn = new Button() { text = "Make Shared" };
			makeShareBtn.clicked += () =>
			{
				// Please contact chernyshev@owlcat.games if you want to change this behaviour
				SharedStringAssetPropertyDrawer.ShowCreator(prop, fieldInfo.GetAttribute<StringCreateWindowAttribute>(),
					_ =>
					{
						CheckSharedState();
						m_SharedField.value = LocString.Shared;
					});
			};

			var deleteBtn = new Button(() =>
			{
				LocString.ClearData();
				LocString.MarkDirty(prop);
				m_TraitsPart.UpdateData();
				UpdateLocText(LocalizationManager.Instance.CurrentLocale);
				m_VOComment.ClearComment();
			}) { text = "Delete String" };

			root.Add(makeShareBtn);
			root.Add(deleteBtn);

			return root;
		}

		private VisualElement GetSharedPart(SerializedProperty prop)
		{
			var root = new VisualElement {name = "SharedPart", style = {flexDirection = FlexDirection.Row}};

			m_SharedField = new ObjectField
			{
				value = LocString.Shared,
				objectType = typeof(SharedStringAsset),
				allowSceneObjects = false,
				style = { flexGrow = new StyleFloat(1), flexShrink = new StyleFloat(1) }
			};

			var clearShared = new Button(() =>
			{
				LocString.SetShared(prop, null);
				m_SharedField.SetValueWithoutNotify(null);
				CheckSharedState();
			})
			{ text = "Clear Shared" };

			m_SharedField.RegisterValueChangedCallback(e =>
			{
				LocString.Shared = e.newValue as SharedStringAsset;
				m_TextField.value = LocString.GetText(LocalizationManager.Instance.CurrentLocale);
				LocString.MarkDirty(prop);
			});

			root.Add(clearShared);
			root.Add(m_SharedField);
			return root;
		}

		private void CheckSharedState()
		{
			bool isShared = LocString.Shared;
			bool needsFixUp = !LocString.Check(Property);
			bool showShared = !needsFixUp && isShared && Property.serializedObject.targetObject is not SharedStringAsset;
			bool showNotShared = !needsFixUp && !isShared && Property.serializedObject.targetObject is not SharedStringAsset;
			m_SharedBtn.style.display = !needsFixUp && Property.serializedObject.targetObject is not SharedStringAsset ? DisplayStyle.Flex : DisplayStyle.None;
			m_SharedPart.style.display = showShared ? DisplayStyle.Flex : DisplayStyle.None;
			m_NotSharedPart.style.display = showNotShared ? DisplayStyle.Flex : DisplayStyle.None;
			m_FixUpPart.style.display = needsFixUp ? DisplayStyle.Flex : DisplayStyle.None;
			m_TextField.isReadOnly = needsFixUp;
			UpdateLocText(LocalizationManager.Instance.CurrentLocale);
		}
#endif
	}
}
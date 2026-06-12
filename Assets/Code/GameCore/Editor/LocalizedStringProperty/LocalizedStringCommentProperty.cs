using System;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.Localization;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Elements;
using Kingmaker.Editor.Utility;
using Kingmaker.Localization;
using Kingmaker.Localization.Enums;
using UnityEditor;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Properties
{
	public class LocalizedStringCommentProperty : VisualElement
    {
        private const int MinLines = 1;
        private const int MaxLines = 5;

        private const int MaxTitleLabelLength = 40;
        protected virtual string LabelText => "Comment";
        protected virtual string PathText => "Comment";
        
        protected readonly SerializedProperty property;
		protected readonly LocalizedString locString;

        protected OwlcatTextField textField;
        protected Foldout foldout;

		public LocalizedStringCommentProperty(SerializedProperty property, bool isUnfolded)
		{
#if UNITY_EDITOR && EDITOR_FIELDS
            this.property = property;
			locString = PropertyResolver.GetPropertyObject<LocalizedString>(property);

            Initialize(isUnfolded);
#endif
        }

#if UNITY_EDITOR && EDITOR_FIELDS
        public void UpdateCommentHeader()
        {
            UpdateCommentHeader(foldout.value);
        }
        
        private void Initialize(bool isUnfolded)
        {
            foldout = new Foldout { 
                text = LabelText, 
                value = isUnfolded,
                viewDataKey = $"{property.propertyPath}.{PathText}",
                style = { textOverflow = TextOverflow.Ellipsis }
            };

            foldout.AddToClassList("localizedString");
            foldout.style.paddingLeft = 15;
            foldout.Add(TextField());
            foldout.RegisterValueChangedCallback(evt => UpdateCommentHeader(evt.newValue));
            Add(foldout);
            
            CheckSharedState();
            OnLocaleChanged(LocalizationManager.Instance.CurrentLocale);
            UpdateCommentHeader(isUnfolded);

            RegisterCallback<AttachToPanelEvent>(_ => LocalizationManager.Instance.LocaleChanged += OnLocaleChanged);
            RegisterCallback<DetachFromPanelEvent>(_ => LocalizationManager.Instance.LocaleChanged -= OnLocaleChanged);
        }

        protected virtual void OnLocaleChanged(Locale locale)
		{
			textField.value = locString.GetCommentOnCurrentLocale();
			if (locale is Locale.ruRU or Locale.dev)
			{
				textField.isReadOnly = false;
                UpdateCommentHeader();
			}
			else
			{
				textField.isReadOnly = true;
                foldout.text = $"{LabelText} <color=red>Not editable in [{locale}]</color>";
			}
		}

		protected virtual VisualElement TextField()
		{
			string oldText = locString.GetCommentOnCurrentLocale();
			textField = new OwlcatTextField(multiline: true) 
			{ 
				value = oldText, 
				style = 
				{ 
					whiteSpace = WhiteSpace.Normal
				}
			};
            textField.SetMinLines(MinLines);
            textField.SetMaxLines(MaxLines);
			
			textField.RegisterValueChangedCallback(e =>
			{
				if (locString.UpdateComment(property, e.newValue))
				{
					string propName = property.serializedObject.targetObject.name + "_" + property.propertyPath;
					textField.value = e.newValue;
					UndoManager.Instance.RegisterUndo(propName + " comment edit", () =>
					{
						locString.UpdateComment(property, e.previousValue);
						textField.value = locString.GetCommentOnCurrentLocale();
					});
				}
			});

			return textField;
		}
        
        protected virtual void UpdateCommentHeader(bool isUnfolded)
        {
            if (textField.isReadOnly)
                return;
            
            if (isUnfolded)
            {
                foldout.text = LabelText;
            }
            else
            {
                string input = GetCommentOnCurrentLocale();
                if (string.IsNullOrEmpty(input))
                {
                    foldout.text = $"{LabelText} [{LocalizationManager.Instance.CurrentLocale}] (Empty)";
                }
                else
                {
                    string comment = $"{LabelText} [{LocalizationManager.Instance.CurrentLocale}]  {input}";
                    bool neededEllipsis = comment.Length > MaxTitleLabelLength;
                    foldout.text = comment.Substring(0, Math.Min(MaxTitleLabelLength, comment.Length)) + 
                                    (neededEllipsis ? "..." : string.Empty);
                }
            }
        }

        protected virtual string GetCommentOnCurrentLocale() => locString.GetCommentOnCurrentLocale();

        private void CheckSharedState()
		{
			bool needsFixUp = !locString.Check(property);
			textField.isReadOnly = needsFixUp;
		}
#endif
	}
}
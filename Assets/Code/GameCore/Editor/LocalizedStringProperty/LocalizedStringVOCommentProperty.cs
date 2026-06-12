using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.Localization;
using Kingmaker.Editor.UIElements.Custom.Elements;
using Kingmaker.Localization;
using Kingmaker.Localization.Enums;
using UnityEditor;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Properties
{
	public class LocalizedStringVOCommentProperty : LocalizedStringCommentProperty
    {
        private const int MinLines = 1;
        private const int MaxLines = 6;
        
        protected override string LabelText => "VO Comment";
        protected override string PathText => "VOComment";
        
		public LocalizedStringVOCommentProperty(SerializedProperty property, bool isUnfolded) : 
            base(property, isUnfolded)
		{ }

#if UNITY_EDITOR && EDITOR_FIELDS
        public void ClearComment()
        {
            textField.value = string.Empty;
            UpdateCommentHeader();
        }
        
        protected override void OnLocaleChanged(Locale locale)
		{
			textField.value = locString.GetVOCommentOnCurrentLocale();
            UpdateCommentHeader();
			textField.isReadOnly = false;
		}

        protected override VisualElement TextField()
		{
			string oldText = locString.GetVOCommentOnCurrentLocale();
            foldout.name = "vo_comment_title";
			textField = new OwlcatTextField(multiline: true, scrollable: true)
				{ 
					name = "vo_comment",
					value = oldText, 
					style =
					{
						whiteSpace = WhiteSpace.PreWrap,
						marginRight = 5,
					}
				};
            textField.SetMinLines(MinLines);
            textField.SetMaxLines(MaxLines);
			
			textField.RegisterValueChangedCallback(e =>
			{
				if (locString.UpdateComment(property, e.newValue, true))
				{
					string propName = property.serializedObject.targetObject.name + "_" + property.propertyPath;
					textField.value = e.newValue;
					UndoManager.Instance.RegisterUndo(propName + " vo comment edit", () =>
					{
						locString.UpdateComment(property, e.previousValue, true);
						textField.value = locString.GetVOCommentOnCurrentLocale();
					});
				}

                UpdateCommentHeader();
			});

            UpdateCommentHeader();
			return textField;
		}
        
        protected override void UpdateCommentHeader(bool isUnfolded)
        {
            if (LocalizedString.Dereference(locString).IsTrulyEmpty)
            {
                foldout.text = "VO Comment <color=red>Not editable for empty string</color>";
                textField.isReadOnly = true;
                return;
            }
			
            textField.isReadOnly = false;
            base.UpdateCommentHeader(isUnfolded);
        }
        
        protected override string GetCommentOnCurrentLocale() => locString.GetVOCommentOnCurrentLocale();
#endif
	}
}
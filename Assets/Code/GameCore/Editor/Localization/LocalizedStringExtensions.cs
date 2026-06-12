using System;
using JetBrains.Annotations;
using Kingmaker.Localization;
using Kingmaker.Localization.Enums;
using Kingmaker.Localization.Shared;

namespace Kingmaker.Editor.Localization
{
	public static class LocalizedStringExtensions
	{
#if EDITOR_FIELDS
		[NotNull]
		public static string[] GetLocaleTraits(this LocalizedString str, Locale locale)
		{
			try
			{
				var data = str.GetData();
				return data != null ? data.GetTraits(locale) : Array.Empty<string>();
			}
			catch
			{
				return Array.Empty<string>();
			}
		}
        
		public static string[] GetStringTraits(this LocalizedString str)
		{
			try
			{
				var data = str.GetData();
				return data != null ? data.GetStringTraits() : Array.Empty<string>();
			}
			catch
			{
				return Array.Empty<string>();
			}
		}

		public static string GetCommentOnCurrentLocale(this LocalizedString str)
		{
			try
			{
				var data = str.GetData();
				var ld = data?.GetOrCreateLocaleData(LocalizationManager.Instance.CurrentLocale);
				if (ld == null)
					return string.Empty;
				var (stringPart, _) = LocalizedStringCommentSections.Parse(ld.TranslationComment);
				return stringPart;
			}
			catch
			{
				return string.Empty;
			}
		}
		
		public static string GetVOCommentOnCurrentLocale(this LocalizedString str)
		{
			try
			{
				var data = str.GetData();
				var ld = data?.GetOrCreateLocaleData(LocalizationManager.Instance.CurrentLocale);
				return ld != null ? 
					ld.VOComment : 
					string.Empty;
			}
			catch
			{
				return string.Empty;
			}
		}
#endif		
	}
}
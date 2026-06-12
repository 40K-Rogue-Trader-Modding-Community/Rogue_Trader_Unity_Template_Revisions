using System;
using System.Globalization;
using UnityEditor;

namespace Kingmaker.Editor.UIElements
{
    [InitializeOnLoad]
    public class UiElementsEnabler
    {
        private const string PrefsKey = "NewEditorEnableTime";

        static UiElementsEnabler()
        {
            string lastTime = EditorPrefs.GetString(PrefsKey);
            var time = DateTime.TryParse(lastTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
                ? result
                : DateTime.MinValue;

            if (DateTime.Now.Date - time.Date >= TimeSpan.FromDays(1))
            {
                EditorPrefs.SetString(PrefsKey, DateTime.Now.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Editor.GDUtilsConverters;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Localization;

namespace Assets.Editor
{
    public static class GDUtils
    {
         // excel-related
        public const string ROW_SEPARATOR = "\r\n";
        public const string COLUMN_SEPARATOR = "\t";

        private static List<IExcelConverter> _converters = new() 
        { 
            new BlueprintMechanicEntityFactExcelConverter(),
            new BlueprintEncyclopediaPageExcelConverter()
        };
        
        public static string FormExcelStringsForLocalization(string[] bpGuids)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string guid in bpGuids)
            {
                string resultString = FormExcelStringForLocalization(guid);
                if (string.IsNullOrEmpty(resultString.Replace(COLUMN_SEPARATOR, "").Replace("\"", "")))
                {
                    PFLog.DesignerDebug.Error($"found empty blueprint entry, skip");
                    continue;
                }
                sb.Append(resultString);
                sb.Append(ROW_SEPARATOR);
            }
            
            return sb.ToString();
        }

        public static string FormExcelStringForLocalization(string guid)
        {
            string res = string.Empty;
            
            var sbp = BlueprintsDatabase.LoadById<SimpleBlueprint>(guid);
            if (sbp == null)
                return res;

            if (TryGetConverter(sbp.GetType(), out var converter))
                res = converter.FormExcelStringForLocalization(sbp);
            
            return res;
        }

        private static bool TryGetConverter(Type type, out IExcelConverter converter)
        {
            converter = _converters.FirstOrDefault(c => c.Type.IsAssignableFrom(type));
            return converter != null;
        }

        public static string GetJsonPathFor(LocalizedString localizedString, string bpName)
        {
#if UNITY_EDITOR && EDITOR_FIELDS

            if (localizedString.Shared == null)
            {
                PFLog.DesignerDebug.Error($"for blueprint [{bpName}] Shared string not set!");
                return localizedString.JsonPath;
            }
            return localizedString.Shared.String.JsonPath;
#endif
            return string.Empty;
        }

        public static string SanitizeString(string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;
            
            return str.Replace("\"", "\"\"").Trim();
        }
    }
}
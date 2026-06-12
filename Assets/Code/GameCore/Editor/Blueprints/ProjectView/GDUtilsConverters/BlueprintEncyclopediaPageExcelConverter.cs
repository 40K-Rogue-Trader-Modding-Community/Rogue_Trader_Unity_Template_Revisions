using System;
using System.Text;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Encyclopedia;
using Kingmaker.Blueprints.Encyclopedia.Blocks;
using Kingmaker.Localization.Enums;

namespace Assets.Editor.GDUtilsConverters
{
    public class BlueprintEncyclopediaPageExcelConverter : IExcelConverter
    {
        public Type Type => typeof(BlueprintEncyclopediaPage);
        
        public string FormExcelStringForLocalization(SimpleBlueprint sbp)
        {
            if (sbp is not BlueprintEncyclopediaPage bp)
            {
                PFLog.DesignerDebug.Error($"Selected bp [{sbp.NameSafe()}] is not a {nameof(BlueprintEncyclopediaPage)}.");
                return string.Empty;
            }
            
            StringBuilder sb = new StringBuilder();
            
#if UNITY_EDITOR && EDITOR_FIELDS
            sb.Append(bp.AssetName);
            sb.Append(GDUtils.COLUMN_SEPARATOR);
            
            sb.Append(GDUtils.GetJsonPathFor(bp.Title, bp.AssetName));
            sb.Append(GDUtils.COLUMN_SEPARATOR);
            sb.Append($"{GDUtils.SanitizeString(bp.Title.GetText(Locale.dev))}");
            
            sb.Append(GDUtils.COLUMN_SEPARATOR);
            //edited text en
            sb.Append(GDUtils.COLUMN_SEPARATOR);
            //edited text ru
            sb.Append(GDUtils.COLUMN_SEPARATOR);
            //approve ND
            sb.Append(GDUtils.COLUMN_SEPARATOR);
            //approve GD
            sb.Append(GDUtils.COLUMN_SEPARATOR);

            if (bp is BlueprintEncyclopediaGlossaryEntry bpGlossaryEntry)
            {
                sb.Append(GDUtils.GetJsonPathFor(bpGlossaryEntry.Description, bp.AssetName));
                sb.Append(GDUtils.COLUMN_SEPARATOR);
                sb.Append($"\"{GDUtils.SanitizeString(bpGlossaryEntry.Description.GetText(Locale.dev))}\"");
            }
            
            bool isFirstBlock = true;
            foreach (var block in bp.Blocks)
            {
                if (block is BlueprintEncyclopediaBlockText bpBlockText)
                {
                    if (!isFirstBlock)
                    {
                        sb.Append(GDUtils.ROW_SEPARATOR);
                        sb.Append(GDUtils.COLUMN_SEPARATOR);
                        sb.Append(GDUtils.COLUMN_SEPARATOR);
                        sb.Append(GDUtils.COLUMN_SEPARATOR);
                        sb.Append(GDUtils.COLUMN_SEPARATOR);
                        sb.Append(GDUtils.COLUMN_SEPARATOR);
                        sb.Append(GDUtils.COLUMN_SEPARATOR);
                        sb.Append(GDUtils.COLUMN_SEPARATOR);
                    }

                    sb.Append(GDUtils.GetJsonPathFor(bpBlockText.Text, bpBlockText.name));
                    sb.Append(GDUtils.COLUMN_SEPARATOR);
                    sb.Append($"\"{GDUtils.SanitizeString(bpBlockText.Text.GetText(Locale.dev))}\"");
                    isFirstBlock = false;
                }
            }
#endif
            
            return sb.ToString();
        }
    }
}
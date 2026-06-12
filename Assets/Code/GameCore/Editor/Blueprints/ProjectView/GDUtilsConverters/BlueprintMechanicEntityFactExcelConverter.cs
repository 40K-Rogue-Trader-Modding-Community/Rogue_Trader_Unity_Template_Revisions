using System;
using System.Collections.Generic;
using System.Text;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Localization.Enums;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Blueprints;
using Kingmaker.UnitLogic.UI;

namespace Assets.Editor.GDUtilsConverters
{
    public class BlueprintMechanicEntityFactExcelConverter : IExcelConverter
    {
        public Type Type => typeof(BlueprintMechanicEntityFact);

        private static readonly Dictionary<Type, Action<StringBuilder, BlueprintComponent>> _componentConverters = new()
        {
            { typeof(UIPropertiesComponent), ConvertUIProperties },
            { typeof(AddStringToFactName), ConvertAddStringToFact },
            { typeof(AddStringToFactDescription), ConvertAddStringToFact },
            { typeof(AdditionalDescriptionComponent), ConvertAdditionalDescription }
        };
        
        public string FormExcelStringForLocalization(SimpleBlueprint sbp)
        {
            if (sbp is not BlueprintMechanicEntityFact bp)
            {
                PFLog.DesignerDebug.Error($"Selected bp [{sbp.NameSafe()}] is not a {nameof(BlueprintMechanicEntityFact)}.");
                return string.Empty;
            }
            
            StringBuilder sb = new StringBuilder();
            
#if UNITY_EDITOR && EDITOR_FIELDS
            sb.Append(bp.AssetName);
            sb.Append(GDUtils.COLUMN_SEPARATOR);

            if (sbp is BlueprintUnit bpUnit)
            {
                sb.Append(GDUtils.GetJsonPathFor(bpUnit.LocalizedName.String, bp.AssetName));
                sb.Append(GDUtils.COLUMN_SEPARATOR);
                sb.Append($"{GDUtils.SanitizeString(bpUnit.LocalizedName.String.GetText(Locale.dev))}");
            }
            else
            {
                sb.Append(GDUtils.GetJsonPathFor(bp.LocalizedName, bp.AssetName));
                sb.Append(GDUtils.COLUMN_SEPARATOR);
                sb.Append($"{GDUtils.SanitizeString(bp.LocalizedName.GetText(Locale.dev))}");
            }
            
            sb.Append(GDUtils.COLUMN_SEPARATOR);
            //edited text en
            sb.Append(GDUtils.COLUMN_SEPARATOR);
            //edited text ru
            sb.Append(GDUtils.COLUMN_SEPARATOR);
            //approve ND
            sb.Append(GDUtils.COLUMN_SEPARATOR);
            //approve GD
            sb.Append(GDUtils.COLUMN_SEPARATOR);
            sb.Append(GDUtils.GetJsonPathFor(bp.LocalizedDescription, bp.AssetName));
            sb.Append(GDUtils.COLUMN_SEPARATOR);
            sb.Append($"\"{GDUtils.SanitizeString(bp.LocalizedDescription.GetText(Locale.dev))}\"");

            foreach (var component in bp.ComponentsArray)
            {
                if (_componentConverters.TryGetValue(component.GetType(), out var converter))
                {
                    sb.Append(GDUtils.ROW_SEPARATOR);
                    sb.Append(GDUtils.COLUMN_SEPARATOR);
                    sb.Append(GDUtils.COLUMN_SEPARATOR);
                    sb.Append(GDUtils.COLUMN_SEPARATOR);
                    sb.Append(GDUtils.COLUMN_SEPARATOR);
                    sb.Append(GDUtils.COLUMN_SEPARATOR);
                    sb.Append(GDUtils.COLUMN_SEPARATOR);
                    sb.Append(GDUtils.COLUMN_SEPARATOR);
                    
                    converter(sb, component);
                }
            }
#endif
            
            return sb.ToString();
        }

        private static void ConvertUIProperties(StringBuilder sb, BlueprintComponent component)
        {
#if UNITY_EDITOR && EDITOR_FIELDS
            if (component is not UIPropertiesComponent uiPropertiesComponent)
                return;
            
            for (int i = 0; i < uiPropertiesComponent.Properties.Length; i++)
            {
                var property = uiPropertiesComponent.Properties[i];
                if (i > 0)
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

                sb.Append(GDUtils.GetJsonPathFor(property.Description, $"{component.OwnerBlueprint.AssetName}:propertyComponent"));
                sb.Append(GDUtils.COLUMN_SEPARATOR);
                sb.Append($"\"{GDUtils.SanitizeString(property.Description.GetText(Locale.dev))}\"");
            }
#endif
        }
        
        private static void ConvertAddStringToFact(StringBuilder sb, BlueprintComponent component)
        {
#if UNITY_EDITOR && EDITOR_FIELDS
            if (component is not AddStringToFact addStringComponent)
                return;
            
            sb.Append(GDUtils.GetJsonPathFor(addStringComponent.AdditionalStringLocalized, 
                $"{component.OwnerBlueprint.AssetName}:addStringToFact"));
            sb.Append(GDUtils.COLUMN_SEPARATOR);
            sb.Append($"\"{GDUtils.SanitizeString(addStringComponent.AdditionalStringLocalized.GetText(Locale.dev))}\"");
#endif
        }
        
        private static void ConvertAdditionalDescription(StringBuilder sb, BlueprintComponent component)
        {
#if UNITY_EDITOR && EDITOR_FIELDS
            if (component is not AdditionalDescriptionComponent additionalDescriptionComponent)
                return;
            
            sb.Append(GDUtils.GetJsonPathFor(additionalDescriptionComponent.AdditionalDescription, 
                $"{component.OwnerBlueprint.AssetName}:additionalDescription"));
            sb.Append(GDUtils.COLUMN_SEPARATOR);
            sb.Append($"\"{GDUtils.SanitizeString(additionalDescriptionComponent.AdditionalDescription.GetText(Locale.dev))}\"");
#endif
        }
    }
}
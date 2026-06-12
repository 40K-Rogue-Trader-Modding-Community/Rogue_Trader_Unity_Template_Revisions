using System;
using Kingmaker.Blueprints;

namespace Assets.Editor.GDUtilsConverters
{
    public interface IExcelConverter
    {
        Type Type { get; }
        
        string FormExcelStringForLocalization(SimpleBlueprint sbp);
    }
}
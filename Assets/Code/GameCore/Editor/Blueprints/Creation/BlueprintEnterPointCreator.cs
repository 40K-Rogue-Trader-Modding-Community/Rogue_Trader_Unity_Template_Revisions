using System.IO;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class BlueprintEnterPointCreator : AssetCreatorBase
	{
		public BlueprintAreaReference Area;
		public BlueprintAreaPartReference AreaPart;

		public override string CreatorName => "Area Enter Point";
		public override string LocationTemplate => "{Area_Path}/EnterPoints/{AreaPart}_{name}.asset";

        public override object CreateAsset()
        {
            return new BlueprintAreaEnterPoint();
        }
        
        public override void PostProcess(object asset)
        {
	        var enterPoint = asset as BlueprintAreaEnterPoint;
	        if (enterPoint == null)
	        {
		        return;
	        }

	        enterPoint.Area = Area;
	        enterPoint.AreaPart = AreaPart == null ? Area : AreaPart;
        }

		protected override string GetAdditionalTemplate(string propName)
		{
			if (propName == "Area_Path")
			{
				if (Area.Get() is { } area)
				{
					return Path.GetDirectoryName(BlueprintsDatabase.GetAssetPath(area));
				}
			}

			if (propName == "AreaPart")
			{
				if (AreaPart.Get() is { } areaPart)
				{
					return Path.GetFileNameWithoutExtension(BlueprintsDatabase.GetAssetPath(areaPart));
				}
				if (Area.Get() is { } area)
				{
					return Path.GetFileNameWithoutExtension(BlueprintsDatabase.GetAssetPath(area));
				}
			}
			return base.GetAdditionalTemplate(propName);
		}
	}
}
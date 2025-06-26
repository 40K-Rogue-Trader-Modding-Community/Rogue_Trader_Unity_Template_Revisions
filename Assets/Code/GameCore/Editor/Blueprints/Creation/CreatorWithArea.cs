using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.View;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public abstract class CreatorWithArea : AssetCreatorBase
	{
		public BlueprintAreaReference Area;

		public override void Init()
		{
			TryGetArea();
		}

		private void TryGetArea()
		{
			Area = FindObjectsOfType<AreaEnterPoint>()
				.Select(ep => ep.Blueprint)
				.Where(ep => ep != null)
				.Select(ep => ep.Area)
				.FirstOrDefault(a => a != null)
				.ToReference<BlueprintAreaReference>();;
		}
	}
}
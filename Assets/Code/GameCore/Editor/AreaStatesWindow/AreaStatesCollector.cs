using System.Collections.Generic;
using System.Linq;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;

#nullable enable

namespace Kingmaker.Editor.AreaStatesWindow
{
    public class AreaStatesCollector
    {
        public BlueprintArea Area { get; }

        public AreaStatesCollector(BlueprintArea area)
        {
            Area = area;
        }

        public List<AreaState> Collect()
        {
            // Collect all BlueprintAreaMechanics from Area
            var areaMechanics = BlueprintsDatabase.LoadAllOfType<BlueprintAreaMechanics>()
                .Where(am => am.Area.Guid == Area.AssetGuid)
                .ToArray();

            // Collect all etudes that contains any AddedAreaMechanics with ones from Area
            var etudes = BlueprintsDatabase.LoadAllOfType<BlueprintEtude>()
                .Where(e => e.AddedAreaMechanics
                    .Any(eam => areaMechanics
                        .Any(am => am.AssetGuid == eam.Guid)))
                .ToArray();

            var states = new List<AreaState>(etudes.Length + 1)
            {
                // Add base area state
                new(Area)
            };
            // Add etude-based states
            states.AddRange(etudes.Select(e => new AreaState(Area, e)));
            return states;
        }
    }
}
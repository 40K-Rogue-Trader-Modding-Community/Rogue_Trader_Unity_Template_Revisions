using System.Collections.Generic;
using System.Linq;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;

#nullable enable

namespace Kingmaker.Editor.AreaStatesWindow
{
    public static class EtudeHelper
    {
        /// <summary>
        /// Collects all etudes with their children
        /// </summary>
        public static Dictionary<BlueprintEtude, List<BlueprintEtude>> CollectChildrenMap()
        {
            var allEtudes = BlueprintsDatabase.LoadAllOfType<BlueprintEtude>()
                .ToArray();

            // Init children for the first time
            var result = allEtudes
                .ToDictionary(etude => etude, _ => new List<BlueprintEtude>());

            // Assign children to parents
            foreach (var etude in allEtudes)
            {
                if (etude.Parent?.Get() == null)
                {
                    continue;
                }
                result[etude.Parent.Get()].Add(etude);
            }
            return result;
        }

        /// <summary>
        /// Finds the first children etudes of some parent etude that have
        /// given area linked, starting from given root etude, and takes the one that
        /// suits the most in case there are several of them.
        /// </summary>
        public static List<BlueprintEtude> GetAreaRootCandidates(BlueprintEtude root, BlueprintArea area)
        {
            var childrenMap = CollectChildrenMap();
            return GetAreaRootCandidates(childrenMap, root, area);
        }

        /// <summary>
        /// Use this overload to avoid re-collecting of childrenMap
        /// </summary>
        public static List<BlueprintEtude> GetAreaRootCandidates(
            IReadOnlyDictionary<BlueprintEtude, List<BlueprintEtude>> childrenMap,
            BlueprintEtude root,
            BlueprintArea area)
        {
            var areaRootCandidates = new List<BlueprintEtude>();

            var etudesToVisit = new Stack<BlueprintEtude>();
            etudesToVisit.Push(root);
            while (etudesToVisit.Count > 0)
            {
                var etude = etudesToVisit.Pop();
                if (etude.LinkedAreaPart?.AssetGuid == area.AssetGuid)
                {
                    areaRootCandidates.Add(etude);
                    continue;
                }

                if (!childrenMap.TryGetValue(etude, out var children))
                {
                    // Should never get here, as any etude should have a list of children
                    // (maybe empty one), but who knows..
                    continue;
                }
                children.ForEach(etudesToVisit.Push);
            }

            return areaRootCandidates;
        }

        /// <summary>
        /// Recursively collects all areas linked to etudes, starting from some root etude
        /// </summary>
        public static HashSet<BlueprintArea> CollectLinkedAreas(BlueprintEtude root)
        {
            var childrenMap = CollectChildrenMap();
            return CollectLinkedAreas(childrenMap, root);
        }

        /// <summary>
        /// Use this overload to avoid re-collecting of childrenMap
        /// </summary>
        public static HashSet<BlueprintArea> CollectLinkedAreas(
            IReadOnlyDictionary<BlueprintEtude, List<BlueprintEtude>> childrenMap,
            BlueprintEtude root)
        {
            var result = new HashSet<BlueprintArea>();
            var etudesToVisit = new Stack<BlueprintEtude>();
            etudesToVisit.Push(root);
            while (etudesToVisit.Count > 0)
            {
                var etude = etudesToVisit.Pop();
                if (childrenMap.TryGetValue(etude, out var children))
                {
                    children.ForEach(etudesToVisit.Push);
                }

                if (etude.LinkedAreaPart is BlueprintArea area)
                {
                    result.Add(area);
                }
            }
            return result;
        }
    }
}
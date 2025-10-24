using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.Utility;
using Kingmaker.Utility.DotNetExtensions;
using UnityEditor;
using UnityEngine;

#nullable enable

namespace Kingmaker.Editor.AreaStatesWindow
{
    /// <summary>
    /// Holds the links between areas and their root etude
    /// </summary>
    // [CreateAssetMenu(menuName = "ScriptableObjects/AreaRootEtudes")]
    public class AreaRootEtudes : ScriptableObject
    {
        private const string InstancePath = "Assets/Editor/Naming/" + nameof(AreaRootEtudes) + ".asset";

        [SerializeField]
        [InspectorButton(nameof(UpdateNames), "Update names and sort")]
        public string UpdateNamesButton = "";

        [SerializeField]
        [InspectorButton(nameof(TryFixMissingEtudes), "Try fix missing etudes")]
        public string TryFillEtudesButton = "";

        [SerializeField]
        public BlueprintEtudeReference? AreaRootEtude;

        [Serializable]
        public class AreaEtude
        {
            // Just to have proper array element names
            public string? Name;

            public BlueprintAreaReference? Area;
            public BlueprintEtudeReference? Etude;

        }

        [SerializeField]
        public AreaEtude[] AreaEtudes = {};

        public static AreaRootEtudes? GetInstance()
        {
            return AssetDatabase.LoadAssetAtPath<AreaRootEtudes>(InstancePath);
        }

        public BlueprintEtude? GetAreaRootEtude(BlueprintArea area)
        {
            return AreaEtudes
                .FirstOrDefault(ae => ae.Area?.Get() == area)?.Etude?.Get();
        }

        /// <summary>
        /// Copies name of the Area into Name and sort AreaEtudes by it
        /// </summary>
        public void UpdateNames()
        {
            // Perform some cleanup - remove elements with no area and areas duplicates
            AreaEtudes = AreaEtudes
                .Where(ae => ae.Area?.Get() != null)
                .GroupBy(ae => ae.Area)
                .Select(g => g.First())
                .OrderBy(ae => ae.Area?.Get().name)
                .ToArray();

            AreaEtudes
                .ForEach(ae => ae.Name = ae.Area?.Get().name);

            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Find all areas linked to etudes starting from MainGame one, try to get it's
        /// root etude and append all this to AreaEtudes in case something is missing
        /// </summary>
        public void TryFixMissingEtudes()
        {
            // Just for cleanup
            UpdateNames();

            var areaRootEtude = AreaRootEtude?.Get();
            if (areaRootEtude == null)
            {
                return;
            }

            var etudeChildrenMap = EtudeHelper.CollectChildrenMap();
            var allAreas = EtudeHelper.CollectLinkedAreas(etudeChildrenMap, areaRootEtude);


            // Re-collect AreaEtudes adding new areas and trying to set corresponding etudes
            var newAreaEtudes = new List<AreaEtude>();
            foreach (var area in allAreas)
            {
                // New area detected
                var areaEtude = AreaEtudes.FirstOrDefault(ae => ae.Area?.Get() == area) ?? new AreaEtude
                {
                    Area = BlueprintAreaReference.CreateTyped<BlueprintAreaReference>(area)
                };

                if (areaEtude.Etude?.Get() == null)
                {
                    // Try to find root area etude
                    var rootEtudeCandidates = EtudeHelper.GetAreaRootCandidates(etudeChildrenMap, areaRootEtude, area);
                    if (rootEtudeCandidates.Count == 1)
                    {
                        // Single etude found - count as root area etude
                        var rootEtude = rootEtudeCandidates[0];
                        areaEtude.Etude = BlueprintEtudeReference.CreateTyped<BlueprintEtudeReference>(rootEtude);
                    }
                    else
                    {
                        Debug.Log($"Failed to get root area etude for {area.name}");
                        Debug.Log($"Candidates:\n{string.Join('\n', rootEtudeCandidates.Select(e => e.name))}");
                    }
                }
                newAreaEtudes.Add(areaEtude);
            }

            AreaEtudes = newAreaEtudes.ToArray();

            UpdateNames();
        }
    }
}
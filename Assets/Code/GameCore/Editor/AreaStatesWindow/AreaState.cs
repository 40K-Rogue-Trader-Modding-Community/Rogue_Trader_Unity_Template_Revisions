using System.Collections.Generic;
using System.Linq;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints.Area;
using UnityEditor;

#nullable enable

namespace Kingmaker.Editor.AreaStatesWindow
{
    public class AreaState
    {
        private const string BaseStateName = "BaseState";

        public BlueprintArea Area { get; }
        public BlueprintEtude? Etude { get; }

        public string Name => Etude?.name ?? BaseStateName;

        public string StateKey => $"{AreaStatesElement.BaseKey}.{Name}";

        public readonly List<SceneAsset> SceneAssets;

        public AreaState(BlueprintArea area, BlueprintEtude? etude = null)
        {
            Area = area;
            Etude = etude;
            SceneAssets = new List<SceneAsset>();
        }

        public List<SceneAsset> GetSceneAssets()
        {
            if (Etude == null)
            {
                GetAreaPartScenes(SceneAssets, Area);
            }
            else
            {
                SceneAssets.Clear();
                SceneAssets.AddRange(Etude.AddedAreaMechanics
                    .Where(am => am.Get().Area.Guid == Area.AssetGuid && am.Get().Scene != null)
                    .Select(am => am.Get().Scene.Asset));
            }
            return SceneAssets;
        }

        public static void GetAreaPartScenes(List<SceneAsset> sceneAssets, BlueprintAreaPart? areaPart)
        {
            sceneAssets.Clear();
            if (areaPart == null)
            {
                return;
            }

            if (areaPart.DynamicScene != null)
            {
                sceneAssets.Add(areaPart.DynamicScene.Asset);
            }

            if (areaPart.StaticScene != null)
            {
                sceneAssets.Add(areaPart.StaticScene.Asset);
            }

            if (areaPart.LightScene != null)
            {
                sceneAssets.Add(areaPart.LightScene.Asset);
            }
        }
    }
}
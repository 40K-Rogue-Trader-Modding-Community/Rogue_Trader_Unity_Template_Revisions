using System.Collections.Generic;
using System.Linq;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.UIElements;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Elements;
using Kingmaker.Utility.DotNetExtensions;
using Kingmaker.Utility.UnityExtensions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

#nullable enable

namespace Kingmaker.Editor.AreaStatesWindow
{
    /// <summary>
    /// This element collects and displays scene states for some area
    ///
    /// Expected to be used as a part of OwlcatInspectorRoot or in a separate
    /// window keeping the OwlcatInspectorRoot style
    /// </summary>
    public class AreaStatesElement : OwlcatInspectorStyle
    {
        public const string BaseKey = nameof(AreaStatesElement) + ".States";

        private readonly BlueprintArea _area;

        private readonly OwlcatInspectorFoldout _root;

        private readonly List<AreaState> _states = new(32);

        public AreaStatesElement(BlueprintArea area)
        {
            _area = area;
            _root = new OwlcatInspectorFoldout(BaseKey)
            {
                TitleLabel =
                {
                    text = "Scene States"
                },
            };
            Add(_root);
        }

        public void UpdateStates()
        {
            _root.ContentContainer.Clear();

            _states.Clear();
            _states.AddRange(new AreaStatesCollector(_area).Collect());

            int index = -1; // For zebra-style
            foreach (var state in _states)
            {
                index++;
                _root.ContentContainer.Add(new AreaStateFoldout(
                    state,
                    MakeSureBaseStateIsLoaded,
                    () => LoadState(state),
                    () => AppendState(state),
                    () => UnloadState(state),
                    BaseKey,
                    index));
            }

            var buttonsLayout = new OwlcatContentContainer
            {
                style = {flexDirection = FlexDirection.Row}
            };
            buttonsLayout.Add(new Button(() => NewStateForm.Present(_area.name, CreateNewState))
            {
                text = "New State",
                tooltip = "Create new state etude with a new scene",
            });
            buttonsLayout.Add(new Button(HierarchyWindowHelper.CollapseAllScenes)
            {
                text = "Collapse All Scenes",
                tooltip = "Collapse all scenes in Hierarchy",
            });
            _root.ContentContainer.Add(buttonsLayout);
        }

        public void LoadBaseState()
        {
            if (_states.Count > 0)
            {
                LoadState(_states[0]);
            }
        }

        private void CreateNewState((string stateName, string sceneName, string sceneTemplatePath) _)
        {
            // Let's find an etude that is root for the area
            var rootAreaEtude = AreaRootEtudes.GetInstance()?.GetAreaRootEtude(_area);
            if (rootAreaEtude == null)
            {
                ErrorPrompt($"Cannot get root area etude for {_area.name}");
                return;
            }

            // Add new state etude asset and append it to root area etude "StartWith"
            string? rootEtudeDir = BlueprintsDatabase.GetAssetPath(rootAreaEtude)[..^".jbp".Length];
            var stateEtude = BlueprintsDatabase.CreateAsset<BlueprintEtude>(rootEtudeDir, _.stateName);

            stateEtude.Parent = BlueprintEtudeReference.CreateTyped<BlueprintEtudeReference>(rootAreaEtude);
            rootAreaEtude.AppendStartWith(stateEtude);
            rootAreaEtude.SetDirty();

            // Create area mechanics
            AreaStateFoldout.CreateNewSceneForState(stateEtude, _area, (_.sceneName, _.sceneTemplatePath));
            EditorApplication.delayCall += UpdateStates;
        }

        private static List<SceneAsset> GetAreaPartScenes(BlueprintAreaPart areaPart)
        {
            var partScenes = new List<SceneAsset>();
            AreaState.GetAreaPartScenes(partScenes, areaPart);
            return partScenes;
        }

        private static void ErrorPrompt(string? message)
        {
            if (message != null)
            {
                EditorUtility.DisplayDialog("Error", message, "Ok");
            }
        }

        private void LoadState(AreaState state)
        {
            MakeSureBaseStateIsLoaded();

            _states
                .Where(s => s.Etude != null && s.Etude.AssetGuid != state.Etude?.AssetGuid)
                .ForEach(UnloadState);

            AppendState(state);
        }

        private static void AppendState(AreaState state)
        {
            var loadedScenePaths = new List<string>(SceneManager.sceneCount);
            for (int sceneIdx = 0; sceneIdx < SceneManager.sceneCount; sceneIdx++)
            {
                var scene = SceneManager.GetSceneAt(sceneIdx);
                if (scene.isLoaded)
                {
                    loadedScenePaths.Add(scene.path);
                }
            }

            // Load all scenes from the state if they are not loaded already
            var scenePaths = GetStateScenePaths(state);
            scenePaths
                .Except(loadedScenePaths)
                .ForEach(sp => EditorSceneManager.OpenScene(sp, OpenSceneMode.Additive));
        }

        private static IEnumerable<string> GetStateScenePaths(AreaState state)
        {
            bool isBaseState = state.Etude == null;

            var sceneAssets = new List<SceneAsset?>();
            sceneAssets.AddRange(state.SceneAssets);

            if (isBaseState && state.Area.Parts.Any())
            {
                var areaParts = state.Area.Parts
                    .Where(p => p != null);
                foreach (var areaPart in areaParts)
                {
                    var areaPartScenes = GetAreaPartScenes(areaPart);
                    sceneAssets.AddRange(areaPartScenes);
                }
            }
            return sceneAssets
                .Where(sa => sa != null)
                .Select(AssetDatabase.GetAssetPath);
        }

        private void MakeSureBaseStateIsLoaded()
        {
            var baseState = _states.FirstOrDefault(state => state.Etude == null);
            if (baseState == null)
            {
                // There is no base state
                return;
            }
            AppendState(baseState);
            RemoveAlienScenes();
        }

        /// <summary>
        /// Removes all scenes that are not from any state
        /// </summary>
        private void RemoveAlienScenes()
        {
            // Collect scene paths from all states
            var scenePaths = new HashSet<string>();
            foreach (var state in _states)
            {
                scenePaths.AddRange(GetStateScenePaths(state));
            }

            var scenesToRemove = new List<Scene>(SceneManager.sceneCount);
            for (int sceneIdx = 0; sceneIdx < SceneManager.sceneCount; sceneIdx++)
            {
                var scene = SceneManager.GetSceneAt(sceneIdx);
                if (!scenePaths.Contains(scene.path))
                {
                    scenesToRemove.Add(scene);
                }
            }
            scenesToRemove.ForEach(s => EditorSceneManager.CloseScene(s, true));
        }

        private static void UnloadState(AreaState state)
        {
            foreach (var sceneAsset in state.SceneAssets)
            {
                string path = AssetDatabase.GetAssetPath(sceneAsset);
                var scene = SceneManager.GetSceneByPath(path);
                if (scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, false);
                }
            }
        }
    }
}
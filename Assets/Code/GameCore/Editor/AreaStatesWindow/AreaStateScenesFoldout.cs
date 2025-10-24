using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Elements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

#nullable enable

namespace Kingmaker.Editor.AreaStatesWindow
{
    public class AreaStateScenesFoldout : VisualElement
    {
        private readonly AreaState _state;
        private readonly SerializedProperty? _scenesSourceProp;
        private readonly Action<(string sceneName, string sceneTemplatePath)>? _createNewSceneForState;
        private readonly Action? _addExistingSceneToState;

        private readonly OwlcatInspectorFoldout _foldout;

        public AreaStateScenesFoldout(
            AreaState state,
            SerializedProperty? scenesSourceProp,
            Action<(string sceneName, string sceneTemplatePath)> createNewSceneForState,
            Action addExistingSceneToState)
        {
            _state = state;
            _createNewSceneForState = createNewSceneForState;
            _addExistingSceneToState = addExistingSceneToState;

            _foldout = new OwlcatInspectorFoldout($"{_state.StateKey}.Scenes")
            {
                TitleLabel = {text = "Scenes"},
                style = {paddingBottom = 10}
            };

            void Update(SerializedProperty? _) => UpdateSceneAssets();
            Update(scenesSourceProp);
            if (scenesSourceProp != null)
            {
                _foldout.TrackPropertyValue(scenesSourceProp, Update);
            }
            Add(_foldout);
        }

        public AreaStateScenesFoldout(AreaState state, BlueprintAreaPart areaPart)
        {
            _state = state;
            _foldout = new OwlcatInspectorFoldout($"{_state.StateKey}.{areaPart.name}.Scenes")
            {
                TitleLabel = {text = "Part Scenes"},
                style = {paddingBottom = 10}
            };
            var sceneAssets = new List<SceneAsset>();
            AreaState.GetAreaPartScenes(sceneAssets, areaPart);
            AddSceneAssets(sceneAssets);
            Add(_foldout);
        }

        private void AddSceneAssets(List<SceneAsset> sceneAssets)
        {
            _foldout.ContentContainer.Clear();
            foreach (var sceneAsset in sceneAssets)
            {
                if (sceneAsset == null)
                {
                    continue;
                }

                var sceneLayout = new OwlcatContentContainer
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                    }
                };

                var text = new TextField
                {
                    value = sceneAsset.name,
                    style = {flexGrow = 1},
                };
                text.SetEnabled(false);
                sceneLayout.Add(text);

                sceneLayout.Add(new OwlcatSmallButton(() => MakeSceneActive(sceneAsset))
                {
                    text = "A",
                    tooltip = "Make scene active in Hierarchy",
                });
                sceneLayout.Add(new OwlcatSmallButton(() => EditorGUIUtility.PingObject(sceneAsset))
                {
                    text = "P",
                    tooltip = "Ping scene asset in Project",
                });
                if (_state.Etude != null)
                {
                    sceneLayout.Add(new OwlcatSmallButton(() => RemoveSceneAssetFromEtude(sceneAsset))
                    {
                        text = "X",
                        tooltip = "Remove scene from the state",
                        style =
                        {
                            backgroundColor = new Color(0.94f, 0.5f, 0.5f),
                            color = Color.black,
                        }
                    });
                }

                _foldout.ContentContainer.Add(sceneLayout);
            }
        }

        private void UpdateSceneAssets()
        {
            var sceneAssets = _state.GetSceneAssets();
            AddSceneAssets(sceneAssets);

            if (_state.Etude != null && _createNewSceneForState != null && _addExistingSceneToState != null)
            {
                var buttonsLayout = new OwlcatContentContainer
                {
                    style = {flexDirection = FlexDirection.Row}
                };
                buttonsLayout.Add(new Button(() => NewSceneNameForm.Present(
                    _state.Area.name,
                    _state.Etude?.name,
                    _createNewSceneForState))
                {
                    text = "New Scene",
                    tooltip = "Create new scene and add it to the state.",
                });
                buttonsLayout.Add(new Button(_addExistingSceneToState)
                {
                    text = "Add Scene",
                    tooltip = "Add existing scene to the state.",
                });
                _foldout.ContentContainer.Add(buttonsLayout);
            }
        }

        private static void MakeSceneActive(SceneAsset sceneAsset)
        {
            string path = AssetDatabase.GetAssetPath(sceneAsset);
            var scene = SceneManager.GetSceneByPath(path);
            if (scene.isLoaded)
            {
                SceneManager.SetActiveScene(scene);
                return;
            }
            EditorUtility.DisplayDialog("Error", "Scene is not loaded!", "Ok");
        }

        private void RemoveSceneAssetFromEtude(SceneAsset sceneAsset)
        {
            if (_state.Etude == null)
            {
                return;
            }
            Undo.RecordObject(BlueprintEditorWrapper.Wrap(_state.Etude), _state.Etude.AssetGuid);

            // Re-create etude's AddedAreaMechanics skipping one with sceneAsset
            string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
            _state.Etude.ReplaceAddedAreaMechanics(_state.Etude.AddedAreaMechanics
                .Where(addedMechanicsRef => addedMechanicsRef.Get().Scene.ScenePath != scenePath)
                .ToList());
            _state.Etude.SetDirty();
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Editor;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.UIElements;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Elements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

#nullable enable

namespace Kingmaker.Editor.AreaStatesWindow
{
    public class AreaStateFoldout : VisualElement
    {
        private readonly OwlcatInspectorFoldout _foldout;
        private readonly AreaState _state;
        private readonly Action _loadStateFromScratch;
        private readonly Action _loadState;
        private readonly Action _appendState;
        private readonly Action _unloadState;

        private const string EtudeCommentPropertyPath =
            nameof(BlueprintEditorWrapper.Blueprint) + "." + nameof(BlueprintScriptableObject.Comment);

        private const string EtudeAddedAreaMechanicsPropertyPath =
            nameof(BlueprintEditorWrapper.Blueprint) + "." + BlueprintEtude.NameOfAddedAreaMechanics;

        public AreaStateFoldout(
            AreaState state,
            Action loadStateFromScratch,
            Action loadState,
            Action appendState,
            Action unloadState,
            string baseKey,
            int stateIndex)
        {
            _state = state;
            _loadStateFromScratch = loadStateFromScratch;
            _loadState = loadState;
            _appendState = appendState;
            _unloadState = unloadState;

            _foldout = new OwlcatInspectorFoldout($"{baseKey}.{_state.Name}")
            {
                TitleLabel = {text = state.Name},
                style = { backgroundColor = UIElementsResources.GetZebra(stateIndex)}
            };

            if (state.Etude == null)
            {
                AddBaseStateLayout();
            }
            else
            {
                AddEtudeStateLayout();
            }
            Add(_foldout);
        }

        private void AddBaseStateLayout()
        {
            _foldout.ContentContainer.Add(new AreaStateScenesFoldout(
                _state,
                null,
                CreateNewSceneForState,
                AddExistingSceneToState));

            var parts = _state.Area.Parts
                .Where(p => p != null)
                .ToArray();
            if (parts.Any())
            {
                string partsKey = $"{_state.StateKey}.Parts";
                _foldout.ContentContainer.Add(CreatePartsLayout(partsKey, parts));
            }
            var loadButtonsLayout = new OwlcatContentContainer
            {
                style = {flexDirection = FlexDirection.Row}
            };
            loadButtonsLayout.Add(new Button(_loadStateFromScratch)
            {
                text = "Load State From Scratch"
            });
            _foldout.ContentContainer.Add(loadButtonsLayout);
        }

        private VisualElement CreatePartsLayout(string partsKey, BlueprintAreaPartReference[] parts)
        {
            var foldout = new OwlcatInspectorFoldout(partsKey)
            {
                TitleLabel = {text = "Area Parts"},
            };

            int index = -1; // For zebra-style
            foreach (var areaPartReference in parts)
            {
                index++;
                var areaPart = areaPartReference.Get();
                var partRoot = new OwlcatInspectorFoldout($"{partsKey}.{areaPart.name}")
                {
                    TitleLabel = {text = areaPart.name},
                    style =
                    {
                        backgroundColor = UIElementsResources.GetZebra(index),
                    }
                };

                partRoot.ContentContainer.Add(CreateBlueprintButton("Area Part", "Ping area part blueprint", areaPart));
                partRoot.ContentContainer.Add(new AreaStateScenesFoldout(_state, areaPart));

                foldout.ContentContainer.Add(partRoot);
            }

            return foldout;
        }

        private static VisualElement CreateBlueprintButton(string label, string tooltip, SimpleBlueprint etude)
        {
            var etudeLayout = new OwlcatPropertyLayout(OwlcatPropertyLayout.Layout.Horizontal, false)
            {
                TitleLabel = {text = label},
            };

            var etudeButton = new Button
            {
                text = etude.name,
                style = {flexGrow = 1},
                tooltip = tooltip,
            };
            etudeButton.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.ctrlKey)
                {
                    Selection.activeObject = BlueprintEditorWrapper.Wrap(etude);
                }
                else
                {
                    BlueprintProjectView.Ping(etude);
                }
            });
            etudeLayout.ContentContainer.Add(etudeButton);
            return etudeLayout;
        }

        private void AddEtudeStateLayout()
        {
            if (_state.Etude == null)
            {
                return;
            }

            _foldout.ContentContainer.Add(CreateBlueprintButton("Etude", "Ping etude blueprint", _state.Etude));

            var etudeWrapper = BlueprintEditorWrapper.Wrap(_state.Etude);
            var etudeSo = new SerializedObject(etudeWrapper);
            _foldout.Bind(etudeSo);

            // Add comment property of the etude
            var commentElement = new OwlcatTextField
            {
                multiline = true,
                tooltip = "Comment from state etude.",
                style = {minHeight = 20},
                bindingPath = EtudeCommentPropertyPath
            };
            _foldout.ContentContainer.Add(commentElement);

            var areasProp = etudeSo.FindProperty(EtudeAddedAreaMechanicsPropertyPath);
            _foldout.ContentContainer.Add(new AreaStateScenesFoldout(
                _state,
                areasProp,
                CreateNewSceneForState,
                AddExistingSceneToState));

            var buttonsLayout = new OwlcatContentContainer
            {
                style = {flexDirection = FlexDirection.Row}
            };
            buttonsLayout.Add(new Button(_loadState)
            {
                text = "Load",
                tooltip = "Load this state and unload all other states.",
            });
            buttonsLayout.Add(new Button(_appendState)
            {
                text = "Append",
                tooltip = "Append this state in addition to other loaded states.",
            });
            buttonsLayout.Add(new Button(_unloadState)
            {
                text = "Unload",
                tooltip = "Unload this state.",
            });

            _foldout.ContentContainer.Add(buttonsLayout);
        }

        private void AddExistingSceneToState()
        {
            string? scenesRootDir = GetScenesRootDir(_state.Area, out string? error)?.Replace("\\", "/");
            if (scenesRootDir == null)
            {
                ErrorPrompt(error);
                return;
            }

            string scenePath = EditorUtility.OpenFilePanel("Select scene", scenesRootDir, "unity")
                .Replace(Application.dataPath, "Assets");
            if (string.IsNullOrEmpty(scenePath))
            {
                return;
            }

            if (SceneAlreadyExistInState(_state.Etude, scenePath))
            {
                return;
            }

            AddExistingSceneToStateImpl(_state.Etude, _state.Area, scenePath);
        }

        private static string? GetScenesRootDir(BlueprintArea area, out string? error)
        {
            error = null;

            // Get the folder base state scenes are placed in
            var sceneAssets = new List<SceneAsset>();
            AreaState.GetAreaPartScenes(sceneAssets, area);
            if (!sceneAssets.Any())
            {
                ErrorPrompt("No area scenes are defined for the base state!");
                return null;
            }

            string? scenesDir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(sceneAssets.First()));
            if (scenesDir == null)
            {
                ErrorPrompt("Cannot get scenes folder");
                return null;
            }

            return scenesDir;
        }

        private void CreateNewSceneForState((string? sceneName, string? sceneTemplatePath) _)
        {
            CreateNewSceneForState(_state.Etude, _state.Area, _);
        }

        public static void CreateNewSceneForState(BlueprintEtude? etude, BlueprintArea area, (string? sceneName, string? sceneTemplatePath) _)
        {
            if (_.sceneName == null)
            {
                ErrorPrompt("Scene name not defined");
                return;
            }

            if (_.sceneTemplatePath == null)
            {
                ErrorPrompt("Scene template not defined");
                return;
            }

            string? scenesRootDir = GetScenesRootDir(area, out string? error)?.Replace("\\", "/");
            if (scenesRootDir == null)
            {
                ErrorPrompt(error);
                return;
            }

            if (!File.Exists(_.sceneTemplatePath))
            {
                ErrorPrompt($"Cannot find scene template at {_.sceneTemplatePath}");
                return;
            }

            string newScenePath = $"{scenesRootDir}/{_.sceneName}";
            if (File.Exists(newScenePath))
            {
                if (SceneAlreadyExistInState(etude, newScenePath))
                {
                    return;
                }

                if (!EditorUtility.DisplayDialog(
                        "Warning",
                        $"Scene already exists:\n{newScenePath}\n\nOverwrite?",
                        "Ok", "Cancel"))
                {
                    return;
                }
            }

            File.Copy(_.sceneTemplatePath, newScenePath, true);
            AssetDatabase.Refresh();

            AddExistingSceneToStateImpl(etude, area, newScenePath);
        }

        private static bool SceneAlreadyExistInState(BlueprintEtude? etude, string scenePath)
        {
            if (etude == null)
            {
                return false;
            }

            var existingScenePaths = etude.AddedAreaMechanics
                .Select(am => am.Get().Scene.ScenePath);
            if (existingScenePaths.Contains(scenePath))
            {
                ErrorPrompt($"Scene\n{scenePath}\nAlready present in the state");
                return true;
            }
            return false;
        }

        private static void AddExistingSceneToStateImpl(BlueprintEtude? etude, BlueprintArea area, string scenePath)
        {
            if (etude == null)
            {
                return;
            }

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset == null)
            {
                ErrorPrompt($"Cannot load scene asset from:\n{scenePath}");
                return;
            }

            // Create new AreaMechanics in area's Addon folder
            string? areaDir = Path.GetDirectoryName(BlueprintsDatabase.GetAssetPath(area));
            if (areaDir == null)
            {
                ErrorPrompt("Cannot get area folder");
                return;
            }

            string addonsPath = @$"{areaDir}\Addons";
            var mechanics = BlueprintsDatabase.CreateAsset<BlueprintAreaMechanics>(addonsPath, sceneAsset.name);
            mechanics.Area = BlueprintAreaReference.CreateTyped<BlueprintAreaReference>(area);
            mechanics.Scene = new SceneReference(sceneAsset);
            mechanics.SetDirty();

            // Add new mechanics to etude
            Undo.RecordObject(BlueprintEditorWrapper.Wrap(etude), etude.AssetGuid);
            etude.ReplaceAddedAreaMechanics(etude.AddedAreaMechanics
                .Append(BlueprintAreaMechanicsReference.CreateTyped<BlueprintAreaMechanicsReference>(mechanics))
                .ToList());
            etude.SetDirty();
        }

        private static void ErrorPrompt(string? message)
        {
            if (message != null)
            {
                EditorUtility.DisplayDialog("Error", message, "Ok");
            }
        }
    }
}
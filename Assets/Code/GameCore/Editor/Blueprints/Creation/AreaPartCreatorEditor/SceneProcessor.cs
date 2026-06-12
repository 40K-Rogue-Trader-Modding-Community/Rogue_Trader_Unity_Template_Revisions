﻿using System;
using System.Collections.Generic;
using System.Linq;
 using Kingmaker.Pathfinding;
 using Kingmaker.Utility.DotNetExtensions;
using Owlcat.Runtime.Core.Utility;
using Pathfinding;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

#nullable enable

namespace Kingmaker.Editor.Blueprints.Creation
{
    /// <summary>
    /// This is a helper to process some scenes in editor keeping already open scenes intact
    /// </summary>
    public class SceneProcessor : IDisposable
    {
        private const string LayoutName = "LAYOUT";
        private const string GroundName = "Ground";
        private const string NavMeshPrefabPath = "Assets/Mechanics/Prefabs/NavMesh.prefab";
        private const string GroundMaterialPath = "Assets/Art/Techart/Prototyping/PrototypeGrid.mat";

        private enum SceneFinishMode
        {
            Keep,
            Unload,
            Remove,
        }

        private readonly SceneFinishMode _mode;

        private readonly Scene Scene;

        private SceneProcessor(string scenePath)
        {
            Scene = SceneManager.GetSceneByPath(scenePath);
            if (!Scene.IsValid())
            {
                // Remove the scene, if it was not opened  before processing
                _mode = SceneFinishMode.Remove;
                Scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }
            else if (!Scene.isLoaded)
            {
                // Unload the scene, if it was unloaded before processing
                _mode = SceneFinishMode.Unload;
                Scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }
            else
            {
                // Keep the scene open, if it was opened  before processing
                _mode = SceneFinishMode.Keep;
            }
        }

        public void Dispose()
        {
            switch(_mode)
            {
                case SceneFinishMode.Keep:
                    break;

                case SceneFinishMode.Unload:
                    EditorSceneManager.CloseScene(Scene, false);
                    break;

                case SceneFinishMode.Remove:
                    EditorSceneManager.CloseScene(Scene, true);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            EditorSceneManager.SaveScene(Scene);
        }

        public static void CreateGroundForScene(Scene scene, Vector3 size)
        {
            var layout = scene.GetRootGameObjects().FirstOrDefault(go => go.name == LayoutName);
            if (layout == null)
            {
                layout = new GameObject(LayoutName);
            }

            var groundTransform = layout.transform.Find(GroundName);
            if (groundTransform == null)
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.name = "Ground";
                ground.layer = (int) Layers.Ground;

                groundTransform = ground.transform;
                groundTransform.parent = layout.transform;

                var mesh = ground.GetComponent<MeshRenderer>();
                if (mesh == null)
                {
                    return;
                }

                var material = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
                if (material != null)
                {
                    mesh.sharedMaterial = material;
                }
            }

            float centerY = -size.y / 2;
            groundTransform.position = new Vector3(0,centerY, 0);
            groundTransform.localScale = size;

            Selection.activeObject = groundTransform.gameObject;
        }

        public static CustomGridGraph? CreateNavMeshForScene(Scene scene, Vector2Int size)
        {
            // Try to get existing navmesh
            var astarPath = scene.GetRootGameObjects()
                .Select(go => go.GetComponent<AstarPath>())
                .FirstOrDefault(astar => astar != null);

            if (astarPath == null)
            {
                // Try to import
                var navMeshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NavMeshPrefabPath);
                if (navMeshPrefab == null)
                {
                    return null;
                }

                var navMesh = PrefabUtility.InstantiatePrefab(navMeshPrefab) as GameObject;
                if (navMesh == null)
                {
                    return null;
                }

                astarPath = navMesh.GetComponent<AstarPath>();
                if (astarPath == null)
                {
                    return null;
                }
            }
            Selection.activeObject = astarPath.gameObject;

            return AreaPartCreatorEditor.SetupAstarPath(astarPath, size);
        }

        public static void Process(string scenePath, IEnumerable<Action<Scene>> actions)
        {
            using var processor = new SceneProcessor(scenePath);
            var scene = processor.Scene;
            actions.ForEach(action => action.Invoke(scene));
        }

        public static void Process(string scenePath, Action<Scene> process)
        {
            using var processor = new SceneProcessor(scenePath);
            var scene = processor.Scene;
            process(scene);
        }
    }
}
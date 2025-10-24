﻿using System;
using System.IO;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
 using Kingmaker.Pathfinding;
 using Kingmaker.Utility.DotNetExtensions;
using Kingmaker.Utility.EditorPreferences;
using Kingmaker.View;
using Owlcat.Runtime.Core.Utility;
using Pathfinding;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Path = System.IO.Path;

#nullable enable

namespace Kingmaker.Editor.Blueprints.Creation
{
    public class AreaPartCreatorEditor
    {
	    private const float BoundsCenterOffset = -2;
	    private const float BoundsFowFactor = 1;
	    private const float BoundsCameraExpand = 10;

	    private const string LayoutName = "LAYOUT";
	    private const string GroundMaterialPath = "Assets/Plugins/External/RootMotion/Shared Demo Assets/Prototyping/Materials/Grid/Prototype_Grid_Floor.mat";

	    private bool HasLightScene { get; set; }
	    private Vector2Int AreaSize { get; set; }

        public AreaPartCreatorEditor()
        {
            HasLightScene = true;
            AreaSize = new Vector2Int(50, 50);
        }

        public void OnGui()
        {
            EditorGUIUtility.wideMode = true;
            EditorGUILayout.Space(10);
            HasLightScene = EditorGUILayout.Toggle("Create light scene", HasLightScene);
            AreaSize = EditorGUILayout.Vector2IntField("Area Size", AreaSize);
            EditorGUILayout.Space(10);
        }

        private static int GetClosestLessEvenNodeCount(int size, float nodeSize)
        {
	        int count = (int) Math.Floor(size / nodeSize);
	        return count % 2 == 0 ? count : --count;
        }

        public static BlueprintAreaEnterPoint? CreateEnterPoint(BlueprintArea? area, BlueprintAreaPart? areaPart, string entranceSuffix)
        {
	        BlueprintAreaEnterPoint? enterPoint = null;
	        var enterPointCreator = ScriptableObject.CreateInstance<BlueprintEnterPointCreator>();
	        if (enterPointCreator != null)
	        {
		        enterPointCreator.Area = area.ToReference<BlueprintAreaReference>();
		        enterPointCreator.AreaPart = areaPart.ToReference<BlueprintAreaPartReference>();
		        enterPoint = NewAssetWindow.CreateWithCreator(
			        enterPointCreator,
			        entranceSuffix) as BlueprintAreaEnterPoint;
	        }

	        enterPoint?.SetDirty();
	        return enterPoint;
        }

        public static BlueprintAreaEnterPoint? CreateEnterPoint(BlueprintArea? area, string entranceSuffix)
        {
	        return CreateEnterPoint(area, area, entranceSuffix);
        }

        public void CreateAssets(BlueprintAreaPart area, BlueprintAreaEnterPoint? enterPoint,
	        string mechanicsPath, string staticPath, string lightPath,
	        string MechanicsTemplateScenePath, string StaticTemplateScenePath, string LightTemplateScenePath)
        {
	        if (area == null)
	        {
		        throw new Exception("Failed to create area assets as given area is undefined.");
	        }

			Directory.CreateDirectory(Path.GetDirectoryName(mechanicsPath) ?? string.Empty);
			AssetDatabase.CopyAsset(MechanicsTemplateScenePath, mechanicsPath);
			var mechanicsScene = EditorSceneManager.OpenScene(mechanicsPath, OpenSceneMode.Single);
			area.DynamicScene = new SceneReference(AssetDatabase.LoadAssetAtPath<SceneAsset>(mechanicsPath));

			Directory.CreateDirectory(Path.GetDirectoryName(staticPath) ?? string.Empty);
			AssetDatabase.CopyAsset(StaticTemplateScenePath, staticPath);
			var staticScene = EditorSceneManager.OpenScene(staticPath, OpenSceneMode.Additive);
			area.StaticScene = new SceneReference(AssetDatabase.LoadAssetAtPath<SceneAsset>(staticPath));

			if (HasLightScene)
			{
				Directory.CreateDirectory(Path.GetDirectoryName(lightPath) ?? string.Empty);
				AssetDatabase.CopyAsset(LightTemplateScenePath, lightPath);
				EditorSceneManager.OpenScene(lightPath, OpenSceneMode.Additive);
				area.LightScene = new SceneReference(AssetDatabase.LoadAssetAtPath<SceneAsset>(lightPath));
			}

			if (EditorPreferences.Instance.LdDesigner)
			{
				SceneManager.SetActiveScene(mechanicsScene);
			}

			AstarPath? astarPath = null;
			foreach (var go in mechanicsScene.GetRootGameObjects())
			{
				// Fix copied unique ids
				go.GetComponentsInChildren<EntityViewBase>()
					.ForEach(e => e.UniqueId = Guid.NewGuid().ToString());

				go.GetComponentsInChildren<AreaEnterPoint>()
					.ForEach(p => p.Blueprint = enterPoint);

				if (astarPath == null)
				{
					astarPath = go.GetComponent<AstarPath>();
				}
			}

			CustomGridGraph? gridGraph = null;
			if (astarPath != null && astarPath.graphs.FirstItem() is CustomGridGraph gg)
			{
				gridGraph = gg;

				// Fit grid node counts into area size that is in meters
				int width = GetClosestLessEvenNodeCount(AreaSize.x, gridGraph.nodeSize);
				int depth = GetClosestLessEvenNodeCount(AreaSize.y, gridGraph.nodeSize);
				gridGraph.SetDimensions(width, depth, gridGraph.nodeSize);
				gridGraph.showNodeConnections = false;
				astarPath.logPathResults = PathLog.OnlyErrors;
				astarPath.debugMode = GraphDebugMode.SolidColor;
			}

			AreaPartBounds? bounds = null;
			var boundsCreator = ScriptableObject.CreateInstance<AreaPartBoundsCreator>();
			if (boundsCreator != null)
			{
				boundsCreator.Area = area.ToReference<BlueprintAreaPartReference>();
				bounds = NewAssetWindow.CreateWithCreator(
					boundsCreator,
					area.name + "_Bounds") as AreaPartBounds;

				if (bounds != null)
				{
					float maxSize = Math.Min(AreaSize.x, AreaSize.y);
					var size = new Vector3(AreaSize.x, maxSize, AreaSize.y);
					var center = new Vector3(0, maxSize / 2 + BoundsCenterOffset, 0);
					var defaultBounds = new Bounds(center, size);

					bounds.DefaultBounds = defaultBounds;
					bounds.MechanicBounds = defaultBounds;
					bounds.LocalMapBounds = defaultBounds;
					bounds.BakedGroundBounds = defaultBounds;

					bounds.OverrideFogOfWarBounds = true;
					var fowBounds = defaultBounds;
					fowBounds.Expand(maxSize * BoundsFowFactor);
					bounds.FogOfWarBounds = fowBounds;

					bounds.OverrideCameraBounds = true;
					var cameraBounds = defaultBounds;
					cameraBounds.Expand(BoundsCameraExpand);
					bounds.CameraBounds = cameraBounds;

					area.Bounds = bounds;
				}
			}

			var layout = staticScene.GetRootGameObjects().FirstOrDefault(go => go.name == LayoutName);
			if (layout != null)
			{
				CreateGround(layout, gridGraph);
			}

			var pathfinder = AstarPath.active;
			if (pathfinder != null)
			{
				pathfinder.data.file_cachedStartup = CreateEmptyNavmeshCacheFile();
				EditorUtility.SetDirty(pathfinder);
			}
			EditorSceneManager.SaveScene(mechanicsScene);


	        #if !OWLCAT_MODS
			area.FixLightScenes();
			#endif

			EditorUtility.SetDirty(bounds);
			area.SetDirty();
		}

        private static void CreateGround(GameObject layout, CustomGridGraph? gridGraph)
        {
	        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
	        ground.name = "Ground";
	        ground.layer = (int) Layers.Ground;

	        var t = ground.transform;
	        t.parent = layout.transform;

	        float centerY = gridGraph == null ? -0.675f : -gridGraph.nodeSize / 2;
	        t.position = new Vector3(0,centerY, 0);

	        float sizeX = gridGraph == null ? 50 : gridGraph.width * gridGraph.nodeSize;
	        float sizeY = gridGraph?.nodeSize ?? 1.35f;
	        float sizeZ = gridGraph == null ? 50 : gridGraph.depth * gridGraph.nodeSize;
	        t.localScale = new Vector3(sizeX, sizeY, sizeZ);

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

        private static TextAsset? CreateEmptyNavmeshCacheFile()
		{
			string scenePath = SceneManager.GetActiveScene().path;

			string sceneName = SceneManager.GetActiveScene().name;
			int underTypeIndex = sceneName.LastIndexOf("_", StringComparison.Ordinal);
			if (underTypeIndex > 0)
				sceneName = sceneName[..underTypeIndex];

			int underscoreIndex = scenePath.LastIndexOf("/", StringComparison.Ordinal);
			if (underscoreIndex > 0)
				scenePath = scenePath[..underscoreIndex];

			scenePath += "/Navmesh/";
			Directory.CreateDirectory(Path.GetDirectoryName(scenePath) ?? string.Empty);

			scenePath += sceneName + ".bytes";
			string path = AssetDatabase.GenerateUniqueAssetPath(scenePath);

			var fileInfo = new FileInfo(path);
			if (fileInfo is {Exists: true, IsReadOnly: true})
				fileInfo.IsReadOnly = false;

			File.WriteAllBytes(path,Array.Empty<byte>());

			AssetDatabase.Refresh();
			return AssetDatabase.LoadAssetAtPath(path, typeof(TextAsset)) as TextAsset;
		}
    }
}
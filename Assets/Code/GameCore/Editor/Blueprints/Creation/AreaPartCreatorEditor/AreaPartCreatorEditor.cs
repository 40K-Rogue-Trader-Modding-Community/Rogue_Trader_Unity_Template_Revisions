using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Pathfinding;
using Kingmaker.Utility.DotNetExtensions;
using Kingmaker.Utility.EditorPreferences;
using Kingmaker.View;
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

	    private bool HasLightScene { get; set; }
	    private bool HasAudioScene { get; set; }
	    private Vector2Int AreaSize { get; set; }

        public AreaPartCreatorEditor()
        {
	        HasLightScene = true;
	        HasAudioScene = true;
            AreaSize = new Vector2Int(50, 50);
        }

        public void OnGui()
        {
            EditorGUIUtility.wideMode = true;
            EditorGUILayout.Space(10);
            HasLightScene = EditorGUILayout.Toggle("Create light scene", HasLightScene);
            HasAudioScene = EditorGUILayout.Toggle("Create audio scene", HasAudioScene);
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

        public static bool AssetsExist(IEnumerable<string> assetPaths)
        {
	        string[] existingPaths = assetPaths
		        .Where(File.Exists)
		        .ToArray();
	        if (!existingPaths.Any())
	        {
		        return false;
	        }

	        string pathsList = string.Join("\n", existingPaths);
	        EditorUtility.DisplayDialog("Error", "Some assets already exist:\n" + pathsList, "Ok");
	        return true;
        }

        public void CreateAssets(BlueprintAreaPart area, BlueprintAreaEnterPoint? enterPoint,
	        string mechanicsPath, string staticPath, string lightPath, string audioPath,
	        string MechanicsTemplateScenePath, string StaticTemplateScenePath, string LightTemplateScenePath, string AudioTemplateScenePath)
        {
	        if (area == null)
	        {
		        throw new Exception("Failed to create area assets as given area is undefined.");
	        }

	        if (AssetsExist(new[] {mechanicsPath, staticPath, lightPath, audioPath}))
	        {
				return;
	        }

			Directory.CreateDirectory(Path.GetDirectoryName(mechanicsPath) ?? string.Empty);
			AssetDatabase.CopyAsset(MechanicsTemplateScenePath, mechanicsPath);
			var mechanicsScene = EditorSceneManager.OpenScene(mechanicsPath, OpenSceneMode.Single);
			area.DynamicScene = new SceneReference(AssetDatabase.LoadAssetAtPath<SceneAsset>(mechanicsPath));

			Directory.CreateDirectory(Path.GetDirectoryName(staticPath) ?? string.Empty);
			AssetDatabase.CopyAsset(StaticTemplateScenePath, staticPath);
			EditorSceneManager.OpenScene(staticPath, OpenSceneMode.Additive);
			area.StaticScene = new SceneReference(AssetDatabase.LoadAssetAtPath<SceneAsset>(staticPath));

			if (HasLightScene)
			{
				Directory.CreateDirectory(Path.GetDirectoryName(lightPath) ?? string.Empty);
				AssetDatabase.CopyAsset(LightTemplateScenePath, lightPath);
				EditorSceneManager.OpenScene(lightPath, OpenSceneMode.Additive);
				area.LightScene = new SceneReference(AssetDatabase.LoadAssetAtPath<SceneAsset>(lightPath));
			}

			if (HasAudioScene)
			{
				Directory.CreateDirectory(Path.GetDirectoryName(audioPath) ?? string.Empty);
				AssetDatabase.CopyAsset(AudioTemplateScenePath, audioPath);
				EditorSceneManager.OpenScene(audioPath, OpenSceneMode.Additive);
				area.SetAudioScenes(new []{new SceneReference(AssetDatabase.LoadAssetAtPath<SceneAsset>(audioPath))});
			}

			if (EditorPreferences.Instance.LdDesigner)
			{
				SceneManager.SetActiveScene(mechanicsScene);
			}

			// Setup enter points
			foreach (var go in mechanicsScene.GetRootGameObjects())
			{
				// Fix copied unique ids
				go.GetComponentsInChildren<EntityViewBase>()
					.ForEach(e => e.UniqueId = Guid.NewGuid().ToString());

				go.GetComponentsInChildren<AreaEnterPoint>()
					.ForEach(p => p.Blueprint = enterPoint);
			}

			var astarPath = GetAstarPath(mechanicsScene);

			CustomGridGraph? gridGraph = null;
			if (astarPath != null)
			{
				gridGraph = SetupAstarPath(astarPath, AreaSize);
			}

			CreateAreaBounds(area, AreaSize);

			SceneProcessor.Process(staticPath,
				scene => SceneProcessor.CreateGroundForScene(scene, GetGroundSizeFromGridGraph(gridGraph)));

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

			area.SetDirty();
		}

        private static Vector3 GetGroundSizeFromGridGraph(CustomGridGraph? gridGraph)
        {
	        float sizeX = gridGraph == null ? 50 : gridGraph.width * gridGraph.nodeSize;
	        float sizeY = gridGraph?.nodeSize ?? 1.35f;
	        float sizeZ = gridGraph == null ? 50 : gridGraph.depth * gridGraph.nodeSize;
	        return new Vector3(sizeX, sizeY, sizeZ);
        }

        /// <summary>
        /// Tries to find AstarPath component in root game objects of given scene
        /// </summary>
        private static AstarPath? GetAstarPath(Scene scene)
        {
	        return scene.GetRootGameObjects()
		        .Select(go => go.GetComponent<AstarPath>())
		        .FirstOrDefault(astar => astar != null);
        }

        /// <summary>
        /// Sets AstarPath GridGraph to fit given area size and sets proper debug options
        /// </summary>
        /// <returns>Properly set GridGraph</returns>
        public static CustomGridGraph? SetupAstarPath(AstarPath astarPath, Vector2Int AreaSize)
        {
	        if (astarPath == null || astarPath.graphs.FirstItem() is not CustomGridGraph gridGraph)
	        {
		        return null;
	        }

	        // Fit grid node counts into area size that is in meters
	        int width = GetClosestLessEvenNodeCount(AreaSize.x, gridGraph.nodeSize);
	        int depth = GetClosestLessEvenNodeCount(AreaSize.y, gridGraph.nodeSize);
	        gridGraph.SetDimensions(width, depth, gridGraph.nodeSize);
	        gridGraph.showNodeConnections = false;
	        astarPath.logPathResults = PathLog.OnlyErrors;
	        astarPath.debugMode = GraphDebugMode.SolidColor;

	        return gridGraph;
        }

        private static void CreateAreaBounds(BlueprintAreaPart area, Vector2Int areaSize)
        {
	        var boundsCreator = ScriptableObject.CreateInstance<AreaPartBoundsCreator>();
	        if (boundsCreator == null)
	        {
		        return;
	        }

	        boundsCreator.Area = area.ToReference<BlueprintAreaPartReference>();
	        var bounds = NewAssetWindow.CreateWithCreator(
		        boundsCreator,
		        area.name + "_Bounds") as AreaPartBounds;
	        if (bounds == null)
	        {
		        return;
	        }

	        float maxSize = Math.Min(areaSize.x, areaSize.y);
	        var size = new Vector3(areaSize.x, maxSize, areaSize.y);
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

	        EditorUtility.SetDirty(bounds);
	        area.Bounds = bounds;
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
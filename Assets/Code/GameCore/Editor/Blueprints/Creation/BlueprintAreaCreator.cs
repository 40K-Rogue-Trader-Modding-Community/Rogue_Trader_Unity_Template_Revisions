using System.Collections.Generic;
using System.Linq;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.AreaStatesWindow;
using Kingmaker.Editor.Blueprints.Creation.Naming;
using Kingmaker.Pathfinding;
using Kingmaker.View;
using UnityEditor;
using Kingmaker.Utility.DotNetExtensions;
using UnityEngine;
using Path = System.IO.Path;

#nullable enable

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class BlueprintAreaCreator : NamingCreatorBase
	{
		public static class SceneTemplates
		{
			public const string MechanicsPostfix = "_Mechanics.unity";
			public const string AddedMechanicsPostfix = "_Mechanics_Add.unity";
			public const string StaticPostfix = "_Static.unity";
			public const string LightPostfix = "_Light.unity";

			private const string Prefix = ScenesRoot + "!Templates/Template";

			private const string DefaultMechanicsPath = Prefix + MechanicsPostfix;
			private const string DefaultAddedMechanicsPath = Prefix + AddedMechanicsPostfix;
			private const string DefaultStaticPath = Prefix + StaticPostfix;
			private const string DefaultLightPath = Prefix + LightPostfix;

			public static string MechanicsPath
				=> Templates.instance == null
					? DefaultMechanicsPath
					: AssetDatabase.GetAssetPath(Templates.instance.BlueprintArea.MechanicsTemplateScene);

			public static string AddedMechanicsPath
				=> Templates.instance == null
					? DefaultAddedMechanicsPath
					: AssetDatabase.GetAssetPath(Templates.instance.BlueprintArea.AddedMechanicsTemplateScene);

			public static string StaticPath
				=> Templates.instance == null
					? DefaultStaticPath
					: AssetDatabase.GetAssetPath(Templates.instance.BlueprintArea.StaticTemplateScene);

			public static string LightPath
				=> Templates.instance == null
					? DefaultLightPath
					: AssetDatabase.GetAssetPath(Templates.instance.BlueprintArea.LightTemplateScene);
		}

		public const string ScenesRoot = "Assets/Scenes/";

		public const string EntranceSuffix = "Enter";

		protected override string NameTokenNotEmpty
			=> "_" + NameToken;

		protected virtual string MechanicsTemplateScenePath
			=> SceneTemplates.MechanicsPath;

		private const string DefaultSceneTemplate = ScenesRoot + "{Chapter}/{Location}/{Location}{name}";
		protected virtual string DefaultMechanicsSceneTemplate => DefaultSceneTemplate + SceneTemplates.MechanicsPostfix;
		private const string DefaultStaticSceneTemplate = DefaultSceneTemplate + SceneTemplates.StaticPostfix;
		private const string DefaultLightSceneTemplate = DefaultSceneTemplate + SceneTemplates.LightPostfix;

		protected virtual string MechanicsSceneTemplate
			=> Templates.instance == null
				? DefaultMechanicsSceneTemplate
				: Templates.instance.BlueprintArea.MechanicsSceneTemplate;

		private static string StaticSceneTemplate
			=> Templates.instance == null
				? DefaultStaticSceneTemplate
				: Templates.instance.BlueprintArea.StaticSceneTemplate;

		private static string LightSceneTemplate
			=> Templates.instance == null
				? DefaultLightSceneTemplate
				: Templates.instance.BlueprintArea.LightSceneTemplate;

		protected virtual string DefaultEntranceSuffix
			=> EntranceSuffix;

		protected override string DefaultFolder
			=> "Blueprints/World/Areas/";

		protected override string DefaultTemplate
			=> "Blueprints/World/Areas/{Chapter}/{Location}/{Location}{name}.jbp";

		protected override string Template
			=> Templates.instance == null
				? DefaultTemplate
				: Templates.instance.BlueprintArea.BlueprintAreaTemplate;

		public override string CreatorName => "Area";

		private AreaPartCreatorEditor? m_AreaPartCreatorEditor;
		private AreaPartCreatorEditor AreaPartCreatorEditor => m_AreaPartCreatorEditor ??= new AreaPartCreatorEditor();

		[Tooltip("If set, standard, properly named etude structure will be created:\n" +
		         "-->This etude as parent\n" +
		         "---->Area root etude\n" +
		         "------>\"Base\" area etude\n" +
		         "------>Are outcomes etude")]
		public BlueprintEtudeReference? AreaParentEtude;

        public override object CreateAsset()
        {
            return new BlueprintArea();
        }

        public override void OnGUI()
        {
	        AreaPartCreatorEditor.OnGui();
	        base.OnGUI();
        }

        public override string ProcessTemplate(string? assetName = null)
        {
	        string result = base.ProcessTemplate(assetName);
	        if (IsFolderOverridden)
	        {
		        string? path = Path.GetDirectoryName(result)?.Replace("\\", "/");
		        string? folderName = Path.GetFileName(path);
		        string filename = Path.GetFileName(result);
		        result = $"{path}/{folderName}{filename}";
	        }
	        return result;
        }

        private string GetScenePathFromTemplate(
	        BlueprintArea area, string sceneTemplate, string defaultSceneTemplate, string scenePostfix)
        {
	        if (IsFolderOverridden)
	        {
		        string assetPath = BlueprintsDatabase.GetAssetPath(area);
		        string? areaFolder = Path.GetDirectoryName(assetPath);
		        string? areaFolderName = Path.GetFileName(areaFolder);
		        string? parentFolder = Path.GetDirectoryName(areaFolder);
		        string? parentFolderName = Path.GetFileName(parentFolder);
		        return $"{ScenesRoot}{parentFolderName}/{areaFolderName}/{area.name}{scenePostfix}";
	        }

	        return UpdateTemplateResult(sceneTemplate, defaultSceneTemplate);
        }

        public override void PostProcess(object asset)
        {
	        var area = (BlueprintArea)asset;

	        string mechanicsPath = GetScenePathFromTemplate(
		        area,MechanicsSceneTemplate, DefaultMechanicsSceneTemplate, SceneTemplates.MechanicsPostfix);
	        string staticPath = GetScenePathFromTemplate(
		        area, StaticSceneTemplate, DefaultStaticSceneTemplate, SceneTemplates.StaticPostfix);
	        string lightPath = GetScenePathFromTemplate(
		        area, LightSceneTemplate, DefaultLightSceneTemplate, SceneTemplates.LightPostfix);

	        var enterPoint = AreaPartCreatorEditor.CreateEnterPoint(area, DefaultEntranceSuffix);

	        AreaPartCreatorEditor.CreateAssets(area, enterPoint,
		        mechanicsPath, staticPath, lightPath,
		        MechanicsTemplateScenePath, SceneTemplates.StaticPath, SceneTemplates.LightPath);

	        CreateEtudeStructure(area);

	        // Create default preset
	        BlueprintAreaPreset? preset = null;
	        var presetCreator = CreateInstance<BlueprintAreaPresetCreator>();
	        if (presetCreator != null)
	        {
		        presetCreator.Area = area.ToReference<BlueprintAreaReference>();
		        preset = NewAssetWindow.CreateWithCreator(
			        presetCreator,
			        "Default") as BlueprintAreaPreset;
		        if (preset != null)
		        {
			        preset.Area = area;
			        preset.EnterPoint = enterPoint;
			        area.DefaultPreset = preset;
		        }
	        }
	        preset?.SetDirty();

	        // Update build scenes list
	        var scenes = new List<SceneReference> { area.DynamicScene, area.StaticScene };
	        scenes.AddRange(area.LightScenes.Where(s => s.IsDefined));
	        scenes = scenes.Distinct().ToList();

	        var buildScenes = scenes
		        .Select(s => s.ScenePath)
		        .Where(p => EditorBuildSettings.scenes.All(s => s.path != p))
		        .Select(p => new EditorBuildSettingsScene(p, true))
		        .ToArray();

	        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(buildScenes).ToArray();

			// Save all stuff
	        AssetDatabase.SaveAssets();
	        BlueprintsDatabase.SaveAllDirty();
	        Selection.activeObject = BlueprintEditorWrapper.Wrap(area);
		}

        public override string CantCreateReason()
        {
	        if (!IsFolderOverridden && AreaParentEtude?.Get() == null)
	        {
		        // Any in-game area requires parent etude to be provided
		        // Area is counted in-game if it is created with standard naming
		        // convention - i.e. the folder is not overridden by any custom value
		        return "Parent etude is not set!";
	        }
	        return base.CantCreateReason();
        }

        private void CreateEtudeStructure(BlueprintArea newlyCreatedArea)
        {
	        var parentEtude = AreaParentEtude?.Get();
	        if (parentEtude == null)
	        {
		        return;
	        }

	        string areaName = NewAssetWindow.AssetName;
	        if (string.IsNullOrEmpty(areaName))
	        {
		        // Something wrong with area name
		        return;
	        }

	        string? parentDir = Path.GetDirectoryName(BlueprintsDatabase.GetAssetPath(parentEtude));
	        if (parentDir == null)
	        {
		        return;
	        }
	        parentDir = @$"{parentDir}\{parentEtude.name}";

	        var rootAreaEtude = BlueprintsDatabase.CreateAsset<BlueprintEtude>(parentDir, areaName);
	        parentEtude.AppendStartWith(rootAreaEtude);
	        parentEtude.SetDirty();

	        string rootAreaDir = $@"{parentDir}\{areaName}";
	        var baseAreaEtude = BlueprintsDatabase.CreateAsset<BlueprintEtude>(rootAreaDir, $"{areaName}_Base");
	        baseAreaEtude.Parent = rootAreaEtude.ToReference<BlueprintEtudeReference>();
	        baseAreaEtude.SetLinkedAreaPart(newlyCreatedArea);
	        baseAreaEtude.SetDirty();

	        rootAreaEtude.Parent = parentEtude.ToReference<BlueprintEtudeReference>();
	        rootAreaEtude.AppendStartWith(baseAreaEtude);
	        rootAreaEtude.SetDirty();

	        // Remember area root etude for creating area state etudes later
	        var areaRootEtudes = AreaRootEtudes.GetInstance();
	        if (areaRootEtudes == null)
	        {
		        return;
	        }
	        var areaEtudes = areaRootEtudes.AreaEtudes.ToList();

	        var areaEtude = areaEtudes.FindOrDefault(ae => ae.Area?.Get() == newlyCreatedArea);
	        if (areaEtude == null)
	        {
		        areaEtude = new AreaRootEtudes.AreaEtude()
		        {
			        Area = newlyCreatedArea.ToReference<BlueprintAreaReference>(),
		        };
		        areaEtudes.Add(areaEtude);
	        }
	        areaEtude.Etude = baseAreaEtude.ToReference<BlueprintEtudeReference>();

	        areaRootEtudes.AreaEtudes = areaEtudes.ToArray();
	        areaRootEtudes.UpdateNames();
	        EditorUtility.SetDirty(areaRootEtudes);
        }
	}
}
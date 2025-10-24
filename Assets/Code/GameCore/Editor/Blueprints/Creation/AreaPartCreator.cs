using Kingmaker.Blueprints.Area;
using System;
using System.IO;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using UnityEditor;

#nullable enable

namespace Kingmaker.Editor.Blueprints.Creation
{
    public class AreaPartCreator : AssetCreatorBase
    {
        public BlueprintAreaReference? Area;

        public override string CreatorName => "Area Part";
        public override string LocationTemplate => GetNewPath();
        
        private AreaPartCreatorEditor? m_AreaPartCreatorEditor;
        private AreaPartCreatorEditor AreaPartCreatorEditor => m_AreaPartCreatorEditor ??= new AreaPartCreatorEditor();

        private string GetNewPath()
        {
            if (Area?.Get() == null)
                return "Assets/Mechanics/Blueprints/World/Areas/{name}.asset";

            string pathToAsset = GetMatchingFolder(BlueprintsDatabase.GetAssetPath(Area));

            return pathToAsset + "/{name}.asset";
        }
        
        public override object CreateAsset()
        {
            return new BlueprintAreaPart();
        }
        
        public override bool CanCreateAssetsOfType(Type type)
        {
            return type == typeof(BlueprintAreaPart);
        }

        public override void OnGUI()
        {
            AreaPartCreatorEditor.OnGui();
            base.OnGUI();
        }

        public override void PostProcess(object asset)
        {
            var areaPart = (BlueprintAreaPart)asset;

            string assetPath = BlueprintsDatabase.GetAssetPath(areaPart);
            string? areaFolder = Path.GetDirectoryName(assetPath);
            string? areaFolderName = Path.GetFileName(areaFolder);
            string? parentFolder = Path.GetDirectoryName(areaFolder);
            string? parentFolderName = Path.GetFileName(parentFolder);
            string basePath = $"{BlueprintAreaCreator.ScenesRoot}{parentFolderName}/{areaFolderName}/{areaPart.name}";

            string mechanicsPath = $"{basePath}{BlueprintAreaCreator.SceneTemplates.MechanicsPostfix}";
            string staticPath = $"{basePath}{BlueprintAreaCreator.SceneTemplates.StaticPostfix}";
            string lightPath = $"{basePath}{BlueprintAreaCreator.SceneTemplates.LightPostfix}";

            var enterPoint = AreaPartCreatorEditor.CreateEnterPoint(Area?.Get(), areaPart, BlueprintAreaCreator.EntranceSuffix);

            AreaPartCreatorEditor.CreateAssets(areaPart, enterPoint,
                mechanicsPath, staticPath, lightPath,
                BlueprintAreaCreator.SceneTemplates.MechanicsPath,
                BlueprintAreaCreator.SceneTemplates.StaticPath,
                BlueprintAreaCreator.SceneTemplates.LightPath);

            var area = Area?.Get();
            if (area != null)
            {
                area.Parts.Add(areaPart.ToReference<BlueprintAreaPartReference>());
                area.SetDirty();
            }

            // Save all stuff
            AssetDatabase.SaveAssets();
            BlueprintsDatabase.SaveAllDirty();
            Selection.activeObject = BlueprintEditorWrapper.Wrap(areaPart);
        }
    }
}
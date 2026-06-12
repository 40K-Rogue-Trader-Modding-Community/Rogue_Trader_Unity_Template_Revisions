using System;
using System.IO;
using System.Text.RegularExpressions;
using Kingmaker.Editor.Utility;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public abstract class AssetCreatorBase: ScriptableObject
	{
		public bool NeedFolderSelection => LocationTemplate.Contains("{folder}");
		
		public bool NeedsAreaReference => LocationTemplate.Contains("{Area}");

		public string Folder { get; set; }

		/*public string CurrentOpenArea  //tmp HOTFIX: WH-25091
			=> FindObjectsOfType<AreaEnterPoint>()?
				.Select(ep => ep.Blueprint)
				.Where(ep => ep != null)
				.Select(ep => ep.Area)
				.FirstOrDefault(p => p)?.ToString();*/


		public abstract string CreatorName { get; }

		public abstract string LocationTemplate { get; }

		public virtual bool CreatesBlueprints => true;

		public virtual string DefaultName => string.Empty;
		
		private static readonly Regex ArrayElementRx = new(@"(\w+)\.Array\.data\[(\d+)\]");

        public abstract object CreateAsset();

        public virtual bool CanCreateAssetsOfType(Type type)
	    {
	        return false;
	    }

        public virtual string CantCreateReason()
        {
	        return null;
        }

        public virtual void Init() {}

        public virtual void PostProcess(object asset) { }
		public virtual void OnGUI() { }

		public virtual void SetRootObject(Object root) { }

		public virtual string ProcessTemplate(string assetName = null)
		{
			return null;
		}

		public string ReplaceTemplates(string path, SerializedObject so)
		{
			return TextTemplates.ReplaceTemplates(TextTemplates.ReplacePropertyNames(path, so), GetAdditionalTemplate);
		}

		protected virtual string GetAdditionalTemplate(string propName)
		{
			return null;
		}

		public virtual bool ShouldSkipProperty(string propName)
		{
			return false;
		}
		
		public virtual string GetNameFromProperty(SerializedProperty prop)
		{
			string objectName = prop.serializedObject.targetObject.name;
			string propName = prop.name;

			if (prop.IsArrayElement())
			{
				// Simplify array element path from "SomeField.Array.data[0]" to "SomeField.0"
				var m = ArrayElementRx.Match(prop.propertyPath);
				if (m.Success)
					propName = $"{m.Groups[1].Value}.{m.Groups[2].Value}";
			}
			// Remove private prefix
			propName = propName.StartsWith("m_") ? propName[2..] : propName;
			
			return $"{objectName}_{propName}";
		}

		protected string GetMatchingFolder(string assetPath)
        {
	        if (assetPath.StartsWith("Assets/Mechanics/Blueprints"))
	        {
		        return assetPath.Substring("Assets/Mechanics/".Length);
	        }

	        if (assetPath.StartsWith("Assets"))
	        {
		        return "Blueprints/" + assetPath.Substring("Assets/".Length);
	        }

	        if (assetPath.EndsWith(".asset"))
	        {
		        return Path.GetDirectoryName(assetPath);
	        }

	        if (assetPath.EndsWith(".jbp"))
	        {
		        return "Assets/Mechanics/" + Path.GetDirectoryName(assetPath);
	        }

	        return assetPath;
        }
	}
}
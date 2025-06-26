using System.Collections.Generic;
using System.Linq;
using Kingmaker.Editor.Utility;
using UnityEditor;
using UnityEngine;

#nullable enable

namespace Kingmaker.Editor.UIElements.Custom
{
	/// <summary>
	/// This is base class for assets, containing property paths layout for CustomFieldsInspector
	/// Also, contains a helper to collect all property paths for component and fill them in.
	/// </summary>
	public abstract class CustomFieldsInspectorLayout<TComponent> : ScriptableObject where TComponent : Component
	{
		[SerializeField]
		[InspectorButton(nameof(AppendMissing), "Append missing property paths")]
		public string Action = "";

		[SerializeField]
		public string[] PropertyPaths = {};

		public abstract List<string> GetAllPropertyPaths(SerializedObject so);

		protected virtual TComponent AddComponent(GameObject go)
		{
			return go.AddComponent<TComponent>();
		}

		public void AppendMissing()
		{
			// Collect all property paths for InteractionSkillCheck
			var tmpGo = new GameObject
			{
				hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy | HideFlags.HideInInspector,
			};
			var component = AddComponent(tmpGo);
			var so = new SerializedObject(component);
			var allPaths = GetAllPropertyPaths(so);

			DestroyImmediate(tmpGo);

			// Leave only missing paths
			foreach (string path in PropertyPaths)
			{
				allPaths.Remove(path);
			}

			// Append missing paths to the end
			PropertyPaths = PropertyPaths.Concat(allPaths).ToArray();
			EditorUtility.SetDirty(this);
		}
	}
}
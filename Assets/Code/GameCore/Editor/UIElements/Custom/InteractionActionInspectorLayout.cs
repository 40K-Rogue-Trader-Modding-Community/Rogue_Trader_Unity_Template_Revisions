using System.Collections.Generic;
using System.Linq;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.View.MapObjects;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.UIElements.Custom
{
	[CreateAssetMenu(menuName = "ScriptableObjects/" + nameof(InteractionActionInspectorLayout), fileName = nameof(InteractionActionInspector))]
	public class InteractionActionInspectorLayout : CustomFieldsInspectorLayout<InteractionAction>
	{
		public override List<string> GetAllPropertyPaths(SerializedObject so)
		{
			var allPaths = new List<string>();
			allPaths.AddRange(so.FindProperty(nameof(InteractionAction.Settings))
				.GetChildren()
				.Select(p => p.propertyPath));
			return allPaths;
		}
	}
}
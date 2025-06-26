using System.Collections.Generic;
using System.Linq;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.View.MapObjects;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.UIElements.Custom
{
	[CreateAssetMenu(menuName = "ScriptableObjects/" + nameof(InteractionSkillCheckInspectorLayout), fileName = nameof(InteractionSkillCheckInspector))]
	public class InteractionSkillCheckInspectorLayout : CustomFieldsInspectorLayout<InteractionSkillCheck>
	{
		public override List<string> GetAllPropertyPaths(SerializedObject so)
		{
			var allPaths = new List<string>();
			allPaths.AddRange(so.FindProperty(nameof(InteractionSkillCheck.Settings))
				.GetChildren()
				.Select(p => p.propertyPath));
			return allPaths;
		}
	}
}
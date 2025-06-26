using System.Collections.Generic;
using Kingmaker.UnitLogic.Interaction;
using Kingmaker.View.Spawners;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.UIElements.Custom
{
    [CreateAssetMenu(menuName = "ScriptableObjects/" + nameof(SpawnerInteractionBarkInspectorLayout), fileName = nameof(SpawnerInteractionBarkInspectorLayout))]
    public class SpawnerInteractionBarkInspectorLayout : CustomFieldsInspectorLayout<SpawnerInteractionBark>
    {
        public override List<string> GetAllPropertyPaths(SerializedObject so)
        {
            var allPaths = new List<string>();
            var it = so.GetIterator();
            bool firstTime = true;
            while (it.NextVisible(firstTime))
            {
                firstTime = false;
                allPaths.Add(it.propertyPath);
            }
            return allPaths;
        }

        protected override SpawnerInteractionBark AddComponent(GameObject go)
        {
            go.AddComponent<UnitSpawner>();
            return go.AddComponent<SpawnerInteractionBark>();
        }
    }
}

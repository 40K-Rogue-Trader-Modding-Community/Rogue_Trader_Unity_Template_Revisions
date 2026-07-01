using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.View;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.Blueprints
{
    [CustomPropertyDrawer(typeof(MultiEntityReferenceDropAttribute))]
    public class MultiEntityReferenceDropDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            // Iterate direct children instead of using PropertyField(property) to avoid
            // recursive invocation of this drawer (Unity calls it per array element and
            // PropertyField on the same property triggers it again)
            var child = property.Copy();
            var end = property.GetEndProperty();
            if (child.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(child, end))
                        break;
                    root.Add(new PropertyField(child.Copy()));
                } while (child.NextVisible(false));
            }

            RegisterDropHandlers(property, root);
            root.Bind(property.serializedObject);
            return root;
        }

        private static void RegisterDropHandlers(SerializedProperty property, VisualElement root)
        {
            var arrayPath = GetParentArrayPath(property.propertyPath);
            if (arrayPath == null)
                return;

            var serializedObject = property.serializedObject;

            // Capture phase: intercept drag before EntityReferenceProperty's DragAndDropComponent
            // Only intercept container/multi drops; let EntityReference handle single LocatorView drops
            root.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (IsDirectLocatorViewDrop())
                    return;
                if (CollectLocators().Count > 0)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    evt.StopPropagation();
                }
            }, TrickleDown.TrickleDown);

            root.RegisterCallback<DragPerformEvent>(evt =>
            {
                if (IsDirectLocatorViewDrop())
                    return;
                var locators = CollectLocators();
                if (locators.Count == 0)
                    return;
                var arrayProp = serializedObject.FindProperty(arrayPath);
                if (arrayProp == null)
                    return;
                DragAndDrop.AcceptDrag();
                evt.StopPropagation();
                FillArray(arrayProp, locators);
            }, TrickleDown.TrickleDown);
        }

        private static void FillArray(SerializedProperty arrayProp, List<LocatorView> locators)
        {
            // Remove trailing empty entries so they are replaced rather than appended after
            while (arrayProp.arraySize > 0)
            {
                var last = arrayProp.GetArrayElementAtIndex(arrayProp.arraySize - 1);
                if (IsEmptyEntry(last))
                    arrayProp.DeleteArrayElementAtIndex(arrayProp.arraySize - 1);
                else
                    break;
            }

            int startIndex = arrayProp.arraySize;
            arrayProp.arraySize += locators.Count;

            for (int i = 0; i < locators.Count; i++)
            {
                var entryProp = arrayProp.GetArrayElementAtIndex(startIndex + i);
                FillEntry(entryProp, locators[i]);
            }

            arrayProp.serializedObject.ApplyModifiedProperties();
        }

        // Supports both EntityReference[] and Target[] (Target has a nested Entity: EntityReference field)
        private static void FillEntry(SerializedProperty entryProp, LocatorView locator)
        {
            var entityProp = entryProp.FindPropertyRelative("Entity") ?? entryProp;

            var uidProp = entityProp.FindPropertyRelative("UniqueId");
            var nameProp = entityProp.FindPropertyRelative("EntityNameInEditor");
            var sceneProp = entityProp.FindPropertyRelative("SceneAssetGuid");

            if (uidProp == null)
                return;

            uidProp.stringValue = locator.UniqueId;
            nameProp.stringValue = locator.name;
            sceneProp.stringValue = AssetDatabase.AssetPathToGUID(locator.gameObject.scene.path);

            // Clear Unit evaluator on newly added entries (Unity copies the last element on array resize)
            var unitProp = entryProp.FindPropertyRelative("Unit");
            if (unitProp != null && unitProp.propertyType == SerializedPropertyType.ManagedReference)
                unitProp.managedReferenceValue = null;
        }

        private static bool IsEmptyEntry(SerializedProperty entryProp)
        {
            var entityProp = entryProp.FindPropertyRelative("Entity") ?? entryProp;
            var uidProp = entityProp.FindPropertyRelative("UniqueId");
            return uidProp == null || string.IsNullOrEmpty(uidProp.stringValue);
        }

        // A "direct" drop is a single GameObject with LocatorView — let EntityReference handle it normally
        private static bool IsDirectLocatorViewDrop()
            => DragAndDrop.objectReferences.Length == 1
               && DragAndDrop.objectReferences[0] is GameObject go
               && go.GetComponent<LocatorView>() != null;

        private static List<LocatorView> CollectLocators()
        {
            var result = new List<LocatorView>();
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is not GameObject go)
                    continue;
                var direct = go.GetComponent<LocatorView>();
                if (direct != null)
                    result.Add(direct);
                else
                    result.AddRange(go.GetComponentsInChildren<LocatorView>());
            }
            return result;
        }

        private static string GetParentArrayPath(string elementPath)
        {
            var idx = elementPath.IndexOf(".Array.data[");
            return idx >= 0 ? elementPath.Substring(0, idx) : null;
        }
    }
}

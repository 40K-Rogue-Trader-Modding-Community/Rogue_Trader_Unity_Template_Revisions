using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

#nullable enable

namespace Kingmaker.Utility.UnityExtensions
{
    /// <summary>
    /// This class is dedicated to some functions that allow to collapse
    /// scene hierarchy to the very top level in hierarchy window.
    /// It relies on internal UnityEditor stuff that is used via reflection
    /// </summary>
    public static class HierarchyWindowHelper
    {
        public static class Reflection
        {
            private static Type? hierarchyWindowType;
            public static Type? HierarchyWindowType => hierarchyWindowType
                ??= typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");

            private static Type? hierarchyType;
            public static Type? HierarchyType => hierarchyType
                ??= HierarchyWindowType?.Assembly.GetType("UnityEditor.SceneHierarchy");

            private static FieldInfo? hierarchyField;
            public static FieldInfo? HierarchyField => hierarchyField
                ??= HierarchyWindowType?.GetField(
                    "m_SceneHierarchy",
                    BindingFlags.NonPublic | BindingFlags.Instance);

            private static FieldInfo? handleField;
            public static FieldInfo? HandleField => handleField
                ??= typeof(Scene).GetField(
                    "m_Handle",
                    BindingFlags.NonPublic | BindingFlags.Instance);

            private static MethodInfo? expandMethod;
            public static MethodInfo? ExpandMethod => expandMethod
                ??= HierarchyType?.GetMethod("ExpandTreeViewItem",
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new[] {typeof(int), typeof(bool)},
                    null);
        }

        public static void CollapseScene(Scene scene)
        {
            if (Reflection.HierarchyWindowType == null)
            {
                Debug.LogError("SceneHierarchyWindow type not found.");
                return;
            }

            // Get the active hierarchy window
            var hierarchyWindow = EditorWindow.GetWindow(Reflection.HierarchyWindowType);

            if (hierarchyWindow == null)
            {
                Debug.LogError("Hierarchy window not found.");
                return;
            }

            if (Reflection.HierarchyField == null)
            {
                Debug.LogError("Failed to find field 'SceneHierarchyWindow.m_SceneHierarchy'");
                return;
            }

            object hierarchy = Reflection.HierarchyField.GetValue(hierarchyWindow);

            if (Reflection.HierarchyType == null)
            {
                Debug.LogError("SceneHierarchy type not found.");
                return;
            }

            if (Reflection.ExpandMethod == null)
            {
                Debug.LogError(
                    "SceneHierarchy.ExpandTreeViewItem method not found. Unity version may not support this.");
                return;
            }

            if (Reflection.HandleField == null)
            {
                Debug.LogError("Failed to find field 'Scene.m_Handle'");
                return;
            }

            object sceneHandle = Reflection.HandleField.GetValue(scene);
            Reflection.ExpandMethod.Invoke(hierarchy, new[] {sceneHandle, false});
        }

        public static void CollapseAllScenes()
        {
            for (int sceneIdx = 0; sceneIdx < SceneManager.sceneCount; sceneIdx++)
            {
                var scene = SceneManager.GetSceneAt(sceneIdx);
                if (!scene.isLoaded)
                {
                    continue;
                }

                CollapseScene(scene);
            }
        }
    }
}

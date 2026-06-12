using System;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Kingmaker.Editor.UIElements
{
    [InitializeOnLoad]
    public class InspectorReload
    {
        static InspectorReload()
        {
            AssemblyReloadEvents.afterAssemblyReload += OnAfterReload;
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
        }
    
        private static void PlayModeStateChanged(PlayModeStateChange stateChange)
        {
            if (stateChange is PlayModeStateChange.EnteredEditMode or PlayModeStateChange.EnteredPlayMode)
                ReloadInspector();
        }
    
        private static void OnAfterReload()
        {
            ReloadInspector();
        }
        
        private static void ReloadInspector()
        {
            EditorApplication.delayCall += () =>
            {
                var selected = Selection.objects;
                Selection.objects = Array.Empty<Object>();
                EditorApplication.delayCall += () =>
                {
                    Selection.objects = selected;
                };
            };
        }
    }
}
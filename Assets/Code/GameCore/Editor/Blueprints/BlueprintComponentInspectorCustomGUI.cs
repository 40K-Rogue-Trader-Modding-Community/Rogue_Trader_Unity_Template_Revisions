using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;

namespace Kingmaker.Editor.Blueprints
{
    public abstract class BlueprintComponentInspectorCustomGUI
    {
        private static readonly Dictionary<Type, Type> CustomAdditions = new();

        static BlueprintComponentInspectorCustomGUI()
        {
            var customGuis = TypeCache.GetTypesDerivedFrom<BlueprintComponentInspectorCustomGUI>()
                .Where(t => !t.IsAbstract);

            foreach (var customGUI in customGuis)
            {
                var attr = customGUI.GetAttribute<BlueprintCustomEditorAttribute>();
                if (attr == null) 
                    continue;
                var inspectedType = attr.InspectedType;
                CustomAdditions[inspectedType] = customGUI;
            }
        }
        
        public static BlueprintComponentInspectorCustomGUI GetForType(Type t)
        {
            if (t == typeof(BlueprintComponent) || t.BaseType==null)
                return null;
            
            if(CustomAdditions.TryGetValue(t, out var gui))
            {
                return (BlueprintComponentInspectorCustomGUI)Activator.CreateInstance(gui);
            }

            return GetForType(t.BaseType);
        }
        
        public static bool TryGetForType(Type t, out BlueprintComponentInspectorCustomGUI inspector)
        {
            inspector = GetForType(t);
            return inspector != null;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Attributes;
using Kingmaker.ElementsSystem;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Kingmaker.Editor.Blueprints
{
	public class ClassNames
	{
		private static readonly Dictionary<Type, NamesData> s_Names = new Dictionary<Type, NamesData>();

		static ClassNames()
		{
			var types = 
					TypeCache.GetTypesDerivedFrom(typeof(BlueprintScriptableObject)).Where(t => !t.IsAbstract)
					.Concat(TypeCache.GetTypesDerivedFrom(typeof(BlueprintComponent)).Where(t=> !t.IsAbstract))
					.Concat(TypeCache.GetTypesDerivedFrom(typeof(Element)).Where(t=> !t.IsAbstract))
					.Where(t => t.HasAttribute<ComponentNameAttribute>() || t.HasAttribute<GroupAttribute>());

			foreach (var type in types)
			{
				var componentNameAttribute = type.GetCustomAttribute(typeof(ComponentNameAttribute), false) 
					as ComponentNameAttribute;
				var groupAttribute = type.GetCustomAttribute(typeof(GroupAttribute), true) as GroupAttribute;
				
				var stringBuilder = new StringBuilder();
				if (componentNameAttribute != null)
				{
					stringBuilder.Append(componentNameAttribute.Name);
				} 
				else if (groupAttribute != null)
				{
					stringBuilder.Append(groupAttribute.Name);
					stringBuilder.Append('/');
					stringBuilder.Append(type.Name);
				}
				
				if (stringBuilder.Length > 0)
				{
					string componentPath = stringBuilder.ToString();
					int slash = componentPath.LastIndexOf("/", StringComparison.Ordinal);
					s_Names[type] = new NamesData
					{
						NameWithPrefix = slash > 0 || groupAttribute == null ? componentPath : 
							groupAttribute.Name + '/' + componentPath,
						NameWithoutPrefix = slash < 0 ? componentPath : componentPath.Substring(slash + 1)
					};
				}
			}
		}

		public static string GetClassName(Type type)
		{
			NamesData data;
			if (s_Names.TryGetValue(type, out data))
				return data.NameWithPrefix;
			return type.Name;
		}

		public static string GetClassNameNoPrefix(Type type)
		{
			NamesData data;
			if (s_Names.TryGetValue(type, out data))
				return data.NameWithoutPrefix;
			return type.Name;
		}

		public static string GetObjectNameNoPrefix(object obj)
		{
			NamesData data;
			if (s_Names.TryGetValue(obj.GetType(), out data))
				return data.NameWithoutPrefix;
            if(obj is Object unityObject)
			    return ObjectNames.GetClassName(unityObject);
            return obj.GetType().Name;
        }

		private class NamesData
		{
			public string NameWithoutPrefix;
			public string NameWithPrefix;
		}
	}
}
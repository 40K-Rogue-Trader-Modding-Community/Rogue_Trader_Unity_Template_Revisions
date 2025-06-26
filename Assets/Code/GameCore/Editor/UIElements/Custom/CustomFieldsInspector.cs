using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Elements;
using Kingmaker.Utility.EditorPreferences;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

#nullable enable

namespace Kingmaker.Editor.UIElements.Custom
{
	/// <summary>
	/// Base class for creation of component inspectors with custom fields layout, taken
	/// from the asset, containing property paths in some order and divided by named sections.
	/// A property section is defined as an entry in property path list, starting with '>'
	/// </summary>
	public abstract class CustomFieldsInspector<TComponent> : UnityEditor.Editor where TComponent : Component
	{
		/// <summary>
		/// Unique key to store foldout state of property sections
		/// </summary>
		protected abstract string DataKeyRoot { get; }

		/// <summary>
		/// Asset path fields layout is stored in
		/// </summary>
		protected abstract string LayoutAssetPath { get; }

		private class PropertySection
		{
			public string Name { get; }
			public string[] PropertyPaths { get; }
			public PropertySection(string name, string[] propertyPaths)
			{
				Name = name;
				PropertyPaths = propertyPaths;
			}
		}

		private PropertySection? m_Remaining;

		private readonly List<string> m_AllPaths = new();

		private readonly List<OwlcatVisualElement> m_PropertiesLayout = new(256);

		public override VisualElement CreateInspectorGUI()
		{
			if (!EditorPreferences.Instance.UseNewEditor)
			{
				return new IMGUIContainer(OnInspectorGUI);
			}

			CreateLayout();

			var inspectorRoot = new OwlcatInspectorRoot(serializedObject, m_PropertiesLayout);
			return inspectorRoot;
		}

		private void CreateLayout()
		{
			var layout = AssetDatabase.LoadAssetAtPath<CustomFieldsInspectorLayout<TComponent>>(LayoutAssetPath);
			if (layout == null)
			{
				Debug.LogError("Failed to load InspectorLayout from " + LayoutAssetPath);
				return;
			}

			// Collect all paths to remember all paths that left unprocessed
			m_AllPaths.Clear();
			m_AllPaths.AddRange(layout.GetAllPropertyPaths(serializedObject));

			m_Remaining = null;
			m_PropertiesLayout.Clear();

			// GetLayoutFromCode();
			GetLayoutFromAsset(layout);

			if (m_AllPaths.Count > 0)
			{
				// Generate foldout for remaining properties
				m_Remaining = new PropertySection("_Unsorted", m_AllPaths.ToArray());
				m_PropertiesLayout.Add(GenerateFoldout(m_Remaining));
			}
		}

		private void GetLayoutFromAsset(CustomFieldsInspectorLayout<TComponent> layout)
		{
			string? currentSectionName = null;
			List<string> paths = new(256);
			List<string> basePaths = new(256);
			List<PropertySection> sections = new(8);
			foreach (string path in layout.PropertyPaths)
			{
				if (string.IsNullOrEmpty(path))
				{
					continue;
				}

				if (!path.StartsWith(">"))
				{
					// Just a path
					paths.Add(path);
					continue;
				}

				// New section started
				if (currentSectionName == null)
				{
					// No section were found yet.
					// So, all collected paths are base properties
					basePaths.AddRange(paths);
				}
				else
				{
					// Previous section ended - store it
					sections.Add(new PropertySection(currentSectionName, paths.ToArray()));
				}

				// Start collecting paths for next section
				currentSectionName = path[1..];
				paths.Clear();
			}

			// Add base properties
			AddPropertiesByPaths(currentSectionName == null ? paths : basePaths, e => m_PropertiesLayout.Add(e));

			if (currentSectionName != null && paths.Count > 0)
			{
				// Store last section
				sections.Add(new PropertySection(currentSectionName, paths.ToArray()));
			}

			// Generate foldouts for custom-layout properties
			m_PropertiesLayout.AddRange(sections.Select(GenerateFoldout));
		}

		public override void OnInspectorGUI()
		{
			PrototypedObjectEditorUtility.DisplayProperties(serializedObject);
		}

		private OwlcatFoldout GenerateFoldout(PropertySection section)
		{
			var foldout = new OwlcatFoldout(
				section.Name,
				$"{DataKeyRoot}.{section.Name}"); // To store global foldout state
			// $"{serializedObject.targetObject.GetInstanceID()}.{section.Name}"); // To store per-object foldout state
			AddPropertiesByPaths(section.PropertyPaths, e => foldout.Add(e));
			return foldout;
		}

		private void AddPropertiesByPaths(IEnumerable<string> paths, Action<OwlcatVisualElement> add)
		{
			foreach (string path in paths)
			{
				var property = serializedObject.FindProperty(path);
				if (property == null)
				{
					continue;
				}

				add(UIElementsUtility.CreatePropertyElement(property, false));
				if (m_Remaining == null)
				{
					// Section with remaining properties was not created yet
					// So - remove current path to have only remaining ones at the end
					m_AllPaths.Remove(path);
				}
			}
		}
	}
}
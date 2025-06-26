using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

#nullable enable

namespace Kingmaker.Editor.Blueprints.Creation.Naming
{
	[FilePath("Assets/Editor/Naming/" + nameof(LocationsByChapter) + ".asset", FilePathAttribute.Location.ProjectFolder)]
	public class LocationsByChapter : ScriptableSingleton<LocationsByChapter>
	{
		[Serializable]
		public class ChapterLocations
		{
			public Chapter? Chapter;
			public Location[]? Locations;
		}

		[SerializeField]
		public ChapterLocations[]? Items;

		public IEnumerable<string>? GetLocationNames(string chapterName)
		{

			var locations = Items?
				.FirstOrDefault(item => item.Chapter != null && item.Chapter.name == chapterName)?
				.Locations?.Where(l => l != null);
			return locations?.Select(location => location.name);
		}
	}
}
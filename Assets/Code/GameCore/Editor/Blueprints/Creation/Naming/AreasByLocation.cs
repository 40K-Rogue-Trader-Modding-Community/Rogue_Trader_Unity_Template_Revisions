using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

#nullable enable

namespace Kingmaker.Editor.Blueprints.Creation.Naming
{
    [FilePath("Assets/Editor/Naming/" + nameof(AreasByLocation) + ".asset", FilePathAttribute.Location.ProjectFolder)]
    public class AreasByLocation : ScriptableSingleton<AreasByLocation>
    {
        [Serializable]
        public class LocationAreas
        {
            public Location? Location;
            public Location[]? Areas;
        }

        [SerializeField]
        public LocationAreas[]? Items;

        public IEnumerable<string>? GetAreaNames(string locationName)
        {
            var locations = Items?
                .FirstOrDefault(item => item.Location != null && item.Location.name == locationName)?
                .Areas?.Where(a => a != null);
            return locations?.Select(location => location.name);
        }
    }
}
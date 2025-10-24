using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Owlcat.Blueprints.Server.FileDatabase
{
    public interface IFileReader
    {
        IFileData ReadFile(string path, ILogger logger);
    }

    partial class FileReader : IFileReader
    {
        [GeneratedRegex("!bp_[0-9a-z]{32}")]
        private static partial Regex BlueprintReferenceRegex();
        
        [GeneratedRegex("\"_entity_id\": \"([^\"]+)\"")]
        private static partial Regex EntityReferenceRegex();
        
        class TypeHolder
        {
            [JsonPropertyName("$type")]
            public string TypeId { get; set; }
        }

        class Meta
        {
            [JsonPropertyName("ShadowDeleted")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public bool? ShadowDeleted { get; set; }
        }
        
        class FileData : IFileData
        {
            [JsonIgnore]
            public string Name { get; set; }

            [JsonPropertyName("AssetId")]
            public string UniqueId { get; set; }

            [JsonPropertyName("Data")]
            public TypeHolder Data { get; set; }

            [JsonPropertyName("Meta")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public Meta? Meta { get; set; }

            [JsonIgnore] 
            public HashSet<string> ReferencedBlueprints { get; set; }

            [JsonIgnore]
            public HashSet<string> ReferencedEntities { get; set; }

            [JsonIgnore]
            public string TypeId
            {
                get => Data.TypeId;
                set => Data.TypeId = value;
            }

            [JsonIgnore]
            public bool IsShadowDeleted => Meta is { ShadowDeleted: not null } && Meta.ShadowDeleted.Value;
        }

        public IFileData ReadFile(string path, ILogger logger)
        {
            // todo: when this gets called from a FileSystemWatcher callback, the file might be IN THE PROCESS
            // OF BEING WRITTEN TO, and we'll read wrong json. It does not seem to happen right now with Unity,
            // but may fail when git/svn updates or something
            var text = FileUtility.ReadAllText(path, logger); // todo: we need ony the first few lines if all blueprints are well-formed
            var readFile = JsonSerializer.Deserialize<FileData>(text);
            if (readFile.TypeId.Length > 32)
            {
                readFile.TypeId = readFile.TypeId[..32]; // cut to guid length
            }

            readFile.Name = Path.GetFileName(path); // do NOT try to read it from json, make it match file name always

            var blueprintReferenceMatches = BlueprintReferenceRegex().Matches(text);
            readFile.ReferencedBlueprints = new HashSet<string>(blueprintReferenceMatches.Count);
            foreach (Match bpRefMatch in blueprintReferenceMatches)
            {
                string id = bpRefMatch.Value[4..];
                readFile.ReferencedBlueprints.Add(id);
            }

            var entityReferenceMatches = EntityReferenceRegex().Matches(text);
            readFile.ReferencedEntities = new HashSet<string>(entityReferenceMatches.Count);
            foreach (Match entityRefMatch in entityReferenceMatches)
            {
                string id = entityRefMatch.Groups[1].Value;
                readFile.ReferencedEntities.Add(id);
            }

            return readFile;
        }
    }
}
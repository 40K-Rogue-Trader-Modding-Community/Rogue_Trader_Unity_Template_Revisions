using System.IO;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using UnityEngine.Serialization;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class BlueprintUnitCreator : CreatorWithArea
	{
		public enum UnitType
		{
			Reference,
			Monster,
			Boss,
			NPC
		}

		public enum NpcKind
		{
			Drukhari,
			Eldar,
			Human,
			Mutant,
			SpaceMarine,
		}

		public UnitType Type;
		[FormerlySerializedAs("NpcKind")]
		public NpcKind NpcType;
		public BlueprintUnitReference Prototype;

		public override string CreatorName => "Unit";
		public override string LocationTemplate 
		{
			get
			{
				switch (Type)
				{
						case UnitType.Reference:
							return "Assets/Mechanics/Blueprints/Units/Monsters/{folder}/{name}.asset";
						case UnitType.Monster:
							return "{prototype_path}/{Area}/{name}.asset";
						case UnitType.Boss:
							return "Assets/Mechanics/Blueprints/Units/Bosses/{name}/{name}.asset";
						case UnitType.NPC:
							return "Assets/Mechanics/Blueprints/Units/NPC/{folder}/{NpcType}/{Area}/{name}.asset";
				}
				return "{wtf}";
			}
		}
		
        public override object CreateAsset()
        {
            return new BlueprintUnit();
        }

		public override void PostProcess(object asset)
		{
			var u = (BlueprintUnit) asset;
			// todo: [bp] fix when prototypes are a thing
			u.SetPrototype(Prototype.Get());
			u.SetDirty();
		}

		public override bool ShouldSkipProperty(string propName)
		{
			return propName switch
			{
				nameof(Area) => Type != UnitType.Monster && Type != UnitType.NPC,

				nameof(NpcType) => Type != UnitType.NPC,

				_ => base.ShouldSkipProperty(propName)
			};
		}

		protected override string GetAdditionalTemplate(string propName)
		{
			return propName switch
			{
				"prototype_path" => Prototype.Get()
					? Path.GetDirectoryName(BlueprintsDatabase.GetAssetPath(Prototype))
					: null,

				nameof(NpcType) => NpcType.ToString(),

				_ => base.GetAdditionalTemplate(propName)
			};
		}
	}
}
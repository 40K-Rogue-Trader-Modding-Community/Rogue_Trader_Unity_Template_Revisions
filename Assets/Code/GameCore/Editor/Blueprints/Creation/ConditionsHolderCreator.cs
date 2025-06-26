using System;
using Kingmaker.ElementsSystem;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class ConditionsHolderCreator : CreatorWithArea
	{
		public override string CreatorName => "Conditions holder";
		public override string DefaultName => "ConditionsHolder";

		public override string LocationTemplate => "Assets/Mechanics/Blueprints/World/Encounters/{Area}/ConditionsHolders/{name}.asset";


        public override object CreateAsset()
        {
            return new ConditionsHolder();
        }

        public override bool CanCreateAssetsOfType(Type type)
        {
	        return type == typeof(ConditionsHolder);
        }
    }
}
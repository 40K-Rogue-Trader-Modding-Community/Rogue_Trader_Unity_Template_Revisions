using System;
using Kingmaker.ElementsSystem;

namespace Kingmaker.Editor.Blueprints.Creation
{
    public class ActionsHolderCreator : CreatorWithArea
    {
        public override string CreatorName
            => "Actions holder";

        public override string DefaultName
            => "ActionsHolder";

        public override string LocationTemplate
            => "Assets/Mechanics/Blueprints/World/Encounters/{Area}/ActionsHolders/{name}.asset";

        public override object CreateAsset()
        {
            return new ActionsHolder();
        }

        public override bool CanCreateAssetsOfType(Type type)
        {
            return type == typeof(ActionsHolder);
        }
    }
}
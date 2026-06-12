using Kingmaker.Blueprints.Items;

#nullable enable

namespace Kingmaker.Editor.Blueprints.Creation
{
    /// <summary>
    /// This item creator is devoted to quest items creation directly by LD
    /// </summary>
    public class QuestItemCreator : CreatorWithArea
    {
        public override string CreatorName => "Quest Item";

        public override string LocationTemplate
            => "Blueprints/World/Encounters/{Area}/Items/{name}.jbp";

        public override object CreateAsset()
        {
            return new BlueprintItem();
        }
    }
}
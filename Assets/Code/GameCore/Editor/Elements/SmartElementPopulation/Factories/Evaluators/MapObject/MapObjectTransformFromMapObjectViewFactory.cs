using Code.GameCore.EntitySystem.Entities;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.View.MapObjects;

namespace Kingmaker.Editor.Elements.SmartElementPopulation.Factories.Evaluators.Unit
{
    [UsedImplicitly]
    public class MapObjectTransformFromMapObjectViewFactory : ElementFactory<MapObjectTransform, MapObjectView>
    {
        protected override void Populate(SimpleBlueprint owner, MapObjectTransform element, MapObjectView source)
        {
            element.MapObject = EntityViewBaseExtension.ToEditorEntityReference(source);
        }
    }
}

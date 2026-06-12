using Kingmaker.ResourceLinks;
using Kingmaker.View.Mechanics.Entities;
using UnityEditor;

namespace Kingmaker.Editor.Blueprints
{
    [CustomPropertyDrawer(typeof(DestructibleObjectViewLink))]
    public class DestructibleObjectLinkDrawer : WeakLinkDrawer<DestructibleEntityView>
    {
    }
}
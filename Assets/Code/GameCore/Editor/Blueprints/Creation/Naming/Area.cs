using UnityEngine;

namespace Kingmaker.Editor.Blueprints.Creation.Naming
{
    [CreateAssetMenu(
        menuName = Str.MenuPrefix + nameof(Area),
        order = (int)NamingMenuOrder.Area,
        fileName = Str.RootFolder + nameof(Area) + "/NewName")]
    public class Area : Location
    {
    }
}
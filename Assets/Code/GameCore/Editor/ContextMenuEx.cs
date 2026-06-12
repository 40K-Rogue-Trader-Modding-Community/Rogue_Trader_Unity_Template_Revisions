using UnityEngine.UIElements;

namespace Code.Editor.Utility
{
    public static class ContextMenuEx
    {
        public static bool TryAddSeparator(this ContextualMenuPopulateEvent evt)
        {
            if (evt.menu.MenuItems().Count == 0)
                return false;
            
            if (evt.menu.MenuItems()[^1] is DropdownMenuSeparator)
                return false;
            
            evt.menu.AppendSeparator();
            return true;
        }
        
        public static bool TryRemoveItem(this ContextualMenuPopulateEvent evt, string item)
        {
            var menuItems = evt.menu.MenuItems();
            for (int i = 0; i < menuItems.Count; i++)
            {
                if (menuItems[i] is DropdownMenuAction action && action.name.Equals(item))
                {
                    evt.menu.RemoveItemAt(i);
                    return true;
                }
            }

            return false;
        }
    }
}
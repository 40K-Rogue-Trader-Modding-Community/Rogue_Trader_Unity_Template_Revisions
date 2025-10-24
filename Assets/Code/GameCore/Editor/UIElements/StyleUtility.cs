using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements
{
    public static class StyleUtility
    {
        public static void SetPadding(IStyle style, float padding)
        {
            style.paddingBottom = padding;
            style.paddingLeft = padding;
            style.paddingRight = padding;
            style.paddingTop = padding;
        }

        public static void SetBorder(IStyle style, float border)
        {
            style.borderBottomWidth = border;
            style.borderLeftWidth = border;
            style.borderRightWidth = border;
            style.borderTopWidth = border;
        }

        public static void SetBorderColor(IStyle style, Color color)
        {
            style.borderBottomColor = color;
            style.borderLeftColor = color;
            style.borderRightColor = color;
            style.borderTopColor = color;
        }

        public static void SetBorderRadius(IStyle style, float radius)
        {
            style.borderBottomLeftRadius = radius;
            style.borderBottomRightRadius = radius;
            style.borderTopLeftRadius = radius;
            style.borderTopRightRadius = radius;
        }
    }
}
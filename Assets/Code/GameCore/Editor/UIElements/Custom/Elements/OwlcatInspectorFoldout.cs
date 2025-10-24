using Kingmaker.Editor.UIElements.Custom.Base;
using UnityEngine.UIElements;

#nullable enable

namespace Kingmaker.Editor.UIElements.Custom.Elements
{
    /// <summary>
    /// This is a foldout element that matches the style of other properties
    /// in inspector and is able to remember it's foldout state
    /// </summary>
    public class OwlcatInspectorFoldout : OwlcatPropertyLayout
    {
        /// <summary>
        /// A key in global scope to store this foldout state
        /// </summary>
        private string StateDataKey { get; }

        public OwlcatInspectorFoldout(string? stateDataKey = null) : base(Layout.Vertical, true)
        {
            StateDataKey = stateDataKey ?? nameof(OwlcatInspectorFoldout);
            IsExpanded = GetSavedExpandedState();
        }

        protected override void SwitchExpanded(MouseDownEvent evt)
        {
            base.SwitchExpanded(evt);
            UIElementsUtility.SetExpandedState(GetExpandedPath(), IsExpanded);
        }

        protected override string GetExpandedPath()
        {
            return StateDataKey; // To store global foldout state
        }
    }
}
using Kingmaker.Editor.UIElements.Custom.Base;

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

        public override bool IsExpanded 
        {
            get => base.IsExpanded;
            set
            {
                base.IsExpanded = value;
                UIElementsUtility.SetExpandedState(StateDataKey, IsExpanded);
            }
        }

        public OwlcatInspectorFoldout(string? stateDataKey = null) : base(Layout.Vertical,
            UIElementsUtility.GetExpandedState(stateDataKey ?? nameof(OwlcatInspectorFoldout)))
        {
            StateDataKey = stateDataKey ?? nameof(OwlcatInspectorFoldout);
        }
    }
}
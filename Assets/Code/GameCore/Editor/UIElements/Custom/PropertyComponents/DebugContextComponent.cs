using System;
using Code.GameCore.Editor.Elements.Debug;
using ElementsSystem.Debug;
using Kingmaker.Utility.EditorPreferences;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.PropertyComponents
{
    public class DebugContextComponent : OwlcatPropertyComponent
    {
        private const string Title = "Debug Context";
        private const int UpdateVisibilityTimerMs = 300;
        
        private readonly Func<ContextDebugData> _debugContextGetter;
        
        private Button _button;
        private ContextDebugData _currentContextDebugData;

        public DebugContextComponent(Func<ContextDebugData> debugContextGetter)
        {
            _debugContextGetter = debugContextGetter;
        }

        protected override void OnAttached()
        {
            _button = new Button(OpenDebugContext) { text = Title };
            Property.ControlsContainer.Add(_button);
            UpdateVisibility();
            
            Property.schedule.Execute(UpdateVisibility).Every(UpdateVisibilityTimerMs);
        }

        private void OpenDebugContext()
        {
            if (_currentContextDebugData?.StringData != null)
                ContextDebugWindow.Show(Title, _currentContextDebugData.StringData);
        }

        private void UpdateVisibility()
        {
            _currentContextDebugData = _debugContextGetter?.Invoke();
            bool isVisible = EditorPreferences.Instance.EnableContextDebugger && _currentContextDebugData != null;
            
            _button.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
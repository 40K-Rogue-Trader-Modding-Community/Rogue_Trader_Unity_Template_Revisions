using System;

#nullable enable

namespace Kingmaker.Editor.AreaStatesWindow
{
    public class NewStateForm : FormWindowBase<NewStateForm>
    {
        private string? _areaName;
        private Action<(string stateName, string sceneName, string sceneTemplatePath)>? _createState;

        public static void Present(
            string areaName,
            Action<(string stateName, string sceneName, string sceneTemplatePath)>? createState)
        {
            Present("Create new state", window =>
            {
                window._areaName = areaName;
                window._createState = createState;
            });
        }

        protected override void FillContent()
        {
            if (_content == null || _areaName == null || _createState == null)
            {
                return;
            }

            // State name input
            var stateNameField = new NewStateElement(_areaName, _createState, Close);
            _content.Add(stateNameField);
        }
    }
}
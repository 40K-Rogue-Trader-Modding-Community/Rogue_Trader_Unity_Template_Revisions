using System;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.UIElements.Custom.Elements;
using UnityEditor;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom
{
    public class BlueprintScriptProperty : OwlcatPropertyLayout
    {
        private readonly Type m_Type;
        private readonly UnityEngine.Object m_Script;

        public BlueprintScriptProperty(Type type) : base(Layout.Horizontal)
        {
            m_Type = type;
            BlueprintsDatabase.Binder.BindToName(m_Type, out _, out string guid);
            m_Script = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(MonoScript));

            var field = new OwlcatGenericObjectField(null, null, null);
            field.Display.RegisterCallback<MouseDownEvent>(OnClick); //,VisualElementEx.InvokePolicy.IncludeDisabled);
            field.SelectButton.style.display = DisplayStyle.None;
            field.UpdateView(m_Script, m_Type);

            TitleLabel.text = "Script";
            ContentContainer.Add(field);
            var button = new VisualElement();
            button.style.width = 23;
            ControlsContainer.Add(button);
        }

        private void OnClick(MouseDownEvent evt)
        {
            BlueprintsDatabase.Binder.BindToName(m_Type, out _, out string guid);
            var script = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(MonoScript));
            if (evt.clickCount > 1 && m_Script)
            {
                AssetDatabase.OpenAsset(m_Script);
            }
            else
            {
                EditorGUIUtility.PingObject(m_Script);
            }

            evt.StopPropagation();
        }
    }
}
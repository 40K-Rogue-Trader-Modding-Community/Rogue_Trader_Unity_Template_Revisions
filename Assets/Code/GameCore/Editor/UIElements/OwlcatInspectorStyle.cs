using Kingmaker.Editor.UIElements.Custom.Base;
using UnityEditor;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements
{
    public class OwlcatInspectorStyle : OwlcatContentContainer
    {
        //Paths may change in nearest future. This is left to make search easier
#if OWLCAT_MODS
		public const string CommonPath = "Assets/Code/GameCore/Editor/UIElements/Styles/CommonStyle.uss";
		public const string ProPath = "Assets/Code/GameCore/Editor/UIElements/Styles/ProStyle.uss";
		public const string PersonalPath = "Assets/GameCore/Code/Editor/UIElements/Styles/PersonalStyle.uss";
#else
        public const string CommonPath = "Assets/Code/GameCore/Editor/UIElements/Styles/CommonStyle.uss";
        public const string ProPath = "Assets/Code/GameCore/Editor/UIElements/Styles/ProStyle.uss";
        public const string PersonalPath = "Assets/GameCore/Code/Editor/UIElements/Styles/PersonalStyle.uss";
#endif

        public OwlcatInspectorStyle()
        {
            LoadStyles();
        }

        private void LoadStyles()
        {
            var styles = AssetDatabase.LoadAssetAtPath<StyleSheet>(EditorGUIUtility.isProSkin ? ProPath : PersonalPath);
            styleSheets.Add(styles);

            styles = AssetDatabase.LoadAssetAtPath<StyleSheet>(CommonPath);
            styleSheets.Add(styles);
        }
    }
}
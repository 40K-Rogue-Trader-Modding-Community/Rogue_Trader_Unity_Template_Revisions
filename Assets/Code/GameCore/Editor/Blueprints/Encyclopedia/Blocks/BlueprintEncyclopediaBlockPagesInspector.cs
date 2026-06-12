using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Kingmaker.Blueprints.Encyclopedia.Blocks
{
    [CustomEditor(typeof(BlueprintEncyclopediaBlockPages), true)]
    public class BlueprintEncyclopediaBlockPagesInspector : BlueprintEncyclopediaBlockInspector
    {
        protected SerializedProperty Pages;
        protected SerializedProperty Source;
        protected ReorderableList m_PagesList;

        public override void OnEnable()
        {
            base.OnEnable();
            Pages = serializedObject.FindProperty("Pages");
            Source = serializedObject.FindProperty("Source");
            m_PagesList = new ReorderableList(serializedObject, Pages);
            m_PagesList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                rect.y += 2;
                var e = Pages.GetArrayElementAtIndex(index);
                if (e != null) EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), e);
            };
            m_PagesList.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, Pages.displayName); };
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var sourceField = new PropertyField(serializedObject.FindProperty("Source"));
            root.Add(sourceField);

            var pagesProp = serializedObject.FindProperty("Pages");
            var pagesField = new PropertyField(pagesProp);
            const int byList = (int) BlueprintEncyclopediaBlockPages.SourcePages.ByList;
            pagesField.style.display = Source.intValue == byList ? DisplayStyle.Flex : DisplayStyle.None;
            
            root.TrackPropertyValue(serializedObject.FindProperty("Source"), prop =>
                pagesField.style.display = prop.intValue == byList ? DisplayStyle.Flex : DisplayStyle.None);
            root.Add(pagesField);
            
            return root;
        }


        public override float GetHeight(params string[] ignoreList)
        {
            return base.GetHeight("Pages") + 2 + (Source.intValue == (int)BlueprintEncyclopediaBlockPages.SourcePages.ByList ? m_PagesList.GetHeight() : 0);
        }

        public override void OnDraw(Rect rect, params string[] ignoreList)
        {            
            Rect eRect = new Rect(rect.x, rect.y, rect.width, base.GetHeight("Pages"));

            base.OnDraw(eRect, "Pages");

            if (Source.intValue == (int)BlueprintEncyclopediaBlockPages.SourcePages.ByList)
            {
                eRect.y += eRect.height;
                eRect.height = rect.height - eRect.height;
                m_PagesList.DoList(eRect);
            }
        }
    }
}

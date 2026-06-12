using System;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Kingmaker.Blueprints.Encyclopedia.Blocks;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.Blueprints;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Object = UnityEngine.Object;

namespace Kingmaker.Blueprints.Encyclopedia
{
    
    [BlueprintCustomEditor(typeof(BlueprintEncyclopediaPage), true)]
    public class BlueprintEncyclopediaPageInspector : BlueprintEncyclopediaNodeInspector
    {
        protected SerializedProperty m_Blocks;
        protected ReorderableList m_BlockList;
        protected Dictionary<Object, BlueprintEncyclopediaBlockInspector> m_BlockEditors;

        public override void OnEnable(BlueprintWrapperInspector ed)
        {
            base.OnEnable(ed);

            m_Blocks = SerializedObject.FindProperty("Blueprint.Blocks");
            m_BlockEditors = new Dictionary<Object, BlueprintEncyclopediaBlockInspector>();

            m_BlockList = new ReorderableList(SerializedObject, m_Blocks);
            m_BlockList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, m_Blocks.displayName);
            };
            m_BlockList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var property = m_Blocks.GetArrayElementAtIndex(index);
                var value = property.objectReferenceValue;
                if (value == null)
                {
                    EditorGUI.PropertyField(new Rect(rect.x, rect.y + 2, rect.width, EditorGUIUtility.singleLineHeight), property);
                    return;
                }

                BlueprintEncyclopediaBlockInspector editor;
                if (m_BlockEditors.TryGetValue(value, out editor))
                {
                    editor.OnDraw(rect);
                }
            };
            m_BlockList.elementHeightCallback = (int index) =>
            {
                var property = m_Blocks.GetArrayElementAtIndex(index);
                var value = property.objectReferenceValue;
                if (value == null)
                {
                    return EditorGUIUtility.singleLineHeight + 2;
                }
                BlueprintEncyclopediaBlockInspector editor;
                if (m_BlockEditors.TryGetValue(value, out editor))
                {
                    if (editor == null)
                    {
                        editor = UnityEditor.Editor.CreateEditor(value, value.GetType()) as BlueprintEncyclopediaBlockInspector;
                    }
                    float h = editor.GetHeight();
                    return h;
                }
                return EditorGUIUtility.singleLineHeight;
            };
            m_BlockList.onAddDropdownCallback = (Rect buttonRect, ReorderableList list) =>
            {
                GenericMenu menu = new GenericMenu();

                menu.AddItem(new GUIContent("Empty"), false, () => { AddBlock(null); });

                System.Type[] blockTypes = GetBlockTypes();
                foreach (var item in blockTypes)
                {
                    menu.AddItem(new GUIContent(item.Name), false, () => { AddBlock(item); });
                }
                menu.DropDown(buttonRect);
            };
        }
        
        public override void OnDisable()
        {
            foreach (var editor in m_BlockEditors)
            {
                Object.DestroyImmediate(editor.Value, true);
            }
            m_BlockEditors.Clear();
            m_BlockEditors = null;
        }

        protected System.Type[] m_BlockTypes = null;
        private SimpleBlueprint m_Blueprint;

        protected System.Type[] GetBlockTypes()
        {
            if (m_BlockTypes != null) return m_BlockTypes;
            System.Type parent = typeof(BlueprintEncyclopediaBlock);
            System.Type[] types = parent.Assembly.GetTypes();
            m_BlockTypes = types.Where(t => parent.IsAssignableFrom(t) && t != parent).ToArray();
            return m_BlockTypes;
        }

        protected void AddBlock(System.Type type)
        {
            try
            {
                if (type != null)
                {
                    ScriptableObject block = ScriptableObject.CreateInstance(type);
                    string path = BlueprintsDatabase.GetAssetPath(m_Blueprint).Replace(".jbp", ".asset");
                    path = Path.Combine("Assets/Mechanics/", path);
                    string fileExt = ".asset";
                    path = path.Insert(path.Length - fileExt.Length,
                        "_" + type.Name.Replace("BlueprintEncyclopedia", ""));
                    path = path.Replace("/", "\\");
                    string dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);

                    path = AssetDatabase.GenerateUniqueAssetPath(path);
                    AssetDatabase.CreateAsset(block, path);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.ImportAsset(path);

                    int index = m_Blocks.arraySize;
                    m_Blocks.InsertArrayElementAtIndex(index);
                    SerializedProperty element = m_Blocks.GetArrayElementAtIndex(index);
                    element.objectReferenceValue = block;
                }
                else
                {
                    m_Blocks.InsertArrayElementAtIndex(m_Blocks.arraySize);
                }

                SerializedObject.ApplyModifiedProperties();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        public override VisualElement OnBeforeComponentsElement(SimpleBlueprint bp)
        {
            m_Blueprint = bp;
            var root = new VisualElement();
            var baseEl = base.OnBeforeComponentsElement(bp);
            if (baseEl != null) 
                root.Add(baseEl);
            root.Add(CreateBlocksListView());
            
            return root;
        }

        private ListView CreateBlocksListView()
        {
            var listView = new ListView
            {
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                showAddRemoveFooter = true,
                showBorder = true,
                showFoldoutHeader = true,
                headerTitle = "Blocks",
                makeItem = () => new VisualElement(),
                bindItem = BindBlockItem
            };

            var blockIndices = new List<object>(m_Blocks.arraySize);
            SyncBlockIndices(blockIndices, m_Blocks.arraySize);
            
            listView.itemsSource = blockIndices;
            listView.itemIndexChanged += (oldIdx, newIdx) =>
            {
                m_Blocks.MoveArrayElement(oldIdx, newIdx);
                SerializedObject.ApplyModifiedProperties();
                listView.RefreshItems();
            };

            listView.onAdd = _ => ShowAddBlockMenu();
            listView.onRemove = lv =>
            {
                if (lv.selectedIndex < 0)
                    return;
                
                m_Blocks.DeleteArrayElementAtIndex(lv.selectedIndex);
                SerializedObject.ApplyModifiedProperties();
                SyncBlockIndices(blockIndices, m_Blocks.arraySize);
                listView.Rebuild();
            };
            
            listView.schedule.Execute(() =>
            {
                SerializedObject.Update();
                
                int newSize = m_Blocks.arraySize;
                if (blockIndices.Count == newSize)
                {
                    listView.RefreshItems();
                    return;
                }
                
                SyncBlockIndices(blockIndices, newSize);
                listView.Rebuild();
            }).Every(100);

            return listView;
        }

        private static void SyncBlockIndices(List<object> indices, int targetSize)
        {
            while (indices.Count < targetSize) 
                indices.Add(null);
            while (indices.Count > targetSize) 
                indices.RemoveAt(indices.Count - 1);
        }

        private void BindBlockItem(VisualElement el, int idx)
        {
            SerializedObject.Update();
            if (idx >= m_Blocks.arraySize)
                return;

            var blockObj = m_Blocks.GetArrayElementAtIndex(idx).objectReferenceValue;

            if (blockObj == null)
            {
                if (el.userData == null && el.childCount > 0)
                    return;
                
                ClearBlockItem(el);
                el.Add(new PropertyField(m_Blocks.GetArrayElementAtIndex(idx)));
                return;
            }

            if (el.userData is SerializedObject existingSo && existingSo.targetObject == blockObj)
                return;
            
            ClearBlockItem(el);
            var so = new SerializedObject(blockObj);
            el.userData = so;
            el.Add(new Label(blockObj.name));
            el.Add(new InspectorElement(so));
        }

        private static void ClearBlockItem(VisualElement el)
        {
            el.Unbind();
            if (el.userData is SerializedObject so)
                so.Dispose();
            el.Clear();
            el.userData = null;
        }

        private void ShowAddBlockMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Empty"), false, () => AddBlock(null));
            foreach (var t in GetBlockTypes())
            {
                var captured = t;
                menu.AddItem(new GUIContent(captured.Name), false, () => AddBlock(captured));
            }
            menu.ShowAsContext();
        }

        public override void OnBeforeComponents(SimpleBlueprint bp)
        {
            m_Blueprint = bp;
            
            SerializedObject.Update();
            for (int i = 0; i < m_Blocks.arraySize; i++)
            {
                var property = m_Blocks.GetArrayElementAtIndex(i);
                var value = property.objectReferenceValue;
                if (value == null)
                {
                    continue;
                }
                if (!m_BlockEditors.ContainsKey(value)) m_BlockEditors.Add(value, UnityEditor.Editor.CreateEditor(value) as BlueprintEncyclopediaBlockInspector);
            }
            m_PageList.DoLayoutList();
            m_BlockList.DoLayoutList();
            SerializedObject.ApplyModifiedProperties();
        }
    }
}

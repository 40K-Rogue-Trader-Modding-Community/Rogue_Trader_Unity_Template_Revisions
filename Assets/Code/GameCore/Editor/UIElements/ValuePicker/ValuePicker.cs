using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Code.Blueprints.Attributes;
using Code.Editor.KnowledgeDatabase;
using JetBrains.Annotations;
using Kingmaker.Editor.Blueprints;
using Kingmaker.ElementsSystem;
using Owlcat.Editor.Utility;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Kingmaker.Utility.UnityExtensions;
using Owlcat.Editor.CustomGUI;

namespace Kingmaker.Editor.UIElements.ValuePicker
{
	public abstract class ValuePicker<T> : OwlcatEditorWindowBase
	{
		private static Vector2 s_Size = new Vector2(200, 300);
		private static T m_PrevPicked;

		private static (string description, Rect controlRect)? OvertipData;

		[NotNull]
		private Action<T> m_Callback;

		private ValuesContainer<T> m_ValuesContainer;

		private List<T> m_DisplayValues;
		private List<(ValueNameHierarchyNode node, bool isLeaf)> m_AllHierarchy;
		
		private T m_Selected;

		[NotNull]
		private string m_SearchString = "";

		[CanBeNull]
		private EditorWindow m_Parent;
		private List<string> m_HiddenDisplayNames;
		
		private bool m_FirstFrame = true;
		private bool m_MustScrollToSelected = false;

		private Vector2 m_ScrollPosition;

		private bool IsShowValuesHierarchy
		{
			get => EditorPrefs.GetBool("ValuePicker.IsShowValuesHierarchyKey", false);
			set => EditorPrefs.SetBool("ValuePicker.IsShowValuesHierarchyKey", value);
		}

		private bool IsContextEvaluatorFilterOn
		{
			get => EditorPrefs.GetBool("ValuePicker.ContextEvaluatorFilterOn", false);
			set => EditorPrefs.SetBool("ValuePicker.ContextEvaluatorFilterOn", value);
		}

		public void Init(string buttonText, EditorWindow parent, Action<T> callback, ValuesContainer<T> container)
		{
			titleContent = new GUIContent(buttonText);
			minSize = s_Size;

			m_HiddenDisplayNames = new List<string>();

			m_Parent = parent;
			m_Callback = callback;
			m_ValuesContainer = container;
            FillHierarchy(true);
            UpdateFilteredValues();
		}

		private List<T> GetAllValues()
		{
			return m_ValuesContainer.FilterEnabled && IsContextEvaluatorFilterOn
				? m_ValuesContainer.FilteredValues
				: m_ValuesContainer.RawValues;
		}

		private static class Styles
		{
			[NotNull]
			public static GUIStyle m_CommonEvenStyle;
			[NotNull]
			public static GUIStyle m_CommonOddStyle;
			static Styles()
			{
				m_CommonEvenStyle = new GUIStyle("CN EntryBackEven")
				{
					padding = new RectOffset(2, 0, 1, 1),
					margin = GUI.skin.label.margin,
					fixedHeight = GUI.skin.label.fixedHeight,
					alignment = TextAnchor.MiddleLeft
				};
				m_CommonOddStyle = new GUIStyle("CN EntryBackOdd")
				{
					padding = new RectOffset(2, 0, 1, 1),
					margin = GUI.skin.label.margin,
					fixedHeight = GUI.skin.label.fixedHeight,
					alignment = TextAnchor.MiddleLeft
				};
			}
		}

		protected static void Button(Func<ValuePicker<T>> windowCreator, 
			string buttonText, 
			Func<IEnumerable<T>> valuesCollector, 
			Action<T> callback, 
			bool showNow = false, 
			GUIStyle style = null, 
			params GUILayoutOption[] options)
		{
			style = style ?? GUI.skin.button;
			var rect = GUILayoutUtility.GetRect(new GUIContent(buttonText), style, options);
			Button(windowCreator, rect, buttonText, valuesCollector, callback, showNow, style);
		}

		protected static void Button(Func<ValuePicker<T>> windowCreator, 
			string buttonText, 
			Func<ValuesContainer<T>> valuesContainerCollector, 
			Action<T> callback, 
			bool showNow = false, 
			GUIStyle style = null, 
			params GUILayoutOption[] options)
		{
			style = style ?? GUI.skin.button;
			var rect = GUILayoutUtility.GetRect(new GUIContent(buttonText), style, options);
			Button(windowCreator, rect, buttonText, valuesContainerCollector, callback, showNow, style);
		}

		protected static void Button(Func<ValuePicker<T>> windowCreator, 
			Rect rect, 
			string buttonText, 
			Func<IEnumerable<T>> valuesCollector,
			Action<T> callback, 
			bool showNow = false, 
			GUIStyle style = null)
		{
			var actualStyle = style ?? GUI.skin.button;
			if (showNow || GUI.Button(rect, buttonText, actualStyle))
			{
				List<T> values = valuesCollector().ToList();
				ValuesContainer<T> valuesContainer = new(values);
				OnButtonClicked(windowCreator, rect, buttonText, valuesContainer, callback);
			}
		}

		private static void Button(Func<ValuePicker<T>> windowCreator, 
			Rect rect, 
			string buttonText,
			Func<ValuesContainer<T>> valuesContainerCollector,
			Action<T> callback, 
			bool showNow = false, 
			GUIStyle style = null)
		{
			var actualStyle = style ?? GUI.skin.button;
			if (showNow || GUI.Button(rect, buttonText, actualStyle))
			{
				ValuesContainer<T> valuesContainer = valuesContainerCollector();
				OnButtonClicked(windowCreator, rect, buttonText, valuesContainer, callback);
			}
		}

		private static void OnButtonClicked(Func<ValuePicker<T>> windowCreator, 
			Rect rect, 
			string buttonText, 
			ValuesContainer<T> valuesContainer, 
			Action<T> callback)
		{
			var window = windowCreator();
			window.Init(buttonText, focusedWindow, callback, valuesContainer);
			var size = window.position.size;
			var screenRect = rect;
			screenRect.position = GUIUtility.GUIToScreenPoint(rect.position);
			window.ShowAsDropDown(screenRect, s_Size);
			window.maxSize = new Vector2(4000, 4000);
			var newPos = window.position;
			newPos.size = size;
			window.position = newPos;
		}

		protected static VisualElement CreateButton(Func<ValuePicker<T>> windowCreator, 
			string buttonText, 
			Func<IEnumerable<T>> valuesCollector, 
			Action<T> callback)
		{
			var button = new Button 
			{ 
				text = buttonText, 
				style = 
				{ 
					marginTop = new StyleLength(4), 
					marginBottom = new StyleLength(4) 
				}
			};
			button.clicked += () => ShowPickerMenu(button, windowCreator, buttonText, valuesCollector, callback);
			return button;
		}
		
		protected static VisualElement CreateButton(Func<ValuePicker<T>> windowCreator, 
			string buttonText, 
			Func<ValuesContainer<T>> valuesContainerCollector, 
			Action<T> callback)
		{
			var button = new Button 
			{ 
				text = buttonText, 
				style = 
				{ 
					marginTop = new StyleLength(4), 
					marginBottom = new StyleLength(4) 
				} 
			};
			button.clicked += () => ShowPickerMenu(button, windowCreator, buttonText, valuesContainerCollector, callback);
			return button;
		}

		private static void ShowPickerMenu(VisualElement source,
			Func<ValuePicker<T>> windowCreator, 
			string windowTitle, 
			ValuesContainer<T> valuesContainer, 
			Action<T> callback)
		{
			var parent = focusedWindow;
			var window = windowCreator();
			window.Init(windowTitle, parent, callback, valuesContainer);

			var size = window.position.size;

			var rect = source.layout;
			var screenRect = rect;
			screenRect.position = GUIUtility.GUIToScreenPoint(source.LocalToWorld(source.layout).position);
			window.ShowAsDropDown(screenRect, s_Size);
			window.maxSize = new Vector2(4000, 4000);
			var newPos = window.position;
			newPos.size = size;
			window.position = newPos;
		}
		
		public static void ShowPickerMenu(VisualElement source, 
			Func<ValuePicker<T>> windowCreator, 
			string windowTitle, 
			Func<ValuesContainer<T>> valuesContainerCollector, 
			Action<T> callback)
		{
			ValuesContainer<T> valuesContainer = valuesContainerCollector();
			ShowPickerMenu(source, windowCreator, windowTitle, valuesContainer, callback);
		}
		
		public static void ShowPickerMenu(VisualElement source, 
			Func<ValuePicker<T>> windowCreator, 
			string windowTitle, 
			Func<IEnumerable<T>> valuesCollector, 
			Action<T> callback)
		{
			var values = valuesCollector();
			ValuesContainer<T> valuesContainer = new(values.ToList());
			ShowPickerMenu(source, windowCreator, windowTitle, valuesContainer, callback);
		}

		public virtual string GetItemFullname(T value)
		{
			if (value is Type type)
				return ClassNames.GetClassName(type);
			return GetValueName(value);
		}

		public virtual string GetItemShortname(T value)
		{
			if (value is Type type)
				return ClassNames.GetClassNameNoPrefix(type);
			return GetValueName(value);
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			wantsMouseMove = true;
		}

		protected override void OnGUI()
		{
			base.OnGUI();

			HandleHotkeys();
			DrawFilter();
			DrawContent();

			if (OvertipData != null)
			{
				ShowDescriptionOvertip(OvertipData.Value.description, OvertipData.Value.controlRect);
				OvertipData = null;
			}

			if (Event.current.type == EventType.MouseMove)
			{
				Event.current.Use();
			}
		}

		public void OnLostFocus()
		{
			EditorApplication.delayCall += Close;
		}

		private void DrawFilter()
		{
			string prevSearch = m_SearchString;

			GUI.SetNextControlName("filter");
			using (new GUILayout.HorizontalScope())
			{
				m_SearchString = EditorGUILayout.TextField(m_SearchString);

				if (m_ValuesContainer.FilterEnabled)
				{
					bool newValue = GUILayout.Toggle(IsContextEvaluatorFilterOn, "Filter", "Button", 
						GUILayout.Width(80));
					
					if (newValue != IsContextEvaluatorFilterOn)
					{
						IsContextEvaluatorFilterOn = !IsContextEvaluatorFilterOn;
						UpdateFilteredValues();
					}
				}

				if (GUILayout.Button("Switch view", GUILayout.Width(120)))
				{
					IsShowValuesHierarchy = !IsShowValuesHierarchy;
				}
			}
			if (m_FirstFrame)
			{
				m_FirstFrame = false;
				GUI.FocusControl("filter");
			}

			if (prevSearch != m_SearchString)
				UpdateFilteredValues();
		}

		protected virtual string GetValueName(T value)
		{
			return value.ToString();
		}

		private void DrawContent()
		{
			float selectedY = 0f;
			using (var scroll = new GUILayout.ScrollViewScope(m_ScrollPosition))
			{
				int i = 0;

				if (IsShowValuesHierarchy)
				{
					DrawItemsAsGroups(i, ref selectedY);
				}
				else
				{
					DrawItemsAsList(i, ref selectedY);
				}

				m_ScrollPosition = scroll.scrollPosition;
			}

			if (Event.current.type == EventType.Repaint)
			{
				if (m_MustScrollToSelected)
				{
					m_ScrollPosition.y = selectedY - 150;
					m_MustScrollToSelected = false;
					Repaint();
				}
			}
		}

		private void DrawItemsAsGroups(int i, ref float selectedY)
		{
			FillHierarchy(false);

            bool isHideCategoryOfLevel = false;
			int categoryLevelToHide = 0;

			foreach (var viewNode in m_AllHierarchy)
			{
				if (viewNode.node.Name != "root")
				{
					T v = viewNode.node.Value;

					string indent = "";
					for (int j = 0; j < viewNode.node.Level; j++)
					{
						indent += "     ";
					}

					var commonStyle = i % 2 == 0 ? Styles.m_CommonEvenStyle : Styles.m_CommonOddStyle;
					string valueName = indent + viewNode.node.Name;

					if (isHideCategoryOfLevel)
					{
						if (viewNode.node.Level > categoryLevelToHide)
						{
							continue;
						}
						else
						{
							isHideCategoryOfLevel = false;
						}
					}

					if (viewNode.isLeaf)
					{
						DrawItem(v, valueName, commonStyle, ref selectedY, v.Equals(m_Selected));
					}
					else
					{
						var btnRect = GUILayoutUtility.GetRect(new GUIContent(valueName), commonStyle);
						var labelRect = btnRect;
						labelRect.center += new Vector2(20.0f * (viewNode.node.Level - 1), 0.0f);

						bool isHidden = m_HiddenDisplayNames.Contains(viewNode.node.Name);

						if (isHidden)
						{
							isHideCategoryOfLevel = true;
							categoryLevelToHide = viewNode.node.Level;
						}
						else
						{
							isHideCategoryOfLevel = false;
						}

						if (GUI.Button(btnRect, valueName, commonStyle))
						{
							if (isHidden)
							{
								m_HiddenDisplayNames.Remove(viewNode.node.Name);
							}
							else
							{
								m_HiddenDisplayNames.Add(viewNode.node.Name);
							}
						}

						GUI.Label(labelRect, isHidden ? "▶" : "▼");
					}

					i++;
				}
			}
		}

		private void DrawItemsAsList(int i, ref float selectedY)
		{
			foreach (var v in m_DisplayValues)
			{
				var commonStyle = i % 2 == 0 ? Styles.m_CommonEvenStyle : Styles.m_CommonOddStyle;

				string valueName = GetItemShortname(v);

				DrawItem(v, valueName, commonStyle, ref selectedY, v.Equals(m_Selected));

				i++;
			}
		}

		private void DrawItem(T value, string valueName, GUIStyle style, ref float selectedY, bool selected)
		{
			var btnRect = GUILayoutUtility.GetRect(new GUIContent(valueName), style);
			if (Event.current.type == EventType.Repaint)
				style.Draw(btnRect, valueName, false, false, selected, false);
			if (GUI.Button(btnRect, GUIContent.none, GUIStyle.none))
			{
				if (value.Equals(m_Selected))
				{
					Select(m_Selected);
				}

				m_Selected = value;
			}

			DrawOvertipIcon(value, btnRect);
			DrawEvaluatorFilterTag(value, btnRect);

			if (value.Equals(m_Selected))
			{
				selectedY = btnRect.yMin;
			}

			if (m_PrevPicked != null && value.Equals(m_PrevPicked))
			{
				GUILayout.Space(2);
				Handles.DrawLine(new Vector3(btnRect.xMin, btnRect.yMax + 2),
					new Vector3(btnRect.xMax, btnRect.yMax + 2));
			}
		}

		private void DrawOvertipIcon(T value, Rect btnRect)
		{
			if (value is Type type)
			{
				string description = KnowledgeDatabaseSearch.GetDescription(type);
				string codeDescription = KnowledgeDatabaseSearch.GetCodeDescription(type);

				if (!description.IsNullOrEmpty() || !codeDescription.IsNullOrEmpty() && 
				    Event.current.type == EventType.Repaint)
				{
					float tooltipMarkSize = 20.0f;

					Rect overtipRect =
						new Rect(
							btnRect.xMax - tooltipMarkSize / 2.0f - tooltipMarkSize / 2.0f,
							btnRect.center.y - tooltipMarkSize / 2.0f,
							tooltipMarkSize,
							tooltipMarkSize);

					GUI.Label(overtipRect, "?");

					var controlRect = overtipRect;
					bool isMouseOverProperty = controlRect.Contains(Event.current.mousePosition);
					if (isMouseOverProperty)
					{
						var fullDescription = "";
						if (!codeDescription.IsNullOrEmpty())
							fullDescription += "Programmer's description:\n\n" + codeDescription;
						if (!codeDescription.IsNullOrEmpty() && !description.IsNullOrEmpty())
							fullDescription += "\n\n";
						if (!description.IsNullOrEmpty())
							fullDescription += "Designer's description:\n\n" + description;

						OvertipData = new(fullDescription, controlRect);
					}
				}
			}
		}

		private void DrawEvaluatorFilterTag(T value, Rect btnRect)
		{
			if (value is Type type && type.IsOrSubclassOf<Evaluator>())
			{
				float tooltipMarkSize = 150.0f;
				Rect overtipRect = new(
					btnRect.xMax - tooltipMarkSize,
					btnRect.center.y - tooltipMarkSize / 2.0f,
					tooltipMarkSize,
					tooltipMarkSize);

				EvaluatorFilter filter = type.GetCustomAttribute<EvaluatorFilter>();
				string tag = filter == null ? "" : filter.FilterName;
				GUI.Label(overtipRect, tag);
			}
		}
		
		private void ShowDescriptionOvertip(string description, Rect controlRect)
		{
			if (!description.IsNullOrEmpty())
			{
				var tooltipStyle = new GUIStyle(OwlcatEditorStyles.Instance.CommandBox)
				{
					wordWrap = true,
					normal = { background = MakeTex(1, 1, Color.white), textColor = Color.black },
					richText = true,
					alignment = TextAnchor.UpperLeft,
					clipping = TextClipping.Overflow,
				};

				string fullDescription = "";
				if (!description.IsNullOrEmpty())
					fullDescription = description;

				var tooltipContent = new GUIContent(fullDescription);
				float width = controlRect.y < 10f
					? EditorGUIUtility.labelWidth * 3f
					: EditorGUIUtility.labelWidth * 2f;
				width = Mathf.Clamp(width, 0.0f, focusedWindow.position.width * 0.75f);

				float height = tooltipStyle.CalcHeight(tooltipContent, width);

				Vector2 position = controlRect.center - new Vector2(width + controlRect.width, m_ScrollPosition.y);
				var tooltipRect = new Rect(position.x, position.y, width, height);

				GUI.Box(tooltipRect, tooltipContent, tooltipStyle);
			}
		}
        
        private static Texture2D MakeTex(int width, int height, Color col)
        {
                var pix = new Color[width * height];
                for (int i = 0; i < pix.Length; i++)
                        pix[i] = col;
        
                var result = new Texture2D(width, height);
                result.SetPixels(pix);
                result.Apply();
                
                return result;
        }

		private void HandleHotkeys()
		{
			var e = Event.current;
			if (e.type != EventType.KeyDown)
				return;

			switch (e.keyCode)
			{
				case KeyCode.DownArrow:
					if (IsShowValuesHierarchy)
					{
						int i1 = GetIndexInHierarchy(m_Selected);
						m_Selected = GetNextDisplayValueForHierarchy(i1);
					}
					else
					{
						int i1 = m_DisplayValues.IndexOf(m_Selected);
						if (i1 < m_DisplayValues.Count - 1)
						{
							m_Selected = m_DisplayValues[i1 + 1];
						}
					}

					m_MustScrollToSelected = true;
					e.Use();
					break;
				case KeyCode.UpArrow:
					if (IsShowValuesHierarchy)
					{
						int i2 = GetIndexInHierarchy(m_Selected);
						m_Selected = GetPrevDisplayValueForHierarchy(i2);
					}
					else
					{
						int i2 = m_DisplayValues.IndexOf(m_Selected);
						if (i2 > 0)
						{
							m_Selected = m_DisplayValues[i2 - 1];
						}
					}

					m_MustScrollToSelected = true;
					e.Use();
					break;
				case KeyCode.KeypadEnter:
				case KeyCode.Return:
					Select(m_Selected);
					e.Use();
					break;
				case KeyCode.Escape:
					Cancel();
					e.Use();
					break;
			}
		}

		private void Select(T result)
		{
			Close();
			if (result != null)
			{
				m_PrevPicked = result;
				m_Callback(result);
			}
			if (m_Parent != null)
			{
				m_Parent.Repaint();
				m_Parent.Focus();
			}
		}

		private void Cancel()
		{
			Close();
			if (m_Parent != null)
			{
				m_Parent.Repaint();
				m_Parent.Focus();
			}
		}

		private void UpdateDisplayValuesBySearchString()
		{
			if (string.IsNullOrEmpty(m_SearchString))
			{
				m_DisplayValues = GetAllValues();
			}
			else
			{
				string[] filters = m_SearchString.ToLowerInvariant().Split();
				m_DisplayValues = GetAllValues().Where(t => filters.All(f => GetValueName(t).ToLowerInvariant().Contains(f))).ToList();
			}
		}

		private void PrioritizePrevPicked()
		{
			if (m_PrevPicked != null && !IsShowValuesHierarchy)
			{
				int lastPickedIndex = m_DisplayValues.IndexOf(m_PrevPicked);
				if (lastPickedIndex > 0)
				{
					m_DisplayValues.RemoveAt(lastPickedIndex);
					m_DisplayValues.Insert(0, m_PrevPicked);
				}
			}
		}

		private void UpdateSelected()
		{
			if (!m_DisplayValues.Contains(m_Selected))
			{
				if (IsShowValuesHierarchy)
				{
					if (m_PrevPicked != null)
					{
						m_Selected = m_PrevPicked;
					}
					else
					{
						m_Selected = GetFirstInHierarchyOrDefault();
					}
				}
				else
				{
					m_Selected = m_DisplayValues.FirstOrDefault();
				}
			}

			m_MustScrollToSelected = true;
		}

		private void UpdateFilteredValues()
		{
			UpdateDisplayValuesBySearchString();
			PrioritizePrevPicked();
			UpdateSelected();
		}

		private void FillHierarchy(bool isAllValues)
		{
			ValueNameHierarchyNode rootNode = new ValueNameHierarchyNode(null, "root", default, 0);

			foreach (var v in isAllValues ? GetAllValues() : m_DisplayValues)
			{
				string fullName = GetItemFullname(v);
				var hierarchy = fullName.Split("/");

				if (hierarchy.Length == 0 || hierarchy.Length == 1)
				{
					ValueNameHierarchyNode otherRoot = rootNode.GetOrCreateChild("Other", default);
					otherRoot.AddChild(fullName, v);
				}
				else
				{
					ValueNameHierarchyNode prevNode = rootNode;
					for (int j = 0; j < hierarchy.Length; j++)
					{
						ValueNameHierarchyNode node = prevNode.GetOrCreateChild(
							hierarchy[j],
							(j + 1 == hierarchy.Length) ? v : default);
						prevNode = node;
					}
				}
			}

			m_AllHierarchy = ValueNameHierarchyNode.GetAllHierarchyForRoot(rootNode, true);
		}
		
		private T GetFirstInHierarchyOrDefault()
		{
            if (m_AllHierarchy.Count < 2)
			{
				return default;
            }

			ValueNameHierarchyNode node = null;

			if (m_AllHierarchy[0].node.Children.Count != 1) 
			{
				for (int i = 0; i < m_AllHierarchy[0].node.Children.Count; i++)
				{
					if (m_AllHierarchy[0].node.Children[i].Name != "Other")
                    {
                        node = m_AllHierarchy[0].node.Children[i];
                        break;
					}
				}
			}
			else
			{
				node = m_AllHierarchy[0].node.Children[0];
            }

            while (!node.IsLeaf())
            {
                node = node.Children[0];
            }

            return node.Value;
        }

        private T GetNextDisplayValueForHierarchy(int currentSelectedID)
        {
            int leafSearchIterator = 1;

            while ((currentSelectedID + leafSearchIterator) < m_AllHierarchy.Count)
            {
                if (m_AllHierarchy[currentSelectedID + leafSearchIterator].isLeaf &&
                    !IsHierarchyItemHidden(currentSelectedID + leafSearchIterator))
                    return m_AllHierarchy[currentSelectedID + leafSearchIterator].node.Value;

                leafSearchIterator++;
            }

            return m_AllHierarchy[currentSelectedID].node.Value;
        }

        private T GetPrevDisplayValueForHierarchy(int currentSelectedID)
        {
            int leafSearchIterator = 1;

            while ((currentSelectedID - leafSearchIterator) >= 0)
            {
                if (m_AllHierarchy[currentSelectedID - leafSearchIterator].isLeaf &&
                    !IsHierarchyItemHidden(currentSelectedID - leafSearchIterator))
                    return m_AllHierarchy[currentSelectedID - leafSearchIterator].node.Value;

                leafSearchIterator++;
            }

            return m_AllHierarchy[currentSelectedID].node.Value;
        }

		private bool IsHierarchyItemHidden(int id)
		{
			ValueNameHierarchyNode parent = m_AllHierarchy[id].node.Parent;

            while (parent != null)
			{
				if (m_HiddenDisplayNames.Contains(parent.Name))
				{
					return true;
				}

				parent = parent.Parent;
            }

			return false;
		}

        private int GetIndexInHierarchy(T value)
        {
			for (int i = 0; i < m_AllHierarchy.Count; i++)
			{
				if (m_AllHierarchy[i].node != null &&
                    m_AllHierarchy[i].node.Value != null &&
                    m_AllHierarchy[i].node.Value.Equals(value))
				{
					return i;
                }
			}

			return 0;
        }

        class ValueNameHierarchyNode
		{
			public string Name;
			public T Value;
			public int Level;
			public List<ValueNameHierarchyNode> Children;
			public ValueNameHierarchyNode Parent;

            public ValueNameHierarchyNode(ValueNameHierarchyNode parent, string name, T value, int level)
			{
				Name = name;
				Value = value;
				Level = level;
				Children = new List<ValueNameHierarchyNode>();
				Parent = parent;
            }

			public ValueNameHierarchyNode AddChild(string name, T value)
			{
				ValueNameHierarchyNode node = new ValueNameHierarchyNode(this, name, value, Level + 1);
				Children.Add(node);
				return node;
			}
			
			public ValueNameHierarchyNode GetOrCreateChild(string name, T value)
			{
				foreach (var child in Children)
				{
					if (child.Name == name)
					{
						return child;
					}
				}

				ValueNameHierarchyNode newChild = AddChild(name, value);
				return newChild;
			}

			public bool IsLeaf()
			{
				return Children.Count == 0;
			}

			public static List<(ValueNameHierarchyNode, bool)> GetAllHierarchyForRoot(ValueNameHierarchyNode root, bool otherInTheEnd)
			{
				List<(ValueNameHierarchyNode, bool)> resultList = new List<(ValueNameHierarchyNode, bool)>();
				WalkByTree(root, resultList, otherInTheEnd);

				if (otherInTheEnd)
				{
					foreach (var child in root.Children)
					{
						if (child.Name == "Other")
						{
							WalkByTree(child, resultList, false);
							break;
						}
					}
				}
				
				return resultList;
			}

			private static void WalkByTree(ValueNameHierarchyNode root, List<(ValueNameHierarchyNode, bool)> result, bool ignoreOther)
			{
				result.Add(new (root, root.IsLeaf()));

				foreach (var rootChild in root.Children)
				{
					if (ignoreOther && rootChild.Name == "Other")
					{
						continue;
					}

					WalkByTree(rootChild, result, ignoreOther);
				}
			}
		}
	}
}
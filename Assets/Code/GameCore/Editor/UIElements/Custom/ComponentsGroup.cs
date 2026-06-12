using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Attributes;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.Elements;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.ValuePicker;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Owlcat.Runtime.Core.Utility;
using UnityEditor.UIElements;


namespace Kingmaker.Editor.UIElements.Custom
{
	public class ComponentsGroup : OwlcatVisualElement
	{
		private readonly SerializedObject m_SerializedObject;

		private readonly SerializedProperty m_Property;
		
		private readonly VisualElement m_ComponentsContainer;
		private readonly VisualElement m_RestoreButtonsContainer;
		private readonly VisualElement m_Buttons;

        public BlueprintScriptableObject Blueprint
	        => BlueprintEditorWrapper.Unwrap<BlueprintScriptableObject>(m_SerializedObject?.targetObject);
        
        private static HashSet<ComponentsGroup> m_ActiveGroups = new();

		#region Constructor

		public ComponentsGroup(SerializedObject serializedObject)
		{
			m_SerializedObject = serializedObject;
			name = "Components";

            if (!Blueprint)
            {
                throw new Exception(
                    $"{nameof(ComponentsGroup)}(): blueprint is missing");
            }
            
            m_Property = m_SerializedObject.FindProperty("Blueprint.Components");
            this.TrackPropertyValue(m_Property, _ => UpdateComponents());

			m_ComponentsContainer = new VisualElement 
			{
				name = "ComponentsContainer", 
				style = {flexDirection = FlexDirection.Column}
			};
			Add(m_ComponentsContainer);

			m_RestoreButtonsContainer = new VisualElement 
			{
				name = "RestoreButtonsContainer", 
				style = {flexDirection = FlexDirection.Column}
			};
			Add(m_RestoreButtonsContainer);
            
			m_Buttons = new VisualElement()
			{
				name = "Buttons",
				style =
				{
					flexDirection = FlexDirection.Row,
					alignContent = Align.Center,
					justifyContent = Justify.Center
				}
			};
			Add(m_Buttons);
			
			CreateComponents();
            AddButtons();
            
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }
		
		private void OnAttachToPanel(AttachToPanelEvent evt)
		{
			m_ActiveGroups.Add(this);
		}
		
		private void OnDetachFromPanel(DetachFromPanelEvent evt)
		{
			m_ActiveGroups.Remove(this);
		}

		public void CreateComponents()
		{
			if (Blueprint == null)
				return;

			if (Blueprint.PrototypeLink != null)
				m_SerializedObject.UpdateIfRequiredOrScript();
            
			m_ComponentsContainer.Clear();

			for (int i = 0; i < m_Property.arraySize; i++)
			{
				var element = CreateComponentElement(i);
				m_ComponentsContainer.Add(element);
			}
            
			UpdateDuplicatesAndObsoleteMarks();
			AddRestoreButtons();
		}

		private void UpdateComponents()
		{
			for (int i = 0; i < m_Property.arraySize; i++)
			{
				if (i > m_ComponentsContainer.childCount - 1)
				{
					var newElement = CreateComponentElement(i);
					m_ComponentsContainer.Add(newElement);
				}
				
				if (m_ComponentsContainer[i] is ComponentElement { IsValid: false })
				{
					m_ComponentsContainer[i].RemoveFromHierarchy();
					var newElement = CreateComponentElement(i);
					m_ComponentsContainer.Insert(i, newElement);
				}
			}
			
			for (int i = m_ComponentsContainer.childCount - 1; i >= m_Property.arraySize; i--)
				m_ComponentsContainer[i].RemoveFromHierarchy();
            
			UpdateDuplicatesAndObsoleteMarks();
			AddRestoreButtons();
		}

		private ComponentElement CreateComponentElement(int index)
		{
			var element = new ComponentElement(m_Property.GetArrayElementAtIndex(index), index);
			element.OnMoveDownEvent += ItemMoveDown;
			element.OnMoveUpEvent += ItemMoveUp;
			element.OnRemoveEvent += ItemRemove;
			element.OnCopyEvent += ItemCopy;
            
			return element;
		}
		
		private void UpdateDuplicatesAndObsoleteMarks()
		{
			var duplicateIndices = GetDuplicateIndices(Blueprint).ToHashSet();

			for (int i = 0; i < m_ComponentsContainer.childCount; i++)
			{
				if (m_ComponentsContainer[i] is ComponentElement element)
					element.HasDuplicates = duplicateIndices.Contains(i);
			}
		}
		
		private void InvalidateForGroups(int startIndex, int endIndex)
		{
			foreach (var group in m_ActiveGroups)
				group.Invalidate(startIndex, endIndex, Blueprint);
		}

		private void Invalidate(int startIndex, int endIndex, BlueprintScriptableObject target)
		{
			if (startIndex < 0 || endIndex < 0) 
				return;
            
			if (Blueprint != target)
				return;
            
			for (int i = startIndex; i <= endIndex; i++)
			{
				if (i >= m_ComponentsContainer.childCount)
					break;
	            
				if (m_ComponentsContainer[i] is ComponentElement element)
					element.IsValid = false;
			}
		}

		private static IReadOnlyCollection<int> GetDuplicateIndices(BlueprintScriptableObject objectToValidate)
		{
			var indexed = objectToValidate.ComponentsArray.Select((v, i) => (v, i));
			var duplicates = from component in indexed
				let compIndex = component.i
				where component.v
				let type = component.v.GetType()
				where !type.HasAttribute<AllowMultipleComponentsAttribute>()
				group compIndex by type
				into byType
				where byType.Skip(1).Any()
				select byType;

			var duplicateIndices = duplicates
				.SelectMany(v => v)
				.Distinct()
				.ToHashSet();
			return duplicateIndices;
		}
		
		private void AddRestoreButtons()
		{
			m_RestoreButtonsContainer.Clear();
			if (Blueprint.PrototypeLink is BlueprintScriptableObject proto)
			{
				foreach (var component in proto.ComponentsArray)
				{
                    if (Blueprint.IsOverridden(component.name) && 
                        Blueprint.ComponentsArray.All(c => c.PrototypeLink != component))
                    {
                        AddRestoreButton(component);
                    } 
                }
			}
		}

		private void AddRestoreButton(BlueprintComponent component)
		{
			string compName = component.name;
			var root = new VisualElement();
			root.AddToClassList("owlcat-box");
			root.AddToClassList("labelPart");
			
			var label = new Label($"Removed {ClassNames.GetObjectNameNoPrefix(component)}") 
			{ 
				style = { unityTextAlign = TextAnchor.MiddleLeft } 
			};
			
			var button = new Button { text = "Restore" };
			button.clicked += () =>
			{
                Blueprint.SetOverridden(compName, false);
                ((BlueprintEditorWrapper)m_SerializedObject.targetObject).SyncPropertiesWithProto();
                m_SerializedObject.ApplyModifiedProperties();
                m_SerializedObject.Update();
			};

			root.Add(label);
			root.Add(button);
			m_RestoreButtonsContainer.Add(root);
		}

		private void AddButtons()
		{
			if (!Blueprint.CanAddComponents())
				return;

			var addBtn = TypePicker.CreatePickerButton("Add Component", 
				() => Blueprint.GetValidComponentTypes().Where(t => !t.HasAttribute<HideInPickerAttribute>()),
				type =>
				{
					Blueprint.AddComponentFromMenu(type);
					m_SerializedObject.ApplyModifiedProperties();
					m_SerializedObject.Update();
				});

			var pasteBtn = new Button 
				{ 
					text = "Paste", 
					style = 
					{ 
						marginTop = new StyleLength(4), 
						marginBottom = new StyleLength(4)
					} 
				};
			pasteBtn.SetEnabled(CopyPasteController.HasBlueprintComponent);
			CopyPasteController.ClipboardElementsChangedEvent += () =>
			{
				pasteBtn.SetEnabled(CopyPasteController.HasBlueprintComponent);
			};
			
			pasteBtn.clicked += () =>
			{
				if (CopyPasteController.HasBlueprintComponent)
				{
					CopyPasteController.PasteProperty(typeof(BlueprintComponent), m_Property);
					m_Property.serializedObject.ApplyModifiedProperties();
					m_SerializedObject.Update();
					Blueprint.SetDirty();
				}
			};

			m_Buttons.Add(addBtn);
			m_Buttons.Add(pasteBtn);
		}

		#endregion Constructor
			
		#region Methods

		private void ItemMoveUp(ComponentElement element)
		{
			int index = element.Index;
			if (index <= 0)
			{
				return;
			}

			m_Property.MoveArrayElement(index, index - 1);
			InvalidateForGroups(index - 1, index);
            
			Blueprint.SetDirty();
			m_SerializedObject.ApplyModifiedProperties();
			m_SerializedObject.Update();
		}

		private void ItemMoveDown(ComponentElement element)
		{
			int index = element.Index;
			if (index < 0 || index > m_Property.arraySize - 2)
			{
				return;
			}

			m_Property.MoveArrayElement(index, index + 1);
			InvalidateForGroups(index, index + 1);
            
			Blueprint.SetDirty();
			m_SerializedObject.ApplyModifiedProperties();
			m_SerializedObject.Update();
		}

		private void ItemRemove(ComponentElement element)
		{
			element.OnMoveDownEvent -= ItemMoveDown;
			element.OnMoveUpEvent -= ItemMoveUp;
			element.OnRemoveEvent -= ItemRemove;
			element.OnCopyEvent -= ItemCopy;

			int index = element.Index;
			if (index != -1)
			{
				var component = Blueprint.ComponentsArray[index];
                
				m_Property.DeleteArrayElementAtIndex(index);
				Blueprint.SetDirty();
				Blueprint.Cleanup();
				InvalidateForGroups(index, m_ComponentsContainer.childCount - 1);
                
				m_SerializedObject.ApplyModifiedProperties();
                
				if (component?.PrototypeLink != null)
                {
                    Blueprint.SetOverridden(component.PrototypeLink.name, true);
                    m_SerializedObject.ApplyModifiedProperties();
                }

				m_SerializedObject.Update();
			}
		}

		private void ItemCopy(ComponentElement element)
		{
			int index = element.Index;
			if (index >= 0)
				CopyPasteController.CopyProperty(m_Property.GetArrayElementAtIndex(index), null);
		}

        public void DisableOnInline()
        {
            m_Buttons.style.display = DisplayStyle.None;
            if (m_Property.arraySize == 0)
                style.marginTop = 0;
        }

    #endregion Methods
	}
}
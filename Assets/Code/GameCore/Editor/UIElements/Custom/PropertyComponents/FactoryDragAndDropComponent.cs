using Kingmaker.Editor.Elements.SmartElementPopulation;
using Kingmaker.Editor.Elements.SmartElementPopulation.Factories;
using Kingmaker.Editor.NodeEditor.Window;
using System;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.UIElements.ValuePicker;
using Kingmaker.ElementsSystem;
using UnityEditor;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.PropertyComponents
{
	public class FactoryDragAndDropComponent : DragAndDropComponent
	{
		private readonly Type m_ElementType;
		private readonly Action m_CreateContentPart;
		private readonly Action m_RemoveContentPart;
		
		private SerializedProperty m_TargetProperty;

		public FactoryDragAndDropComponent(Type elementType, Action createContentPart, Action removeContentPart) 
			: base(null, null)
		{
			m_ElementType = elementType;
			IsValidateFunc = IsDropValid;
			DropFunc = DropProcess;
			m_RemoveContentPart = removeContentPart;
			m_CreateContentPart = createContentPart;
		}
		
		public FactoryDragAndDropComponent(VisualElement target, SerializedProperty property, Type elementType,
			Action createContentPart, Action removeContentPart) : base(target, null, null)
		{
			m_ElementType = elementType;
			IsValidateFunc = IsDropValid;
			DropFunc = DropProcess;
			m_RemoveContentPart = removeContentPart;
			m_CreateContentPart = createContentPart;
			m_TargetProperty = property;
		}

		protected override void OnAttached()
		{
			m_TargetProperty ??= Property.Property;
			base.OnAttached();
		}

		private bool IsDropValid()
		{
            if (m_ElementType == null)
                return false;
            
			ElementDragAndDropController.InitFactories(m_ElementType);
			return ElementDragAndDropController.HasFactories(m_ElementType);
		}

		private void DropProcess()
		{
			var factories = ElementDragAndDropController.GetFactories(m_ElementType);
			if (factories.Count == 0)
				return;

			if (factories.Count == 1)
			{
				OverrideProperty(factories[0]);
			}
			else
			{
				ElementFactoryWithSourcePicker.Show(
					m_DndTarget,
					"Pick result",
					() => factories,
					OverrideProperty
				);
			}

			if (NodeEditorBase.Drawing)
				BlueprintNodeEditor.CheckForNewNodes();
		}

		private void OverrideProperty(ElementFactoryWithSource factory)
		{
			SimpleBlueprint wrappedInstance = null;
			if (m_TargetProperty.serializedObject.targetObject is BlueprintEditorWrapper bew)
				wrappedInstance = bew.WrappedInstance;
			
			if (wrappedInstance == null)
				return;
			
			if (!m_TargetProperty.isArray)
            {
	            var oldElement = (Element) m_TargetProperty.boxedValue;
	            if (oldElement != null)
	            {
		            oldElement.Delete();
		            m_RemoveContentPart?.Invoke();
	            }
            }

            var element = factory.Factory.Create(wrappedInstance, factory.Source);

            if (m_TargetProperty.isArray)
            {
	            m_TargetProperty.arraySize++;
	            m_TargetProperty.GetArrayElementAtIndex(m_TargetProperty.arraySize - 1).boxedValue = element;
            }
            else
            {
	            m_TargetProperty.boxedValue = element;
            }

            m_TargetProperty.serializedObject.ApplyModifiedProperties();
            m_TargetProperty.serializedObject.Update();
            m_CreateContentPart?.Invoke();
		}
	}
}
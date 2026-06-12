using System.Linq;
using Code.GameCore.Editor.Elements.Debug;
using Code.GameCore.ElementsSystem;
using ElementsSystem.Debug;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Editor.Elements;
using Kingmaker.Editor.UIElements.Custom.Array;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.PropertyComponents;
using Kingmaker.Editor.Utility;
using Kingmaker.ElementsSystem;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom
{
	public class ConditionListProperty : ElementListProperty<Condition>
	{
		public ConditionListProperty(SerializedProperty property, string listPropName) : base(property, listPropName)
		{
			var operProp = property.FindPropertyRelative("Operation");
			if (operProp != null)
			{
				var oper = new EnumField(string.Empty);
				oper.style.flexShrink = 1;
				oper.style.width = 50;
                oper.BindProperty(operProp);
                oper.RegisterValueChangedCallback(evt =>
                {
	                m_Array.SetBorderColor((int) (Operation)evt.newValue == 0 ? Color.green : Color.yellow);
                });
                m_Array.SetBorderColor(operProp.intValue == 0 ? Color.green : Color.yellow);
                var owlcatProperty = oper.WrapToOwlcatProperty(operProp);
                owlcatProperty.HeaderContainer.style.display = DisplayStyle.None;
                owlcatProperty.ControlsContainer.style.display = DisplayStyle.None;
                m_Array.SetConditionProperty(owlcatProperty);
			}
		}
	}

	public class ElementListProperty<T> : OwlcatProperty where T : Element
	{
		protected readonly OwlcatListViewProperty m_Array;
		
		public new VisualElement HeaderContainer
			=> m_Array.HeaderContainer;
        
		public new VisualElement ControlsContainer
			=> m_Array.ControlsContainer;
		
		public ElementListProperty(SerializedProperty property, string listPropName, Color? borderColor = null) : 
			base(property, Layout.VerticalNotExpandable)
		{
			var list = Property.FindPropertyRelative(listPropName);
			var types = TypeUtility.CollectValues(list, typeof(T)).ToArray();
			
			m_Array = new OwlcatListViewProperty(property, list, types, borderColor, hideArraySizeField: true);
			ContentContainer.Add(m_Array);
			base.HeaderContainer.style.display = DisplayStyle.None;
			base.ControlsContainer.style.display = DisplayStyle.None;
			m_Array.AddComponent(new ElementListTitleProvider(list));
            m_Array.RemoveComponent<ListDragAndDropComponent>();
			m_Array.AddComponent(new FactoryDragAndDropComponent(m_Array.HeaderContainer, list, typeof(T),
                null, null));
			m_Array.AddComponent(new DebugContextComponent(() =>
                ElementsDebuggerDatabase.Get(FieldFromProperty.GetFieldValue(m_Array.Property) as ElementsList)?.ContextDebugData));
		}
	}
}
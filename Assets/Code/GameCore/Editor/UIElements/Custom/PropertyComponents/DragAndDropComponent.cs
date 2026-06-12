using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.PropertyComponents
{
	public class DragAndDropComponent : OwlcatPropertyComponent
	{
		protected VisualElement m_DndTarget;
        
        private IVisualElementScheduledItem m_Scheduled;
        
		private bool m_IsValidDrag;
		private bool m_IsEnable;

		protected Func<bool> IsValidateFunc { get; set; }

		protected Action DropFunc { get; set; }

		public bool IsEnable
		{
			get => m_IsEnable;
			set
			{
				if (m_IsEnable != value)
				{
					m_IsEnable = value;
					if (m_IsEnable)
						OnEnable();
					else
						OnDisable();
				}
			}
		}

		public DragAndDropComponent(Func<bool> validateFunc, Action dropFunc)
		{
			IsValidateFunc = validateFunc;
			DropFunc = dropFunc;
		}
		
		public DragAndDropComponent(VisualElement target, Func<bool> validateFunc, Action dropFunc)
		{
			m_DndTarget = target;
			IsValidateFunc = validateFunc;
			DropFunc = dropFunc;
			IsEnable = true;
		}

		protected override void OnAttached()
		{
			m_DndTarget ??= Property;
			IsEnable = true;
		}
        
        protected override void OnDetached()
 	    {
 	        IsEnable = false;
 	        m_DndTarget.RemoveFromClassList("factory-target");
 	        RemoveDragTarget();
 	    }

		private void OnEnable()
		{
			m_DndTarget.RegisterCallback<DragEnterEvent>(OnDragEnterEvent);
			m_DndTarget.RegisterCallback<DragLeaveEvent>(OnDragLeaveEvent);
			m_DndTarget.RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
			m_DndTarget.RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
			m_DndTarget.RegisterCallback<DragExitedEvent>(OnDragExitedEvent);
            m_DndTarget.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveEvent);
 	        
 	        if (m_Scheduled == null)
 	            m_Scheduled = m_DndTarget.schedule.Execute(HighlightElement).StartingIn(100).Every(400).Until(() => m_DndTarget == null);
 	        else
 	            m_Scheduled.Resume();
		}

		private void OnDisable()
		{
			m_DndTarget.UnregisterCallback<DragEnterEvent>(OnDragEnterEvent);
			m_DndTarget.UnregisterCallback<DragLeaveEvent>(OnDragLeaveEvent);
			m_DndTarget.UnregisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
			m_DndTarget.UnregisterCallback<DragPerformEvent>(OnDragPerformEvent);
			m_DndTarget.UnregisterCallback<DragExitedEvent>(OnDragExitedEvent);
            m_DndTarget.UnregisterCallback<MouseLeaveEvent>(OnMouseLeaveEvent);
            	            
            m_Scheduled?.Pause();
		}
        
        private void HighlightElement()
        {
            if (m_DndTarget == null)
                return;
  
            if (!IsEnable)
                return;
            
            if (IsValidateFunc())
                m_DndTarget.AddToClassList("factory-target");
            else
                m_DndTarget.RemoveFromClassList("factory-target");
        }
		
		private void OnDragEnterEvent(DragEnterEvent e)
		{
			OnDrag();
		}

		private void OnDragUpdatedEvent(DragUpdatedEvent e)
		{
			OnDrag();
			e.StopPropagation();
		}

		private void OnDragPerformEvent(DragPerformEvent e)
		{
			if (IsValidateFunc())
			{
				DragAndDrop.AcceptDrag();
				e.StopPropagation();
				DropFunc();
				m_DndTarget.RemoveFromClassList("drag-target");
			}
			
			m_DndTarget.RemoveFromClassList("drag-target");
		}
        
        private void OnDragLeaveEvent(DragLeaveEvent e)
        {
            RemoveDragTarget();
        }

		private void OnDragExitedEvent(DragExitedEvent e)
		{
            RemoveDragTarget();
		}
        
        private void OnMouseLeaveEvent(MouseLeaveEvent e)
 	    {
 	        RemoveDragTarget();
 	    }
        
        private void RemoveDragTarget()
        {
            m_DndTarget.RemoveFromClassList("drag-target");
            DragAndDrop.visualMode = DragAndDropVisualMode.None;
        }
		
		private void OnDrag()
		{
			m_IsValidDrag = IsValidateFunc();

            if (m_IsValidDrag)
            {
                m_DndTarget.AddToClassList("drag-target");
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            }
            else
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }
        }
	}
}
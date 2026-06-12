using Kingmaker.Editor.DragDrop;
using UnityEditor;

namespace Owlcat.Editor.CustomGUI
{
    public class OwlcatEditorWindowBase : EditorWindow
    {
        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
        }

        protected virtual void OnGUI()
        {
            if (DragManager.Instance.DragInProgress)
                DragManager.Instance.UpdateDrag();
        }
    }
}
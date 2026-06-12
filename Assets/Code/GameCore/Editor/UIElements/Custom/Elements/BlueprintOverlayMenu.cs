using Code.Framework.Editor.Utility;
using Kingmaker.Editor.UIElements.Custom;
using Owlcat.Editor.Framework.Code.EditorFramework.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
 
namespace Kingmaker.Editor.Blueprints.Elements
{
   public class BlueprintOverlayMenu : VisualElement
   {
       private readonly Button m_SaveButton;
       private readonly Button m_DiscardButton;
 
       private BlueprintOverlayMenu(BlueprintInspectorRoot bir)
       {
           style.flexDirection = FlexDirection.Row;
           style.flexGrow = 1;
           style.flexShrink = 1;
           style.maxHeight = 22;
           style.borderTopWidth = 1;
           style.borderTopColor = ColorUtility.TryParseHtmlString("#232323", out var color) ? color : Color.black;
 
           m_SaveButton = new Button(() => bir?.Save()) 
               { text = "Save", style = { flexGrow = 1, flexShrink = 1, width = Length.Percent(50) } };
               
           m_DiscardButton = new Button(() => bir?.Discard()) 
               { text = "Discard", style = { flexGrow = 1, flexShrink = 1, width = Length.Percent(50) } };
           
           Add(m_SaveButton);
           Add(m_DiscardButton);
       }
 
       public static BlueprintOverlayMenu Create(BlueprintInspectorRoot bir)
       {
           var root = VisualElementEx.GetInspectorRoot(bir);
           VisualElement footer = root.Q(className: "unity-inspector-footer-info");
           if (footer == null)
               return null;
           
           int index = footer.parent?.IndexOf(footer) ?? -1;
           if (index == -1)
                return null;
           
           var instance = new BlueprintOverlayMenu(bir);
           footer.parent.Insert(index, instance);
           
           return instance;
       }
 
       public void OnSetDirty(bool isDirty)
       {
           m_SaveButton.SetEnabled(isDirty);
           m_DiscardButton.SetEnabled(isDirty);
       }
 
       public void OnInspectorDetach()
       {
           RemoveFromHierarchy();
       }
   }
}
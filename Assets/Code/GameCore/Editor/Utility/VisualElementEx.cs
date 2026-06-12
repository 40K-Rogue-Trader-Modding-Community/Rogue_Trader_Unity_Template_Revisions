using System;
using System.Reflection;
using Kingmaker;
using Kingmaker.Editor.UIElements;
using UnityEditor;
using UnityEngine.UIElements;

namespace Code.Framework.Editor.Utility
{
    public static class VisualElementEx
    {
        [Flags]
        public enum InvokePolicy
        {
            Default = 0,
            IncludeDisabled = 1,
            Once = 2,
        }
        
        private static Type s_bindingsStyleHelpersType;
        private static FieldInfo s_rightClickMenuCallbackField;
        private static MethodInfo s_stopContextClickEventMethod;
        
        static VisualElementEx()
        {
            s_bindingsStyleHelpersType = Type.GetType("UnityEditor.UIElements.BindingsStyleHelpers, UnityEditor");
            
            if (s_bindingsStyleHelpersType != null)
            {
                s_rightClickMenuCallbackField = s_bindingsStyleHelpersType.GetField(
                    "s_RightClickMenuCallback", 
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                
                s_stopContextClickEventMethod = s_bindingsStyleHelpersType.GetMethod(
                    "StopContextClickEvent", 
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }
        }
        
        public static void AddIfNotContains(this VisualElement element, VisualElement child)
        {
            if (child == null || element == null)
                return;
            
            if (!element.Contains(child))
                element.Add(child);
        }
        
        public static void RemoveIfContains(this VisualElement element, VisualElement child)
        {
            if (child == null || element == null)
                return;
            
            if (element.Contains(child))
                element.Remove(child);
        }
        
        public static void RegisterCallback<TEventType>(this VisualElement element, 
            EventCallback<TEventType> callback, 
            InvokePolicy invokePolicy = InvokePolicy.Default, 
            TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) 
            where TEventType : EventBase<TEventType>, new()
        {
            var type = typeof(CallbackEventHandler);
            
            var method = type
                .GetMethod("RegisterCallback", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.MakeGenericMethod(typeof(TEventType));

            if (method == null)
            {
                PFLog.Default.Error("Method RegisterCallback<T> not found!");
                return;
            }
            
            method.Invoke(element, new object[] { callback, (int)invokePolicy, useTrickleDown });
        }
        
        /// <summary>
        /// Generates default right click menu which works with UIElements ContextMenuManipulator.
        /// Warning: Intended for BaseField. Use at own risk for other VisualElements. 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="property"></param>
        public static void RegisterRightClickMenu(this VisualElement field, SerializedProperty property)
        {
            field.userData = property.Copy();
            
            if (s_rightClickMenuCallbackField == null || s_stopContextClickEventMethod == null)
            {
                PFLog.Default.Error("BindingsStyleHelpers methods not found via reflection!");
                return;
            }
            
            try
            {
                var rightClickCallback = s_rightClickMenuCallbackField.GetValue(null);
                
                if (rightClickCallback != null)
                {
                    field.RegisterCallback<PointerUpEvent>(
                        (EventCallback<PointerUpEvent>)rightClickCallback, 
                        InvokePolicy.IncludeDisabled, 
                        TrickleDown.TrickleDown);
                }
                
                var stopContextClickDelegate = Delegate.CreateDelegate(
                    typeof(EventCallback<ContextClickEvent>), 
                    s_stopContextClickEventMethod);
                
                field.RegisterCallback<ContextClickEvent>(
                    (EventCallback<ContextClickEvent>)stopContextClickDelegate, 
                    TrickleDown.TrickleDown);
            }
            catch (Exception ex)
            {
                PFLog.Default.Error($"Error accessing BindingsStyleHelpers members: {ex.Message}");
            }
        }
        
        public static BaseFieldMouseDragger GetTextValueFieldDragger(this VisualElement element)
        {
            var type = element.GetType();
            
            while (type != null)
            {
                if (type.IsGenericType &&
                    type.GetGenericTypeDefinition() == typeof(TextValueField<>))
                {
                    var fieldInfo = type.GetField("m_Dragger",
                        BindingFlags.Instance | BindingFlags.NonPublic);

                    if (fieldInfo != null)
                        return fieldInfo.GetValue(element) as BaseFieldMouseDragger;
                }

                type = type.BaseType;
            }

            return null;
        }
        
        public static OwlcatInspectorRoot GetOwlcatRoot(this VisualElement element)
        {
            OwlcatInspectorRoot root = null;
            while (element != null)
            {
                if (element is OwlcatInspectorRoot owlcatInspectorRoot)
                    root = owlcatInspectorRoot;

                element = element.parent;
            }

            return root;
        }
        
        public static VisualElement GetInspectorRoot(VisualElement element)
        {
            while (element.parent?.parent != null)
                element = element.parent;
            
            return element;
        }
        
        public static bool IsBaseFieldSubclass(this VisualElement element)
        {
            var type = element.GetType();
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(BaseField<>))
                    return true;
                
                type = type.BaseType;
            }
            
            return false;
        }
        
        public static VisualElement GetFirstAncestorWhere(this VisualElement element, 
            Func<VisualElement, bool> predicate)
        {
            var current = element.parent;

            while (current != null)
            {
                if (predicate(current))
                    return current;

                current = current.parent;
            }

            return null;
        }
    }
}
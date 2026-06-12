using System;
using System.Reflection;
using Kingmaker;
using UnityEditor;
using UnityEngine.UIElements;

namespace Owlcat.Editor.Framework.Code.EditorFramework.Utility
{
    public static class BindingUtilities
    {
        public static void RegisterSerializedBindCallback(this VisualElement ve, Action<object> callback)
        {
            var asm = typeof(EditorWindow).Assembly;
            var evtType = asm.GetType("UnityEditor.UIElements.SerializedObjectBindEvent");
            if (evtType == null)
            {
                PFLog.Default.Error("SerializedObjectBindEvent type not found!");
                return;
            }

            var methods = typeof(CallbackEventHandler).GetMethods(
                BindingFlags.Instance | BindingFlags.Public
            );

            MethodInfo registerMethod = null;
            foreach (var m in methods)
            {
                if (m.Name == "RegisterCallback" && m.IsGenericMethodDefinition)
                {
                    var parms = m.GetParameters();
                    if (parms.Length == 2 &&
                        parms[0].ParameterType.IsGenericType &&
                        parms[0].ParameterType.GetGenericTypeDefinition() == typeof(EventCallback<>))
                    {
                        registerMethod = m;
                        break;
                    }
                }
            }

            if (registerMethod == null)
            {
                PFLog.Default.Error("RegisterCallback method not found!");
                return;
            }

            var genericMethod = registerMethod.MakeGenericMethod(evtType);

            var actionType = typeof(EventCallback<>).MakeGenericType(evtType);
            var del = Delegate.CreateDelegate(actionType, callback.Target, callback.Method);

            object trickleDownEnum = typeof(TrickleDown).GetField("NoTrickleDown").GetValue(null);
            genericMethod.Invoke(ve, new object[] { del, trickleDownEnum });
        }

        public static SerializedObject GetSerializedObjectFromBindEvent(object evt)
        {
            var evtType = evt.GetType();
            var bindObjProp = evtType.GetProperty("bindObject",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (bindObjProp != null)
            {
                object bindObject = bindObjProp.GetValue(evt);
                return bindObject as SerializedObject;
            }

            PFLog.Default.Error("SerializedObjectBindEvent bindObject property not found!");
            return null;
        }
    }
}

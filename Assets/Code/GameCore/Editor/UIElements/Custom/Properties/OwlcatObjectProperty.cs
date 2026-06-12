using System.Reflection;
using JetBrains.Annotations;
using Kingmaker.Editor.UIElements.ValuePicker;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom
{
	public class OwlcatObjectProperty : OwlcatPropertyField
	{
		public OwlcatObjectProperty(SerializedProperty property) : base(property) { }

		// It looks like we don't need custom picker for UnityEngine.Object types
		// protected override void OnFieldChildrenCreated()
		// {
		//     base.OnFieldChildrenCreated();
		//
		//     var objectField = m_InnerField.Q<ObjectField>();
		//     if (objectField != null)
		//         SetCustomPicker(objectField, FieldFromProperty.GetFieldInfo(Property));
		// }

		public static void SetCustomPicker(ObjectField objectField, FieldInfo fieldInfo)
		{
			var info = typeof(ObjectField).GetProperty("visualInput", BindingFlags.NonPublic | BindingFlags.Instance);
			var control = info.GetValue(objectField) as VisualElement;
			var point = control.hierarchy[1];
			point.RemoveFromHierarchy();
			
			var newPoint = new VisualElement();
			foreach (var clss in point.GetClasses())
				newPoint.AddToClassList(clss);
			
			objectField.style.flexGrow = 1;
			objectField.style.flexShrink = 1;
			newPoint.RegisterCallback<MouseDownEvent>(x => OpenPicker(objectField, fieldInfo));
			control.Add(newPoint);
		}
		
		public static void OpenPicker(ObjectField field, FieldInfo fieldInfo)
		{
			AssetPicker.ShowAssetPicker(field.objectType, fieldInfo, x => field.value = x, field.value);
		}
	}

	public class OwlcatObjectField : ObjectField
	{
		public OwlcatObjectField(string label, [NotNull] FieldInfo fieldInfo) : base(label)
		{
			OwlcatObjectProperty.SetCustomPicker(this, fieldInfo);
		}
	}
}
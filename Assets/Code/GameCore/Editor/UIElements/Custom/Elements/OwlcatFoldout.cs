using Kingmaker.Editor.UIElements.Custom.Base;
using UnityEngine;
using UnityEngine.UIElements;

#nullable enable

namespace Kingmaker.Editor.UIElements.Custom.Elements
{
	public class OwlcatFoldout : OwlcatVisualElement
	{
		private readonly Foldout m_Foldout;

		public OwlcatFoldout(
			string text,
			string viewDataKey,
			int padding = 15,
			EventCallback<ChangeEvent<bool>>? valueChangedCallback = null)
		{
			m_Foldout = new Foldout
			{
				text = text,
				visible = true,
				viewDataKey = viewDataKey,
				style =
				{
					paddingLeft = padding,
				},
			};
			var label = m_Foldout.Q<Label>();
			label.style.unityFontStyleAndWeight = FontStyle.Bold;
			label.style.color = new StyleColor(Color.white * 0.8235f);

			if (valueChangedCallback != null)
			{
				m_Foldout.RegisterValueChangedCallback(valueChangedCallback);
			}

			Add(m_Foldout);
		}

		public void Add(OwlcatVisualElement element)
		{
			m_Foldout.Add(element);
		}
	}
}
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.Blueprints.ProjectView
{
    public class EnterNameWindow : EditorWindow
	{
		private string m_NameTemplate;
		private string m_FullNameTemplate;
		private Action<(bool, string)> m_Callback;

		public static void Show(string nameTemplate, string assetFullName, Action<(bool, string)> action)
		{
			var window = GetWindow<EnterNameWindow>();
			window.m_NameTemplate = nameTemplate;
			window.m_FullNameTemplate = assetFullName;
			window.m_Callback = action;
			
			window.Draw();
		}

		private void Draw()
		{
			titleContent = new GUIContent("Enter Name");

			minSize = new Vector2(400, 110);
			maxSize = new Vector2(400, 110);

			var root = rootVisualElement;
			root.style.alignItems = Align.Center;

			Label label = new Label("Enter Name:");
			label.style.marginTop = 10;
			label.style.marginBottom = 3;
			root.Add(label);

			TextField nameField = new TextField();
			nameField.style.width = 280;
			nameField.style.unityTextAlign = TextAnchor.MiddleCenter;
			root.Add(nameField);

			Label finalString = new Label("{text string}");
			finalString.style.color = new Color(0.72f, 0.72f, 0.72f, 0.33f);
			finalString.style.marginBottom = 10;
			root.Add(finalString);

			var buttonContainer = new VisualElement();
			buttonContainer.style.alignItems = Align.Center;
			buttonContainer.style.flexDirection = FlexDirection.Row;
			buttonContainer.style.alignContent = Align.Center;
			buttonContainer.style.alignSelf = Align.Center;
			root.Add(buttonContainer);

			nameField.SetValueWithoutNotify(m_NameTemplate);
			finalString.text = m_FullNameTemplate.Replace($"%", m_NameTemplate);
			nameField.Focus();

			var confirmButton = new Button() { text = "Confirm" };
			var cancelButton = new Button() { text = "Cancel" };

			confirmButton.style.alignSelf = Align.Center;
			cancelButton.style.alignSelf = Align.Center;

			confirmButton.style.width = 160;
			confirmButton.style.height = 30;

			cancelButton.style.width = 160;
			cancelButton.style.height = 30;

			buttonContainer.Add(confirmButton);
			buttonContainer.Add(cancelButton);

			confirmButton.clicked += OnConfirmPressed;
			cancelButton.clicked += Close;

			nameField.RegisterCallback<ChangeEvent<string>>(evt =>
			{
				m_NameTemplate = evt.newValue;
				finalString.text = m_FullNameTemplate.Replace($"%", m_NameTemplate);
			}, TrickleDown.TrickleDown);

			nameField.RegisterCallback<KeyDownEvent>(evt =>
			{
				if (evt.keyCode == KeyCode.Escape)
				{
					Close();
				}
				else if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
				{
					OnConfirmPressed();
				}
			}, TrickleDown.TrickleDown);
		}

		private void OnConfirmPressed()
		{
			m_Callback?.Invoke((true, m_FullNameTemplate.Replace($"%", m_NameTemplate)));
			Close();
		}
	}
}
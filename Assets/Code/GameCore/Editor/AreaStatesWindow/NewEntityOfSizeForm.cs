﻿using System;
using Kingmaker.Editor.Blueprints.Creation;
using Kingmaker.Editor.UIElements.Custom.Base;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

#nullable enable

namespace Kingmaker.Editor.AreaStatesWindow
{
    public class NewEntityOfSizeForm : FormWindowBase<NewEntityOfSizeForm>
    {
        private string? _toggleLabel;
        private string? _createLabel;
        private Action<Vector2Int>? _createAction;
        private EntityOfSizeElement? _entityOfSizeElement;

        private static void Present(string title, string? toggleLabel, string createLabel, Action<Vector2Int>? createAction)
        {
            Present(
                title,
                init:window =>
                {
                    window._toggleLabel = toggleLabel;
                    window._createLabel = createLabel;
                    window._createAction = createAction;
                },
                isAuxWindow:false);
        }

        protected override void FillContent()
        {
            if (_content == null || _createLabel == null || _createAction == null)
            {
                return;
            }

            _entityOfSizeElement = new EntityOfSizeElement(_toggleLabel);
            _content.Add(_entityOfSizeElement);

            var buttonsLayout = new OwlcatContentContainer
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginTop = 10,
                }
            };
            buttonsLayout.Add(new Button(Create)
            {
                text = _createLabel,
            });
            buttonsLayout.Add(new Button(Close)
            {
                text = "Cancel",
            });
            _content.Add(buttonsLayout);
        }

        private void Create()
        {
            if (_entityOfSizeElement == null || _createAction == null)
            {
                return;
            }
            _createAction(_entityOfSizeElement.Size);
            Close();
        }

        [MenuItem("Owlcat Tools/LevelDesign/Create NavMesh")]
        public static void CreateNavMesh()
        {
            var activeScene = SceneManager.GetActiveScene();
            Present("Create NavMesh", null, "Create", size =>
            {
                SceneProcessor.Process(
                    activeScene.path,
                    scene => SceneProcessor.CreateNavMeshForScene(scene, size));
            });
        }

        [MenuItem("Owlcat Tools/LevelDesign/Create Ground")]
        public static void CreateGround()
        {
            var activeScene = SceneManager.GetActiveScene();
            Present("Create Ground", null, "Create", size =>
            {
                SceneProcessor.Process(
                    activeScene.path,
                    scene => SceneProcessor.CreateGroundForScene(scene, new Vector3(size.x, 1.35f, size.y)));
            });
        }
    }
}
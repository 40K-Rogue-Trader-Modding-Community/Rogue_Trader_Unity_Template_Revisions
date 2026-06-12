using System;
using Editors;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Attributes;
using Kingmaker.Blueprints.Base;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.Blueprints.Elements;
using Kingmaker.Editor.UIElements.Custom.Array;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Elements;
using Kingmaker.Editor.UIElements.Custom.Prototypable;
using Kingmaker.PubSubSystem.Core;
using Kingmaker.Utility.EditorPreferences;
using Owlcat.Editor.Framework.Code.EditorFramework.Utility;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using HelpBox = UnityEngine.UIElements.HelpBox;
using Object = UnityEngine.Object;

namespace Kingmaker.Editor.UIElements.Custom
{
	public class BlueprintInspectorRoot : OwlcatInspectorRoot
	{
		private Button m_SyncWithProtoBtn;
		
		private BlueprintWrapperInspector m_BlueprintInspector;

		public BlueprintScriptableObject Blueprint
			=> BlueprintEditorWrapper.Unwrap<BlueprintScriptableObject>(SerializedObject.targetObject);
		
		private BlueprintScriptableObject BlueprintComplex
			=> BlueprintEditorWrapper.Unwrap<BlueprintScriptableObject>(SerializedObject.targetObject);

		private ComponentsGroup m_Components;

		private bool m_UpdateScheduled;

		private VisualElement m_Actions;
		private VisualElement m_Script;
		private PrototypeProperty m_ProtoField;
		private VisualElement m_AdditionalMenu;
		private VisualElement m_SaveDiscardMenu;
		private BlueprintOverlayMenu m_OverlayMenu;
		
		public BlueprintInspectorRoot(SerializedObject serializedObject, BlueprintWrapperInspector blueprintInspector) : 
			base(serializedObject, true)
		{
			m_BlueprintInspector = blueprintInspector;
			CreateInspector();
			
			this.RegisterSerializedBindCallback(obj =>
			{
				var so = BindingUtilities.GetSerializedObjectFromBindEvent(obj);
				if (so != SerializedObject && SerializedObject != null)
					EditorApplication.delayCall += () => this?.Bind(SerializedObject);
			});

		}
		
		private void CreateInspector()
		{
			if (!Blueprint)
			{
				throw new Exception(
					$"{nameof(BlueprintInspectorRoot)}(): {SerializedObject.targetObject} isn't blueprint");
			}
            
			if (IsBlueprintBroken())
				return;

			InlinedBlueprints.Add(Blueprint);

			m_Actions = new IMGUIContainer(() => BlueprintWrapperInspector.DrawActionsForObject(Blueprint));
			Root.Add(m_Actions);
            
			m_Script = new BlueprintScriptProperty(Blueprint.GetType());
			Root.Insert(0, m_Script);

			var custom = m_BlueprintInspector?.Custom;
            if (custom != null)
            {
                var header = new IMGUIContainer(() => custom.OnHeader(Blueprint));
                var mid = custom.OnBeforeComponentsElement(Blueprint) ?? 
                          new IMGUIContainer(() => custom.OnBeforeComponents(Blueprint));
                Root.Add(header);
                header.SendToBack();
                Root.Add(mid);
            }

            CreateAdditionalMenu();
			// todo: [bp] fix prototype field
			// Allow moders create patches for NonOverridable blueprint types
#if OWLCAT_MODS
			if (Blueprint is IHavePrototype prototype)
#else
			if (Blueprint is IHavePrototype prototype &&
			    Blueprint.GetType().GetAttribute<NonOverridableAttribute>() == null)
#endif
			{
				var prop = SerializedObject.FindProperty("Blueprint");
				var protoProp = prop?.FindPropertyRelative("m_PrototypeId");
				if (protoProp != null)
				{
					m_ProtoField = new PrototypeProperty(protoProp, Blueprint.GetType());
					Root.Insert(Root.IndexOf(m_Script) + 1, m_ProtoField);
					m_ProtoField.CanChangeValue += PrototypeWarning;
					m_ProtoField.OnValueChangedEvent += OnPrototypeBlueprintSet;
				}
			}

			if (Blueprint != null)
			{
				m_Components = new ComponentsGroup(SerializedObject) { style = { marginTop = 15 } };
				Add(m_Components);
			}
			
			if (custom != null)
            {
                var footer = new IMGUIContainer(() => custom.OnFooter(Blueprint));
                Root.Add(footer);
            }
			
			var blueprintElement = Root.Q<OwlcatProperty>("Blueprint");
			if (blueprintElement != null)
			{
				blueprintElement.HeaderContainer.style.display = DisplayStyle.None;
				blueprintElement.ControlsContainer.style.display = DisplayStyle.None;
			}

			if (EditorPreferences.Instance.ShowBlueprintButtonsAsOverlay)
			{
				RegisterCallback<AttachToPanelEvent>(_ =>
				{
					m_OverlayMenu ??= BlueprintOverlayMenu.Create(this);
					if (m_OverlayMenu == null)
						CreateSaveDiscardMenu();
				});
			}
			else
			{
				CreateSaveDiscardMenu();
			}
			
			RegisterCallback<DetachFromPanelEvent>(_ => m_OverlayMenu?.OnInspectorDetach());
		}

		private void CreateAdditionalMenu()
		{
			m_AdditionalMenu = new VisualElement { name = "AdditionalMenu" };
			m_AdditionalMenu.style.flexDirection = FlexDirection.Row;
			m_AdditionalMenu.AddToClassList("labelPart");
			CreateScanBtn(m_AdditionalMenu);
			CreateBaseButton(m_AdditionalMenu);
			CreatePrototypableBtn(m_AdditionalMenu);
			hierarchy.Insert(0, m_AdditionalMenu);
		}

		private void CreateSaveDiscardMenu()
		{
			if (m_SaveDiscardMenu != null)
				return;
			
			m_SaveDiscardMenu = new VisualElement();
			m_SaveDiscardMenu.style.flexDirection = FlexDirection.Row;
			m_SaveDiscardMenu.AddToClassList("labelPart");
			
			var saveBtn = new Button(Save) { text = "Save" };
			saveBtn.name = "SaveButton";
			saveBtn.AddToClassList("grow");
			m_SaveDiscardMenu.Add(saveBtn);
			
			if (EditorPreferences.ProjectIsModTemplate)
			{
				var saveAsPatchBtn = new Button(() =>
					{
						BlueprintPatchEditorUtility.SavePatch(BlueprintComplex);
					}
				) { text = "Save as patch" };
				saveAsPatchBtn.name = "SavePatchButton";
				saveAsPatchBtn.AddToClassList("grow");
				m_SaveDiscardMenu.Add(saveAsPatchBtn);
			}

			var discardBtn = new Button(Discard) { text = "Discard" };

			discardBtn.name = "DiscardButton";
			discardBtn.AddToClassList("grow");
			m_SaveDiscardMenu.Add(discardBtn);

			hierarchy.Add(m_SaveDiscardMenu);
		}
		
		public void Save()
		{
	        foreach (var blueprint in InlinedBlueprints)
	        {
                string id = blueprint?.AssetGuid;
                if (!string.IsNullOrEmpty(id))
                    BlueprintsDatabase.Save(id);
	        }
		}
		
		public void Discard()
		{
			string? id = Blueprint?.AssetGuid;
			if (id == null)
				return;
			
			var lists = this.Query<OwlcatListViewProperty>().Build();
            var elements = this.Query<ElementProperty>().Build();
			foreach (var list in lists)
				list.OnBeforeDiscard();
			
			foreach (var blueprint in InlinedBlueprints)
			{
				string blueprintId = blueprint?.AssetGuid;
				if (!string.IsNullOrEmpty(blueprintId))
					BlueprintsDatabase.Discard(blueprintId);
			}

			SerializedObject.Update();
			m_Components?.CreateComponents();
			foreach (var list in lists)
				list.OnAfterDiscard();
            
            foreach (var elementProperty in elements)
                elementProperty?.Recreate();
		}
		
		private void RecreateInspector()
		{
			if (!Blueprint)
				return;
            
			Root.Clear();
			SerializedObject.GetIterator().Reset();
			SetupContent(this, SerializedObject, true);
			CreateInspector();
		}

		private void CreateScanBtn(VisualElement menuRoot)
		{
			if (Blueprint is IBlueprintScanner scaner)
			{
				var scanBtn = new Button() { text = "Scan", style = { flexShrink = 1 } };
				scanBtn.clicked += () =>
				{
					scaner.Scan();
                    BlueprintsDatabase.SetDirty(Blueprint.AssetGuid);
					SerializedObject.Update();
				};

				scanBtn.AddToClassList("grow");
				menuRoot.Add(scanBtn);
			}
		}

		private void CreateBaseButton(VisualElement menuRoot)
		{
			var windowBtn = new Button(() => BlueprintInspectorWindow.OpenFor(Blueprint)) 
				{ text = "New Window", style = { flexShrink = 1 } };
			
			var findRefBtn = new Button(() =>
			{
				ReferencesWindow.ReferencesWindow.FindReferencesInProject(Blueprint);
			}) { text = "Find References", style = { flexShrink = 1 } };

			windowBtn.AddToClassList("grow");
			findRefBtn.AddToClassList("grow");
			menuRoot.Add(windowBtn);
			menuRoot.Add(findRefBtn);
		}

		private void CreatePrototypableBtn(VisualElement menuRoot)
		{
			//Allow moders create patches for NonOverridable blueprint types
#if OWLCAT_MODS
			if (Blueprint is BlueprintScriptableObject bso)
#else
            if (Blueprint is BlueprintScriptableObject bso && !Blueprint.GetType().HasAttribute<NonOverridableAttribute>())
#endif 
            {
                var createInherited = new Button(() => PrototypableUtility.CreateInheritedAsset(bso)) 
	                { text = "Create inherited", style = { flexShrink = 1 } };

				var syncChildBtn = new Button(() => PrototypableUtility.SyncWithChildren(bso)) 
					{ text = "Sync children", style = { flexShrink = 1 } };
				
				m_SyncWithProtoBtn = new Button(()=> BlueprintEditorWrapper.SyncWithProto(Blueprint as BlueprintScriptableObject)) 
					{ text = "Sync with proto", style = { flexShrink = 1 } };
                m_SyncWithProtoBtn.style.display = bso.PrototypeLink == null ? DisplayStyle.None : DisplayStyle.Flex;

                createInherited.AddToClassList("grow");
                syncChildBtn.AddToClassList("grow");
                m_SyncWithProtoBtn.AddToClassList("grow");

                menuRoot.Add(createInherited);
                menuRoot.Add(syncChildBtn);
                menuRoot.Add(m_SyncWithProtoBtn);
            } 
        }

		private static bool PrototypeWarning(string newValue)
		{
			if (string.IsNullOrEmpty(newValue))
				return true;
            
			return EditorUtility.DisplayDialog(
				"Change prototype",
				"Changing prototype link is a dangerous operation that may affect multiple blueprints. Are you sure?",
				"Yes",
				"No");
		}

		private void OnPrototypeBlueprintSet()
		{
			if (Blueprint.PrototypeLink == null)
			{
				Blueprint.ClearOverrides();
				foreach (var component in Blueprint.ComponentsArray)
				{
					component.PrototypeLink = null;
					component.ClearOverrides();
				}
			}
            
			BlueprintEditorWrapper.UpdateOverridesList(Blueprint);
			SerializedObject.UpdateIfRequiredOrScript();
			m_Components?.CreateComponents();
			foreach (var overridablePropertyControl in this.Query<OverridablePropertyControl>().Build())
				overridablePropertyControl.OnOverrideStateChanged();
		}
		
		public void OnInvalidated() { }
        
		public void OnSetDirty()
		{
			if (m_UpdateScheduled)
				return;
			
			m_UpdateScheduled = true;
			
			EditorApplication.delayCall += () =>
			{
				m_UpdateScheduled = false;

				bool isDirty = false;
				foreach (var blueprint in InlinedBlueprints)
				{
					string id = blueprint.AssetGuid;
					if (string.IsNullOrEmpty(id))
						continue;

					if (BlueprintsDatabase.IsDirty(id))
					{
						isDirty = true;
						break;
					}
				}
                
				m_SaveDiscardMenu?.Q("SaveButton")?.SetEnabled(isDirty);
				m_SaveDiscardMenu?.Q("DiscardButton")?.SetEnabled(isDirty);
				m_OverlayMenu?.OnSetDirty(isDirty);
			};
		}

		private bool IsBlueprintBroken()
		{
			if (Blueprint is not BlueprintBroken blueprintBroken)
				return false;
            
			Root.Clear();
			var buttons = new VisualElement();
			buttons.style.flexDirection = FlexDirection.Row;
			buttons.style.alignSelf = Align.Center;
            
			buttons.Add(new Button(() => 
				{ 
					GUIUtility.systemCopyBuffer = blueprintBroken.Exception?.ToString(); 
				}) { text = "Copy Error" });
            
			string path = BlueprintsDatabase.GetAssetPath(Blueprint);
			if (!string.IsNullOrEmpty(path))
			{
				buttons.Add(new Button(() => EditorUtility.RevealInFinder(path)){ text = "Show in explorer" });
				buttons.Add(new Button(() => Application.OpenURL(path)){ text = "Open as file" });
			}
			
			Root.Add(buttons);
            
			var message = blueprintBroken.Exception?.Message;
			if (string.IsNullOrEmpty(message))
				message = "Blueprint is broken";
            
			Root.Add(new HelpBox(message, HelpBoxMessageType.Error));

			return true;
		}
        
		public void SetInline(bool hideSaveDiscard)
		{
			if (m_Actions != null)
				m_Actions.style.display = DisplayStyle.None;

			if (m_Script != null)
				m_Script.style.display = DisplayStyle.None;

			if (m_ProtoField != null)
				m_ProtoField.style.display = DisplayStyle.None;

			if (m_AdditionalMenu != null)
				m_AdditionalMenu.style.display = DisplayStyle.None;

			if (m_SaveDiscardMenu != null && hideSaveDiscard)
				m_SaveDiscardMenu.style.display = DisplayStyle.None;
			
			if (m_OverlayMenu != null)
			{
		        m_OverlayMenu.style.display = DisplayStyle.None;
		        if (!hideSaveDiscard)
		            CreateSaveDiscardMenu();
			}

			var bpName = this.Q("Blueprint.name");
			if (bpName != null)
				bpName.style.display = DisplayStyle.None;
            
			var guid = this.Q("Blueprint.AssetGuid");
			if (guid != null)
				guid.style.display = DisplayStyle.None;
			
			var comment = this.Q("Blueprint.Comment");
			var textArea = comment?.Q<OwlcatTextField>();
			if (textArea != null)
		    {
	            textArea.style.maxHeight = EditorGUIUtility.singleLineHeight;
	            textArea.style.minHeight = EditorGUIUtility.singleLineHeight;
		    }
            
			m_Components?.DisableOnInline();
		}
	}
}
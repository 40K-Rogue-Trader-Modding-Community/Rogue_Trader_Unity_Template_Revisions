using System;
using System.IO;
using Kingmaker;
using Kingmaker.AI.Blueprints;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.Blueprints.ProjectView;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Levelup.Selections;
using Kingmaker.UnitLogic.Progression.Features;
using Kingmaker.UnitLogic.Progression.Paths;
using Kingmaker.Utility.EditorPreferences;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
    public static class BlueprintContextMenuHelper
	{
		public static void HandleContextMenu(ref GenericMenu gm, FileListView fileView, FileListItem selection)
		{
			var bpParent = BlueprintsDatabase.LoadById<SimpleBlueprint>(selection.Id);

			#region BlueprintUnit
			
			if (bpParent.GetType() == typeof(BlueprintUnit))
			{
				var path = BlueprintsDatabase.FullToRelativePath(fileView.RootPath);

                
				gm.AddItem(new GUIContent("Add to Unit/Brain"), false,
					() =>
					{
                        // reference Unit name - TreasureWorld_Arbites_PetMaster
                        // reference Brain name - TreasureWorld_Arbites_PetMaster_Brain
                        string newAssetName = bpParent.name.Replace("_Unit", string.Empty);
                        newAssetName += "_Brain";
                        
						var bp = HandleBlueprintCreation<BlueprintBrain>(path + "/Brains", newAssetName);

						(bpParent as BlueprintUnit).Editor_SetBrain(bp);
						bpParent.SetDirty();
						bpParent.Save();
						
						Selection.activeObject = BlueprintEditorWrapper.Wrap(bp);
						EditorApplication.delayCall += () => // need time for the indexing server to update
						{
							fileView.Reload();
							fileView.SelectAndRename(bp.AssetGuid);
						};
					});
				gm.AddItem(new GUIContent("Add to Unit/Feature"), false,
					() =>
					{
                        // reference Unit name - TreasureWorld_Arbites_PetMaster
                        // reference Brain name - TreasureWorld_Arbites_Reactivate_Feature
						var assetName = bpParent.name.Replace($"_Unit", string.Empty);
                        assetName += "_%_Feature";
						
						EnterNameWindow.Show("[FeatureName]", assetName, resultData =>
						{
							if (!resultData.Item1)
								return;
							
							var bp = HandleBlueprintCreation<BlueprintFeature>(path + "/Abilities", resultData.Item2);
							(bpParent as BlueprintUnit).Editor_AddFact(bp);
							bpParent.SetDirty();
							bpParent.Save();

							Selection.activeObject = BlueprintEditorWrapper.Wrap(bp);
						});
					});
				gm.AddItem(new GUIContent("Add to Unit/Ability"), false,
					() =>
					{
                        // reference Unit name - TreasureWorld_Arbites_PetMaster
                        // reference Brain name - TreasureWorld_Arbites_Reactivate_Ability
                        var assetName = bpParent.name.Replace($"_Unit", string.Empty);
                        assetName += "_%_Ability";

						EnterNameWindow.Show("[AbilityName]", assetName, resultData =>
						{
							if (!resultData.Item1)
								return;
							var bp = HandleBlueprintCreation<BlueprintAbility>(path + "/Abilities", resultData.Item2);
							(bpParent as BlueprintUnit).Editor_AddFact(bp);
							bpParent.SetDirty();
							bpParent.Save();

							Selection.activeObject = BlueprintEditorWrapper.Wrap(bp);
						});
					});
			}
			
			#endregion BlueprintUnit

			#region BlueprintFeature
			
			if (bpParent.GetType() == typeof(BlueprintFeature))
			{
				var path = BlueprintsDatabase.FullToRelativePath(fileView.RootPath);

				gm.AddItem(new GUIContent("Add to Feature/Ability"), false,
					() =>
					{
						var bpAbility = HandleBlueprintCreation<BlueprintAbility>(path, bpParent.name.Replace($"_Feature", "_Ability"));
						HandleAddFactsLink(bpParent as BlueprintFeature, bpAbility);
						Selection.activeObject = BlueprintEditorWrapper.Wrap(bpAbility);
					});
				
				gm.AddItem(new GUIContent("Add to Feature/Buff"), false,
					() =>
					{
						var bpBuff = HandleBlueprintCreation<BlueprintBuff>(path, bpParent.name.Replace($"_Feature", "_Buff").Replace($"_Ability", "_Buff"));
						Selection.activeObject = BlueprintEditorWrapper.Wrap(bpBuff);
					});
				
				gm.AddItem(new GUIContent("Add to Feature/AreaEffect"), false,
					() =>
					{
						var bp = HandleBlueprintCreation<BlueprintAbilityAreaEffect>(path, bpParent.name.Replace($"_Feature", "_AreaEffect").Replace($"_Ability", "_AreaEffect"));
						Selection.activeObject = BlueprintEditorWrapper.Wrap(bp);
					});
                
                //note: behaviour not defined
				// if (bpParent.name.Contains("_Keystone_"))
				// {
				// 	// can add Keystone Talent and Modifier
				// 	gm.AddItem(new GUIContent("Add to Feature/Keystone Talent (Feature)"), false,
				// 		() =>
				// 		{
				// 			var index = bpParent.name.IndexOf("_Specialist", StringComparison.Ordinal);
				// 			var assetName = "[SpecializationName]_Specialist_[Talent00]";
				// 			var targetPath = path + "/Talents";
				// 			if (index != -1)
				// 			{
				// 				int freeIndex = Directory.Exists(targetPath) ? (Directory.GetFiles(targetPath).Length + 1) : 1;
				//
				// 				assetName = bpParent.name.Remove(index + "_Specialist".Length);
				// 				assetName += $"_Talent{(freeIndex):00}_Feature";
				// 			}
				// 			var bp = HandleBlueprintCreation<BlueprintFeature>(targetPath, assetName);
				// 			Selection.activeObject = BlueprintEditorWrapper.Wrap(bp);
				// 		});
				// 	
				// 	gm.AddItem(new GUIContent("Add to Feature/Keystone Talent (Buff)"), false,
				// 		() =>
				// 		{
				// 			var index = bpParent.name.IndexOf("_Specialist", StringComparison.Ordinal);
				// 			var assetName = "[SpecializationName]_Specialist_[Talent00]";
				// 			var targetPath = path + "/Talents";
				// 			if (index != -1)
				// 			{
				// 				int freeIndex = Directory.Exists(targetPath) ? (Directory.GetFiles(targetPath).Length + 1) : 1;
				// 				assetName = bpParent.name.Remove(index + "_Specialist".Length);
				// 				assetName += $"_Talent{(freeIndex):00}_Buff";
				// 			}
				// 			var bp = HandleBlueprintCreation<BlueprintBuff>(targetPath, assetName);
				// 			Selection.activeObject = BlueprintEditorWrapper.Wrap(bp);
				// 		});
				// }
			}
			
			#endregion BlueprintFeature
                
			#region CareerPath

			// if (bpParent.GetType() == typeof(BlueprintCareerPath) && bpParent.name.Contains("_CareerPath"))
			// {
			// 	var path = BlueprintsDatabase.FullToRelativePath(fileView.RootPath);
			// 	
			// 	// CareerPath - Ability (via Feature and Ability)
			// 	gm.AddItem(new GUIContent("Add to CareerPath/Ability"), false,
			// 		() =>
			// 		{
			// 			var assetName = "[CareerName]_[Ability00]";
			// 			var targetPath = path + "/Abilities";
			// 			var index = bpParent.name.IndexOf("_CareerPath", StringComparison.Ordinal);
			// 			if (index != -1)
			// 			{
			// 				int freeIndex = Directory.Exists(targetPath) ? (Directory.GetFiles(targetPath).Length + 1) : 1;
			//
			// 				assetName = bpParent.name.Remove(index, "_CareerPath".Length);
			// 				assetName += $"_Ability{(freeIndex):00}_Feature";
			// 			}
			// 			var bpFeature = HandleBlueprintCreation<BlueprintFeature>(targetPath, assetName);
			// 			Selection.activeObject = BlueprintEditorWrapper.Wrap(bpFeature);
			// 			
			// 			var bpAbility = HandleBlueprintCreation<BlueprintAbility>(targetPath, bpFeature.name.Replace($"_Feature", "_Ability"));
			// 			HandleAddFactsLink(bpFeature, bpAbility);
			// 			HandleAbilityGroupToCareer(bpParent as BlueprintCareerPath, bpFeature, FeatureGroup.ActiveAbility);
			// 		});
   //              
			// 	// CareerPath - Keystone
			// 	gm.AddItem(new GUIContent("Add to CareerPath/Keystone"), false,
			// 		() =>
			// 		{
			// 			var index = bpParent.name.IndexOf("_CareerPath", StringComparison.Ordinal);
			// 			var assetName = "[CareerName]_[Keystone00]";
			// 			var targetPath = path + "/Keystone";
			// 			if (index != -1)
			// 			{
			// 				int freeIndex = Directory.Exists(targetPath) ? (Directory.GetFiles(targetPath).Length + 1) : 1;
			//
			// 				assetName = bpParent.name.Remove(index, "_CareerPath".Length);
			// 				assetName += $"_Keystone{(freeIndex):00}_Feature";
			// 			}
			// 			var bp = HandleBlueprintCreation<BlueprintFeature>(targetPath, assetName);
			// 			Selection.activeObject = BlueprintEditorWrapper.Wrap(bp);
			// 			
			// 			//TODO - handle add to career
			// 			HandleAbilityGroupToCareer(bpParent as BlueprintCareerPath, bp, FeatureGroup.ActiveAbility);
			// 		});
   //              
			// 	// talent
			// 	gm.AddItem(new GUIContent("Add to CareerPath/Talent"), false,
			// 		() =>
			// 		{
			// 			var index = bpParent.name.IndexOf("_CareerPath", StringComparison.Ordinal);
			// 			var assetName = "[CareerName]_[Talent00]";
			// 			var targetPath = path + "/Talents";
			// 			if (index != -1)
			// 			{
			// 				int freeIndex = Directory.Exists(targetPath) ? (Directory.GetFiles(targetPath).Length + 1) : 1;
			//
			// 				assetName = bpParent.name.Remove(index, "_CareerPath".Length);
			// 				assetName += $"_Talent{(freeIndex):00}_Feature";
			// 			}
			// 			var bp = HandleBlueprintCreation<BlueprintFeature>(targetPath, assetName);
			// 			Selection.activeObject = BlueprintEditorWrapper.Wrap(bp);
			// 			
			// 			//TODO - handle add to career
			// 			HandleAbilityGroupToCareer(bpParent as BlueprintCareerPath, bp, FeatureGroup.Talent);
			// 		});
			// }

			#endregion CareerPath

			#region BlueprintItem

			if (bpParent is BlueprintItem)
			{
				var path = BlueprintsDatabase.FullToRelativePath(fileView.RootPath);

				gm.AddItem(new GUIContent("Add to Item/Feature"), false,
					() =>
					{
						var bp = HandleBlueprintCreation<BlueprintFeature>(path, bpParent.name.Replace($"_Item", "_Feature"));

						Selection.activeObject = BlueprintEditorWrapper.Wrap(bp);
						EditorApplication.delayCall += () => // need time for the indexing server to update
						{
							bpParent.SetDirty();
							fileView.Reload();
							fileView.SelectAndRename(bp.AssetGuid);
						};
					});
				gm.AddItem(new GUIContent("Add to Item/Buff"), false,
					() =>
					{
						var bp = HandleBlueprintCreation<BlueprintBuff>(path, bpParent.name.Replace($"_Item", "_Buff"));

						Selection.activeObject = BlueprintEditorWrapper.Wrap(bp);
						EditorApplication.delayCall += () => // need time for the indexing server to update
						{
							bpParent.SetDirty();
							fileView.Reload();
							fileView.SelectAndRename(bp.AssetGuid);
						};
					});
				gm.AddItem(new GUIContent("Add to Item/Ability"), false,
					() =>
					{
						var bp = HandleBlueprintCreation<BlueprintAbility>(path, bpParent.name.Replace($"_Item", "_Ability"));

						Selection.activeObject = BlueprintEditorWrapper.Wrap(bp);
						EditorApplication.delayCall += () => // need time for the indexing server to update
						{
							bpParent.SetDirty();
							fileView.Reload();
							fileView.SelectAndRename(bp.AssetGuid);
						};
					});
			}

			#endregion BlueprintItem

			#region BlueprintBuff

			if (bpParent is BlueprintBuff)
			{
				var path = BlueprintsDatabase.FullToRelativePath(fileView.RootPath);
				
				gm.AddItem(new GUIContent("Add to Buff/AreaEffect"), false,
					() =>
					{
						var bp = HandleBlueprintCreation<BlueprintAbilityAreaEffect>(path, bpParent.name.Replace($"_Buff", "_AreaEffect").Replace($"_Ability", "_AreaEffect"));
						Selection.activeObject = BlueprintEditorWrapper.Wrap(bp);
					});
			}
			
			#endregion BlueprintBuff
			
			#region BlueprintAbility

			if (bpParent is BlueprintAbility)
			{
				var path = BlueprintsDatabase.FullToRelativePath(fileView.RootPath);
				
				gm.AddItem(new GUIContent("Add to Ability/Buff"), false,
					() =>
					{
						var bp = HandleBlueprintCreation<BlueprintBuff>(path, bpParent.name.Replace($"_Ability", "_Buff").Replace($"_Ability", "_Buff"));
						Selection.activeObject = BlueprintEditorWrapper.Wrap(bp);
					});
				gm.AddItem(new GUIContent("Add to Ability/AreaEffect"), false,
					() =>
					{
						var bp = HandleBlueprintCreation<BlueprintAbilityAreaEffect>(path, bpParent.name.Replace($"_Ability", "_AreaEffect"));
						Selection.activeObject = BlueprintEditorWrapper.Wrap(bp);
					});
			}


			#endregion BlueprintBuff
			
			#region BlueprintArmyDescription

			if (bpParent is BlueprintArmyDescription)
			{
				var path = BlueprintsDatabase.FullToRelativePath(fileView.RootPath);

				gm.AddItem(new GUIContent("Add to Army/Feature"), false,
					() =>
					{
						string assetName = bpParent.name.Replace($"Army", "") + "_%_Feature";
						
						EnterNameWindow.Show("[FeatureName]", assetName, resultData =>
						{
							if (!resultData.Item1)
								return;
							
							var bp = HandleBlueprintCreation<BlueprintFeature>(path + "/ArmyFeatures/", resultData.Item2);
							(bpParent as BlueprintArmyDescription).Editor_AddFeature(bp);
							bpParent.SetDirty();
							bpParent.Save();

							Selection.activeObject = BlueprintEditorWrapper.Wrap(bp);
							EditorApplication.delayCall += () =>
							{
								bpParent.SetDirty();
								fileView.Reload();
								fileView.SelectAndRename(bp.AssetGuid);
							};
						});
					});
			}

			#endregion ArmyType
		}

		private static T HandleBlueprintCreation<T>(string path, string assetName) where T : BlueprintScriptableObject
		{
			var bp = BlueprintsDatabase.CreateAsset(typeof(T), path, assetName);
			(bp as BlueprintScriptableObject).Author = EditorPreferences.Instance.NewBlueprintAuthor;
			bp.SetDirty();
			bp.Save();
			return (T)bp;
		}
		
		private static void HandleAddFactsLink(BlueprintFeature bpFeature, BlueprintUnitFact bpAbility)
		{
			var addFactsParentComponent = bpFeature.GetComponent<AddFacts>();
			if (addFactsParentComponent != null)
			{
				addFactsParentComponent.AddFact(bpAbility);
			}
			else
			{
				addFactsParentComponent = new AddFacts();
				bpFeature.AddComponentFromImport(addFactsParentComponent);
				addFactsParentComponent.AddFact(bpAbility);
			}
			bpFeature.SetDirty();
			bpFeature.Save();
		}
		
		private static void HandleAbilityGroupToCareer(BlueprintCareerPath bpCareer, BlueprintFeature bpFeature, FeatureGroup featureGroup)
		{
			var components = bpCareer.GetComponents<AddFeaturesToLevelUp>();
			AddFeaturesToLevelUp fitComponent = null;
			foreach (var addFeatures in components)
			{
				if (addFeatures.Group == featureGroup)
				{
					fitComponent = addFeatures;
					break;
				}
			}

			if (fitComponent == null)
			{
				fitComponent = new AddFeaturesToLevelUp();
				fitComponent.Group = featureGroup;
				bpCareer.AddComponentFromImport(fitComponent);
			}
			fitComponent.Editor_AddFeature(bpFeature);
			bpCareer.SetDirty();
			bpCareer.Save();
		}
		
		private static string FormNameFromUnitName(string unitName, string assetNameTemplate)
		{
			// Unit name template
			// [chapter]_[zone]_[army-careen]_[name]_Unit
			var split = unitName.Split('_');
			if (split.Length == 5)
			{
				assetNameTemplate = assetNameTemplate
					.Replace("[chapter]", split[0])
					.Replace("[zone]", split[1])
					.Replace("[unit_name]", split[3]);
			}
			else
			{
				PFLog.EditorValidation.Error($"Expected root asset name with 5 sections, received [{split.Length}]. Name [{unitName}]");
			}

			return assetNameTemplate;
		}
	}
}
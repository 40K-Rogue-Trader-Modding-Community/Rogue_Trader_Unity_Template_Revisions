using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Code.Blueprints.Attributes;
using Kingmaker.Editor.Elements;
using Kingmaker.Editor.Utility;
using UnityEditor;
using UnityEngine;

namespace Owlcat.Editor.Elements
{
    public class ContextEvaluatorFilter
    {
        public List<Type> Process(RobustSerializedProperty rsp, List<Type> evaluatorTypes)
        {
            try
            {
                List<ContextEvaluatorFilterRule> rules = LoadRules();
                List<HierarchTypeItem> hierarchyChain = GetHierarchyChain(rsp);
                rules.ForEach(rule => rule.SetupContext(hierarchyChain));
                
                List<Type> filteredTypes = Filter(evaluatorTypes, rules);
                return filteredTypes;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                return evaluatorTypes;
            }
        }

        private List<ContextEvaluatorFilterRule> LoadRules()
        {
            List<ContextEvaluatorFilterRule> rules = new();
            foreach (var type in TypeCache.GetTypesDerivedFrom<ContextEvaluatorFilterRule>())
            {
                if (type.IsAbstract)
                    continue;

                ContextEvaluatorFilterRule rule = (ContextEvaluatorFilterRule)Activator.CreateInstance(type);
                rules.Add(rule);
            }

            return rules;
        }

        private List<HierarchTypeItem> GetHierarchyChain(RobustSerializedProperty rsp)
        {
            List<HierarchTypeItem> chain = new();
            HierarchTypeItem current = new(rsp.Path, rsp.serializedObject.targetObject, rsp.serializedObject.targetObject.GetType());
            chain.Add(current);

            string[] parts = rsp.Path.Split('.');
            foreach (string part in parts)
            {
                if (current.Type.IsArrayOrList() && part == "Array")
                    continue;

                current = GetNextPathPartInfo(current, part);
                chain.Add(current);
            }

            return chain;
        }
        
        private HierarchTypeItem GetNextPathPartInfo(HierarchTypeItem currentItem, string nextPathPart)
        {
            if (nextPathPart.StartsWith("data[")) //data[0]
            {
                int indexStart = nextPathPart.IndexOf('[') + 1;
                int indexEnd = nextPathPart.IndexOf(']');
                int index = int.Parse(nextPathPart.Substring(indexStart, indexEnd - indexStart));

                if (currentItem.Type.IsArray)
                {
                    Array array = currentItem.Object as Array;
                    object nextObject = array.GetValue(index);
                    Type nextType = nextObject?.GetType() ?? currentItem.Type.GetElementType();
                    
                    return new HierarchTypeItem(nextPathPart, nextObject, nextType);
                }

                if (typeof(IList).IsAssignableFrom(currentItem.Type))
                {
                    IList list = currentItem.Object as IList;
                    object nextObject = list[index];
                    Type nextType = nextObject?.GetType() ?? currentItem.Type.GetElementType();
                    
                    return new HierarchTypeItem(nextPathPart, nextObject, nextType);
                }

                throw new Exception($"Cannot index into non-array/list type: {currentItem.Type} at path '{currentItem.PathPart}' with index {index}");
            }
            else
            {
                FieldInfo field = GetFieldRecursive(currentItem.Type, nextPathPart,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                
                if (field == null)
                    throw new Exception($"Field '{nextPathPart}' not found in type {currentItem.Type}");

                EvaluatorFilter filter = field.GetCustomAttribute<EvaluatorFilter>();
                object nextObject = field.GetValue(currentItem.Object);

                Type nextType = field.GetCustomAttribute<SerializeReference>() != null && nextObject != null ? 
                    nextObject.GetType() : field.FieldType;

                HierarchTypeItem item = new(nextPathPart, nextObject, nextType);
                item.Filter = filter;
                
                return item;
            }
        }

        private List<Type> Filter(List<Type> evaluatorTypes, List<ContextEvaluatorFilterRule> rules)
        {
            List<Type> filteredTypes = new();
            foreach (Type evaluatorType in evaluatorTypes)
            {
                bool isPassedAllRules = true;
                foreach (ContextEvaluatorFilterRule rule in rules)
                {
                    if (!rule.IsPass(evaluatorType))
                    {
                        isPassedAllRules = false;
                        break;
                    }
                }

                if (isPassedAllRules)
                    filteredTypes.Add(evaluatorType);
            }

            return filteredTypes;
        }
        
        private FieldInfo GetFieldRecursive(Type type, string fieldName, BindingFlags flags)
        {
            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, flags);
                if (field != null)
                    return field;

                type = type.BaseType;
            }
            
            return null;
        }
    }
}
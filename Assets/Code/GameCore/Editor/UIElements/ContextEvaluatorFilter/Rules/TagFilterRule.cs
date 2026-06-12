using System;
using System.Collections.Generic;
using System.Reflection;
using Code.Blueprints.Attributes;
using Kingmaker.ElementsSystem;

namespace Owlcat.Editor.Elements
{
    public class TagFilterRule : ContextEvaluatorFilterRule
    {
        private string m_Tag;

        public override void SetupContext(List<HierarchTypeItem> hierarchyChain)
        {
            List<HierarchTypeItem> evaluatorsChain = GetEvaluatorsChain(hierarchyChain);
            m_Tag = GetCurrentTag(evaluatorsChain);
        }

        private List<HierarchTypeItem> GetEvaluatorsChain(List<HierarchTypeItem> typeChain)
        {
            List<HierarchTypeItem> evaluators = new();
            foreach (HierarchTypeItem info in typeChain)
                if (typeof(Evaluator).IsAssignableFrom(info.Type))
                    evaluators.Add(info);

            return evaluators;
        }

        private string GetCurrentTag(List<HierarchTypeItem> chain)
        {
            string tag = null;
            
            foreach (HierarchTypeItem item in chain)
            {
                if (item.Filter == null)
                    tag = null;
                else if (item.Filter.FilterName == EvaluatorFilter.Universal)
                    throw new Exception($"EvaluatorFilterTag.Any is not allowed in the middle of the path: {item.PathPart}");
                else if (item.Filter.FilterName != EvaluatorFilter.Inherited)
                    tag = item.Filter.FilterName;
            }

            return tag;
        }

        public override bool IsPass(Type evaluatorType)
        {
            if (m_Tag == null)
                return true;

            EvaluatorFilter filter = evaluatorType.GetCustomAttribute<EvaluatorFilter>();

            if (filter == null)
                return false;

            if (filter.FilterName == EvaluatorFilter.Universal)
                return true;

            return filter.FilterName == m_Tag;
        }
    }
}
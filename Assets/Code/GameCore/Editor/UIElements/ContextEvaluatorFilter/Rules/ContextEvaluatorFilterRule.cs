using System;
using System.Collections.Generic;

namespace Owlcat.Editor.Elements
{
    public abstract class ContextEvaluatorFilterRule
    {
        public abstract void SetupContext(List<HierarchTypeItem> hierarchyChain);
        public abstract bool IsPass(Type evaluatorType);

        protected bool Contains<T>(List<HierarchTypeItem> fullChain)
        {
            Type searchType = typeof(T);
            foreach (HierarchTypeItem item in fullChain)
            {
                if (item.Type == searchType)
                    return true;
            }

            return false;
        }
    }
}
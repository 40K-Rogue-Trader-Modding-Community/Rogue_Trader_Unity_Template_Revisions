using System;
using Kingmaker.Editor.UIElements.Custom.Base;

namespace Kingmaker.Editor.UIElements.Custom.PropertyComponents
{
    public class FuncTitleProviderComponent : IOwlcatPropertyTitleProvider
    {
        public FuncTitleProviderComponent(Func<string> titleFunc)
        {
            m_TitleFunc = titleFunc;
        }
        
        public FuncTitleProviderComponent(Func<string> titleFunc, int order)
        {
            m_TitleFunc = titleFunc;
            m_Order = order;
        }

        private Func<string> m_TitleFunc;
        private int m_Order;
        
        void IOwlcatPropertyComponent.AttachToProperty(OwlcatProperty property)
        {
        }
        
        public void DetachFromProperty() { }

        string IOwlcatPropertyTitleProvider.GetTitle()
            => m_TitleFunc.Invoke();

        int IOwlcatPropertyTitleProvider.Order => m_Order;
    }
}

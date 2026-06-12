using System.Collections.Generic;
using Kingmaker.Editor.UIElements.Custom.Base;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.PropertyComponents
{
	public interface IOwlcatPropertyComponent
	{
		void AttachToProperty(OwlcatProperty property);
		void DetachFromProperty();
	}

	public class OwlcatPropertyComponent : IOwlcatPropertyComponent
	{
		protected OwlcatProperty Property { get; private set; }

		public virtual void AttachToProperty(OwlcatProperty property)
		{
			Property = property;
			OnAttached();
		}
		
		public void DetachFromProperty()
		{
			OnDetached();
		}

		protected virtual void OnAttached() { }
		
		protected virtual void OnDetached() { }
	}
	
	public class OwlcatPropertyComponent<TProperty> : OwlcatPropertyComponent where TProperty : OwlcatProperty
	{
		protected new TProperty Property
			=> (TProperty)base.Property;

		public sealed override void AttachToProperty(OwlcatProperty property)
		{
			if (!(property is TProperty))
			{
				PFLog.Default.Error(
					$"Can't attach component {GetType().Name} to property {property.GetType().Name}: " +
					$"required property type is {nameof(TProperty)}");
				return;
			}
	
			base.AttachToProperty(property);
		}
	}

	public interface IOwlcatPropertyTitleProvider : IOwlcatPropertyComponent
	{
		string GetTitle();
		int Order { get; }
	}

	public interface IOwlcatPropertyInputHandler : IOwlcatPropertyComponent
	{
		void TryHandle(KeyDownEvent evt);
		int Order { get; }
	}

	public class OwlcatPropertyInputHandlerSorter : IComparer<IOwlcatPropertyInputHandler>
	{
		public static readonly OwlcatPropertyInputHandlerSorter Instance = new OwlcatPropertyInputHandlerSorter();
		
		public int Compare(IOwlcatPropertyInputHandler x, IOwlcatPropertyInputHandler y)
			=> x.Order.CompareTo(y.Order);
	}
}
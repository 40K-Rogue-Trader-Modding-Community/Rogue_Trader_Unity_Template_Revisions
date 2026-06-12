namespace Kingmaker.Editor.UIElements.Custom.PropertyComponents
{
    public class ReadOnlyComponent : OwlcatPropertyComponent
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            Property.ContentContainer.SetEnabled(false);
            Property.ControlsContainer.SetEnabled(false);
        }
    }
}
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium
{
    public sealed class MyceliariumRootTab : ControlPanelTab
    {
        public override string DisplayName => "Myceliarium";

        public override string PathToUxml => "Editor/Uxml/MyceliariumRootTab";

        protected override void RegisterMainClickable()
        {
            // Given how we want other tabs nested under ours, we're using a 
            // non-Button to serve as the main button for this tab.
            _mainClickable = Root.Q<Foldout>();
            if (_mainClickable == null)
            {
                string logMessage = $"Failed to find a Foldout in the tab UXML " +
                    $"at {PathToUxml} for {GetType().Name}.";
                throw new System.InvalidOperationException(logMessage);
            }
        }

        protected override StyleLength OverrideSubtabHeight { get; set; } = 50f;
    }
}

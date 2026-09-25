using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium
{
    public sealed class MyceliariumAboutTab : ControlPanelTab
    {
        public override string DisplayName => "About";
        public override string PathToUxml => "Editor/Uxml/MyceliariumAboutTab";
        protected override StyleLength OverrideSubtabHeight { get; set; } = 50f;
    }
}

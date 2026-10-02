using UnityEngine.UIElements;
using UitkLabel = UnityEngine.UIElements.Label;

namespace AtMycelia.Myceliarium
{
    public sealed class MyceliariumAboutSubwindow : ControlPanelSubwindow
    {
        public override string PathToUxml => "Editor/Uxml/MyceliariumAboutSubwindow";
        
        private UitkLabel _versionLabel;
        private UitkLabel _copyrightLabel;
        private UitkLabel _multiLinksLabel;

        protected override void RegisterVisualElements()
        {
            _versionLabel = Root.Q<UitkLabel>("VersionLabel");
            _copyrightLabel = Root.Q<UitkLabel>("CopyrightLabel");
            _multiLinksLabel = Root.Q<UitkLabel>("MultiLinksLabel");
        }

        public string VersionText
        {
            get => _versionLabel.text;
            set => _versionLabel.text = value;
        }

        public string CopyrightText
        {
            get => _copyrightLabel.text;
            set => _copyrightLabel.text = value;
        }

        public string MultiLinksText
        {
            get => _multiLinksLabel.text;
            set => _multiLinksLabel.text = value;
        }

    }
}

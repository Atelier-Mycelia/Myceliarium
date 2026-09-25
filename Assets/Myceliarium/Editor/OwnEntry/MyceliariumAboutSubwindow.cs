using UnityEngine.UIElements;
using UitkLabel = UnityEngine.UIElements.Label;

namespace AtMycelia.Myceliarium
{
    public sealed class MyceliariumAboutSubwindow : ControlPanelSubwindow
    {
        public override string PathToUxml => "Editor/Uxml/MyceliariumAboutSubwindow";
        
        public override void Init()
        {
            base.Init();
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            _versionLabel.text = $"Version: {MyceliariumAboutEntry.VersionString}";
            _copyrightLabel.text = "Copyright © 2026 AtMycelia. All rights reserved.";
            _multiLinksLabel.text = string.Join("\n", Links);
        }

        private UitkLabel _versionLabel;
        private UitkLabel _copyrightLabel;
        private UitkLabel _multiLinksLabel;

        private static readonly string[] Links = new string[]
        {
            "Twitter/X: https://x.com/AtelierMycelia",
            "Itch.io: https://ateliermycelia.itch.io/",
            "Github: https://github.com/Atelier-Mycelia",
            "Repository: https://github.com/Atelier-Mycelia/Myceliarium"
        };
    
        protected override void RegisterVisualElements()
        {
            _versionLabel = Root.Q<UitkLabel>("VersionLabel");
            _copyrightLabel = Root.Q<UitkLabel>("CopyrightLabel");
            _multiLinksLabel = Root.Q<UitkLabel>("MultiLinksLabel");
        }

    }
}

namespace AtMycelia.Myceliarium
{
    public sealed class MyceliariumAboutEntry : ControlPanelEntry, IAtMyceliaControlPanelEntry
    {
        public override int SortingOrder => 0;
        public override bool IsTopLevel => false;

        public override string MainDisplayName => "Myceliarium";

        public override bool IsMeantToHaveSubwindow => true;

        protected override void PrepareLeftSidebarTab()
        {
            Tab = new MyceliariumAboutTab();
            Tab.Init();
        }

        protected override void PrepareSubwindow()
        {
            _subwindow ??= new MyceliariumAboutSubwindow();
            _subwindow.Init();
        }

        protected override void OnTabClicked(IControlPanelTab tabClicked)
        {
            UpdateLabels();
            base.OnTabClicked(tabClicked);
        }

        private void UpdateLabels()
        {
            VersionText = $"Version: {VersionString}";
            CopyrightText = "Copyright © 2026 Atelier Mycelia. All rights reserved.";
            MultiLinksText = string.Join("\n", Links);
        }

        private string VersionString => MyceliariumRootEntry.VersionString;

        private string VersionText
        {
            get => TypedSubwindow.VersionText;
            set => TypedSubwindow.VersionText = value;
        }

        private string CopyrightText
        {
            get => TypedSubwindow.CopyrightText;
            set => TypedSubwindow.CopyrightText = value;
        }

        private string MultiLinksText
        {
            get => TypedSubwindow.MultiLinksText;
            set => TypedSubwindow.MultiLinksText = value;
        }

        private MyceliariumAboutSubwindow TypedSubwindow
        {
            get => _subwindow as MyceliariumAboutSubwindow;
        }

        private static readonly string[] Links = new string[]
        {
            "Twitter/X: https://x.com/AtelierMycelia",
            "Itch.io: https://ateliermycelia.itch.io/",
            "Github: https://github.com/Atelier-Mycelia",
            "Repository: https://github.com/Atelier-Mycelia/Myceliarium"
        };
    }
}

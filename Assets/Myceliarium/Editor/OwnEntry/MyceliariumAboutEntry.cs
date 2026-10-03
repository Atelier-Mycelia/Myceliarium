namespace AtMycelia.Myceliarium
{
    public sealed class MyceliariumAboutEntry : ControlPanelEntry, IAtMyceliaControlPanelEntry
    {
        public override int SortingOrder => 0;
        public override bool IsTopLevel => false;

        public override string SortingName => "Myceliarium";

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

        public override void Select()
        {
            base.Select();
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            VersionText = $"Version: {VersionString}";
            CopyrightText = "Copyright © 2026 Atelier Mycelia. All rights reserved.";
            MultiLinksText = string.Join("\n", Links);
        }

        private string VersionText
        {
            get => AboutSubwindow.VersionText;
            set => AboutSubwindow.VersionText = value;
        }

        private MyceliariumAboutSubwindow AboutSubwindow
        {
            get => _subwindow as MyceliariumAboutSubwindow;
        }

        private string VersionString => MyceliariumRootEntry.VersionString;

        private string CopyrightText
        {
            get => AboutSubwindow.CopyrightText;
            set => AboutSubwindow.CopyrightText = value;
        }

        private string MultiLinksText
        {
            get => AboutSubwindow.MultiLinksText;
            set => AboutSubwindow.MultiLinksText = value;
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

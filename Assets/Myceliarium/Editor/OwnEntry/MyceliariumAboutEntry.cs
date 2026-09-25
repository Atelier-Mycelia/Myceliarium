namespace AtMycelia.Myceliarium
{
    public sealed class MyceliariumAboutEntry : ControlPanelEntry, IAtMyceliaControlPanelEntry
    {
        public override int SortingOrder => 0;
        public override bool IsTopLevel => false;

        public override string MainDisplayName => "Myceliarium";
        public static readonly string VersionString = "v0.1.0";

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
    }
}

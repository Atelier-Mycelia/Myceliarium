using System.Collections.Generic;

namespace AtMycelia.Myceliarium
{
    public sealed class MyceliariumRootEntry : ControlPanelEntry, IAtMyceliaControlPanelEntry
    {
        public override int SortingOrder => 0;
        public override bool IsTopLevel => true;

        public override string SortingName => "Myceliarium";
        public static readonly string VersionString = "v0.3.2";

        public override bool IsMeantToHaveSubwindow => false;

        protected override void PrepareLeftSidebarTab()
        {
            Tab = new MyceliariumRootTab();
            Tab.Init();
        }

        protected override void PrepareSubentries()
        {
            base.PrepareSubentries();

            #region Register the instances
            var subsToAdd = new List<ControlPanelEntry>
            {
                new MyceliariumAboutEntry(),
            };
            _subentries.AddRange(subsToAdd);
            #endregion

            #region Init Subentries
            for (int i = 0; i < _subentries.Count; i++)
            {
                var elem = _subentries[i];
                elem.Init();
                Tab.Register(elem.Tab);
            }
            #endregion
        }
    }
}

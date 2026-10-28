using System;
using System.Collections.Generic;

namespace AtMycelia.Myceliarium
{
    public sealed class MyceliariumRootEntry : ControlPanelEntry, IAtMyceliaControlPanelEntry
    {
        public override int SortingOrder => 0;
        public override bool IsTopLevel => true;

        public override string SortingName => "Myceliarium";
        public static readonly string VersionString = "v0.5.1";

        public override bool IsMeantToHaveSubwindow => false;

        protected override Type SidebarTabType => typeof(MyceliariumRootTab);

        protected override IEnumerable<IControlPanelEntry> CreateSubentries()
        {
            return new IControlPanelEntry[]
            {
                new MyceliariumAboutEntry(),
            };
        }
    }
}

using UnityEditor;
using Type = System.Type;

namespace AtMycelia.Myceliarium
{
    public class AtMyceliaControlPanel : ControlPanel
    {
        #region Configurable Properties
        protected override string PathToUxml => "Editor/Uxml/ControlPanel";
        protected override string DisplayName => "Atelier Mycelia Control Panel";
        #endregion

        #region Super Types
        protected override Type EntrySuperType => typeof(IAtMyceliaControlPanelEntry);
        protected override Type LoaderSuperType => typeof(IAtMyceliaControlPanelEntryLoader);
        protected override Type SaverSuperType => typeof(IAtMyceliaControlPanelEntrySaver);

        #endregion

        [MenuItem("Window/Atelier Mycelia/Control Panel", priority = 0)]
        public static void BringUp()
        {
            if (S != null)
            {
                S.Focus();
                return;
            }

            GetWindow<AtMyceliaControlPanel>();
        }

        public static AtMyceliaControlPanel S { get; private set; }

        

    }

}
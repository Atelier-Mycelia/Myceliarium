using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityDebug = UnityEngine.Debug;
using Type = System.Type;

namespace AtMycelia.Myceliarium
{
    public class AtMyceliaControlPanel : ControlPanel
    {
        #region Configurable Properties
        protected override string PathToUxml => "Editor/Uxml/ControlPanel";
        protected override string WindowTitle => "Atelier Mycelia Control Panel";
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

        protected override Type EntrySuperType => typeof(IAtMyceliaControlPanelEntry);
        protected override Type LoaderSuperType => typeof(IAtMyceliaControlPanelEntryLoader);
        protected override Type SaverSuperType => typeof(IAtMyceliaControlPanelEntrySaver);

    }

}
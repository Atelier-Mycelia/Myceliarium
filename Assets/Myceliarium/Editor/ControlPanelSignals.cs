using System;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Event Bus for the Control Panel. This class is used to send and respond to 
    /// signals between different parts of the Control Panel.
    /// </summary>
    public static class ControlPanelSignals 
    {
        public static Action<IControlPanel> OnControlPanelOpened = delegate { };
        public static Action<IControlPanel> OnControlPanelClosed = delegate { };

        public static Action<IControlPanelEntry> OnEntryTabClicked = delegate { };

        public static Action<IControlPanelEntry> SaveRequested = delegate { };
        public static Action<IControlPanelEntry> LoadRequested = delegate { };

        public static Action<IControlPanelEntry> SaveCompleted = delegate { };
        public static Action<IControlPanelEntry> LoadCompleted = delegate { };

        public static Action<IControlPanelEntry> SaveFailed = delegate { };
        public static Action<IControlPanelEntry> LoadFailed = delegate { };
    }
}
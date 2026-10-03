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

        /// <summary>
        /// Occurs when a Control Panel (passed as the arg) requests to save its data. 
        /// This is a signal to any interested parties that they should save the data 
        /// for the given Control Panel.
        /// </summary>
        public static Action<IControlPanel> SaveRequested = delegate { };

        public static Action<IControlPanelEntry> SaveCompleted = delegate { };
        public static Action<IControlPanelEntry> LoadCompleted = delegate { };

        public static Action<IControlPanelEntry> SaveFailed = delegate { };
        public static Action<IControlPanelEntry> LoadFailed = delegate { };

        public static Action<IControlPanel> CloseRequested = delegate { };
    }
}
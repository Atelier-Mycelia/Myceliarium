namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Event Bus for the Control Panel. This class is used to send and respond to 
    /// signals between different parts of the Control Panel.
    /// </summary>
    public static class ControlPanelSignals 
    {
        public static System.Action<IControlPanel> OnControlPanelOpened = delegate { };
        public static System.Action<IControlPanel> OnControlPanelClosed = delegate { };

        public static System.Action<IControlPanelEntry> OnEntryTabClicked = delegate { };

        public static System.Action<IControlPanel> SaveRequested = delegate { };
    }
}
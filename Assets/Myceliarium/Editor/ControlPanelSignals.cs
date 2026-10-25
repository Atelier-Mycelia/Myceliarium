using System;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Event Bus for the Control Panel. This class is used to send and respond to 
    /// signals between different parts of the Control Panel.
    /// </summary>
    public static class ControlPanelSignals
    {
        public static Action<IControlPanel> ControlPanelOpened = delegate { };

        /// <summary>
        /// Delegate invoked immediately before an IControlPanel is closed.
        /// </summary>
        /// <remarks>Invoked synchronously on the closing path; handlers should avoid long-running work.
        /// Defaults to an empty delegate so callers can invoke it without null checks.</remarks>
        public static Action<IControlPanel> PreControlPanelClosed = delegate { };

        public static Action<IControlPanelEntry> EntryTabClicked = delegate { };

        /// <summary>
        /// Occurs when a Control Panel (passed as the arg) requests to save its data. 
        /// This is a signal to any interested parties that they should save the data 
        /// for the given Control Panel.
        /// </summary>
        public static Action<IControlPanel> SaveRequested = delegate { };

        /// <summary>
        /// Occurs when an entry's HasUnsavedChanges changes, so that panels can react
        /// without having to poll their entries.
        /// </summary>
        public static Action<IControlPanelEntry> UnsavedChangesChanged = delegate { };

        public static Action<IControlPanelEntry> SaveCompleted = delegate { };
        public static Action<IControlPanelEntry> LoadCompleted = delegate { };

        public static Action<IControlPanelEntry> SaveFailed = delegate { };
        public static Action<IControlPanelEntry> LoadFailed = delegate { };

        public static Action<IControlPanel> CloseRequested = delegate { };

        /// <summary>
        /// Occurs when a Control Panel's footer asks for all its entries to be reset
        /// to their startup state.
        /// </summary>
        public static Action<IControlPanel> ReinitRequested = delegate { };

        /// <summary>
        /// Occurs when the language is changed in a Control Panel's footer. The string
        /// is the newly selected language.
        /// </summary>
        public static Action<IControlPanel, string> LanguageChanged = delegate { };

    }
}
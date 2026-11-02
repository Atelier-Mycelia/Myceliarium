namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Implemented by entries that carry a logic module (ControlPanelEntry does). This is
    /// what undo requests and the logic itself go through, rather than IControlPanelEntry,
    /// so that the many lightweight implementations of that don't all need to provide
    /// these members.
    /// </summary>
    public interface IEntryLogicOwner
    {
        IControlPanelEntryLogic Logic { get; }

        /// <summary>
        /// The entry's subwindow, or null if it doesn't have one (yet). Unlike
        /// IControlPanelEntry.Subwindow, never throws.
        /// </summary>
        IControlPanelSubwindow SubwindowIfAny { get; }
    }

    /// <summary>
    /// Decides what an undo/redo request does for a panel's displayed entry. Split out
    /// of ControlPanel so it can be tested without a live EditorWindow.
    /// </summary>
    public static class ControlPanelUndoRouter
    {
        /// <param name="displayedEntry">The entry the panel is currently showing, if any.</param>
        /// <returns>Whether the entry's logic actually undid/redid something.</returns>
        public static bool Route(IControlPanelEntry displayedEntry, bool redo)
        {
            var logic = (displayedEntry as IEntryLogicOwner)?.Logic;
            if (logic == null)
            {
                return false;
            }

            logic.CommitPendingEdits();

            if (redo)
            {
                if (!logic.CanRedo)
                {
                    return false;
                }
                logic.Redo();
                return true;
            }

            if (!logic.CanUndo)
            {
                return false;
            }
            logic.Undo();
            return true;
        }
    }
}

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Entries that can respond to Ctrl+Z / Ctrl+Y while their ControlPanel is focused.
    /// ControlPanelEntry implements this, so entries get it by default.
    /// </summary>
    public interface IUndoableEntry
    {
        /// <summary>
        /// Finishes any edit still in progress in the entry's subwindow, so it's on the
        /// undo history before an undo or redo happens.
        /// </summary>
        void CommitPendingEdits();

        bool CanUndo { get; }
        bool CanRedo { get; }
        void Undo();
        void Redo();
    }

    /// <summary>
    /// Decides what an undo/redo request does for a panel's displayed entry. Split out
    /// of ControlPanel so it can be tested without a live EditorWindow.
    /// </summary>
    public static class ControlPanelUndoRouter
    {
        /// <param name="displayedEntry">The entry the panel is currently showing, if any.</param>
        /// <returns>Whether the entry actually undid/redid something.</returns>
        public static bool Route(IControlPanelEntry displayedEntry, bool redo)
        {
            if (!(displayedEntry is IUndoableEntry undoable))
            {
                return false;
            }

            undoable.CommitPendingEdits();

            if (redo)
            {
                if (!undoable.CanRedo)
                {
                    return false;
                }
                undoable.Redo();
                return true;
            }

            if (!undoable.CanUndo)
            {
                return false;
            }
            undoable.Undo();
            return true;
        }
    }
}

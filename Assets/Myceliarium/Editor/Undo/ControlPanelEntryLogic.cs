using System;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// The part of a ControlPanelEntry that owns the working state the user edits (the
    /// entry itself orchestrates its tab, subwindow, loader and so on). Opt-in: entries
    /// get one by overriding ControlPanelEntry.PrepareLogic.
    /// </summary>
    public interface IControlPanelEntryLogic
    {
        /// <summary>
        /// Everything the user did to the state since it was last loaded or saved.
        /// </summary>
        UndoStack History { get; }

        /// <summary>
        /// Whether the state differs from the last loaded or saved one. Derived from the
        /// history, so undoing back to the saved state makes this false again.
        /// </summary>
        bool HasUnsavedChanges { get; }

        /// <summary>
        /// Raised when HasUnsavedChanges may have changed.
        /// </summary>
        event Action UnsavedChangesMayHaveChanged;

        /// <summary>
        /// Replaces the working state with what the entry's loader produced. Clears the
        /// history, since its commands refer to state that's now gone, and marks the new
        /// state as saved.
        /// </summary>
        void Load(object loadResult);

        /// <summary>
        /// Says that the state as it is now is the saved one.
        /// </summary>
        void MarkSaved();

        /// <summary>
        /// Builds the command for an edit the user already made in the subwindow, or
        /// returns null if this doesn't handle it. The command is recorded, not executed,
        /// so it's applied to the state when redoing and reverted when undoing.
        /// </summary>
        IUndoableCommand CreateEditCommand(FieldEdit edit);
    }

    /// <summary>
    /// Base for logic modules: takes care of the history and of deriving the unsaved
    /// changes flag from it. Note that only changes that go through History (Push or
    /// Record) count as changes.
    /// </summary>
    public abstract class ControlPanelEntryLogic : IControlPanelEntryLogic
    {
        protected ControlPanelEntryLogic()
        {
            History.Changed += OnHistoryChanged;
        }

        public UndoStack History { get; } = new UndoStack();

        public virtual bool HasUnsavedChanges => !History.IsClean;

        public event Action UnsavedChangesMayHaveChanged = delegate { };

        public void Load(object loadResult)
        {
            AdoptState(loadResult);
            History.Clear();
            History.MarkClean();
        }

        /// <summary>
        /// Takes the loader's result as the new working state (usually by copying what
        /// it needs out of it, so that edits don't change the saved data directly).
        /// </summary>
        protected abstract void AdoptState(object loadResult);

        public void MarkSaved()
        {
            History.MarkClean();
        }

        public virtual IUndoableCommand CreateEditCommand(FieldEdit edit)
        {
            return null;
        }

        private void OnHistoryChanged()
        {
            UnsavedChangesMayHaveChanged();
        }
    }
}

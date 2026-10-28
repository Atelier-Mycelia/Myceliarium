using System;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// The part of a ControlPanelEntry that owns the working state the user edits, along
    /// with the undo history and unsaved changes tracking that go with it. The entry
    /// itself orchestrates its tab, subwindow, loader and so on. Every entry has one;
    /// entries without state of their own get a DefaultControlPanelEntryLogic.
    /// </summary>
    public interface IControlPanelEntryLogic : IDisposable
    {
        /// <summary>
        /// Called by the entry once its tab, subwindow and loader are up.
        /// </summary>
        void Init(IControlPanelEntry owner);

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

        #region Undo
        bool CanUndo { get; }
        bool CanRedo { get; }

        /// <summary>
        /// Check CanUndo first; asking for an undo when there's nothing to undo logs a
        /// warning.
        /// </summary>
        void Undo();

        /// <summary>
        /// Check CanRedo first; asking for a redo when there's nothing to redo logs a
        /// warning.
        /// </summary>
        void Redo();

        /// <summary>
        /// Finishes any edit still in progress in the entry's subwindow (say, text typed
        /// into a field that hasn't lost focus yet), so it's on the history before an
        /// undo or redo happens.
        /// </summary>
        void CommitPendingEdits();
        #endregion

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
    /// Base for logic modules: takes care of the history, of deriving the unsaved
    /// changes flag from it, and of recording the edits the subwindow reports. Note that
    /// only changes that go through History (Push or Record) count as changes.
    /// </summary>
    public abstract class ControlPanelEntryLogic : IControlPanelEntryLogic
    {
        protected ControlPanelEntryLogic()
        {
            History.Changed += OnHistoryChanged;
        }

        public virtual void Init(IControlPanelEntry owner)
        {
            _owner = owner;
            SetSubs(false);
            SetSubs(true);
        }

        protected IControlPanelEntry _owner;

        // Null if there isn't one, which isn't an error here (the entry is the one that
        // complains about a missing subwindow, when it's asked for it).
        protected IControlPanelSubwindow Subwindow => (_owner as IEntryLogicOwner)?.SubwindowIfAny;

        protected IControlPanelEntryLoader Loader => _owner?.Loader;

        /// <summary>
        /// For what the logic needs to react to. Tab clicks and the panel opening aren't
        /// part of that: the entry owns the navigation side of things.
        /// </summary>
        protected virtual void SetSubs(bool wantsSubsActive)
        {
            if (wantsSubsActive)
            {
                ControlPanelSignals.SaveCompleted += OnSaveCompleted;
                if (Subwindow is IEditReportingSubwindow reporter)
                {
                    reporter.EditCommitted += OnSubwindowEdit;
                }
            }
            else
            {
                ControlPanelSignals.SaveCompleted -= OnSaveCompleted;
                if (Subwindow is IEditReportingSubwindow reporter)
                {
                    reporter.EditCommitted -= OnSubwindowEdit;
                }
            }
        }

        private void OnSaveCompleted(IControlPanelEntry entry)
        {
            if (_owner != null && entry == _owner)
            {
                MarkSaved();
            }
        }

        public UndoStack History { get; } = new UndoStack();

        #region Unsaved changes
        public virtual bool HasUnsavedChanges => !History.IsClean;

        public event Action UnsavedChangesMayHaveChanged = delegate { };

        private void OnHistoryChanged()
        {
            if (_owner != null)
            {
                ControlPanelSignals.UnsavedChangesChanged(_owner);
            }
            UnsavedChangesMayHaveChanged();
        }

        public virtual void MarkSaved()
        {
            History.MarkClean();
        }
        #endregion

        #region Undo
        public virtual bool CanUndo => History.CanUndo;
        public virtual bool CanRedo => History.CanRedo;

        public virtual void Undo()
        {
            if (!History.Undo())
            {
                UnityEngine.Debug.LogWarning($"{GetType().Name} was asked to undo, " +
                    $"but it has nothing to undo. Check CanUndo first.");
            }
        }

        public virtual void Redo()
        {
            if (!History.Redo())
            {
                UnityEngine.Debug.LogWarning($"{GetType().Name} was asked to redo, " +
                    $"but it has nothing to redo. Check CanRedo first.");
            }
        }

        public virtual void CommitPendingEdits()
        {
            (Subwindow as IEditReportingSubwindow)?.CommitPendingEdits();
        }
        #endregion

        #region Edits
        /// <summary>
        /// Called when the subwindow reports an edit the user finished making. By
        /// default, records the command that CreateEditCommand makes for it, if any.
        /// </summary>
        protected virtual void OnSubwindowEdit(FieldEdit edit)
        {
            IUndoableCommand command = CreateEditCommand(edit);
            if (command != null)
            {
                History.Record(command);
            }
        }

        public virtual IUndoableCommand CreateEditCommand(FieldEdit edit)
        {
            return null;
        }
        #endregion

        public virtual void Load(object loadResult)
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

        public virtual void Dispose()
        {
            SetSubs(false);
            History.Changed -= OnHistoryChanged;
            History.Clear();
        }
    }

    /// <summary>
    /// For entries with no state of their own to keep track of.
    /// </summary>
    public class DefaultControlPanelEntryLogic : ControlPanelEntryLogic
    {
        protected override void AdoptState(object loadResult)
        {
            // No state to adopt
        }
    }
}

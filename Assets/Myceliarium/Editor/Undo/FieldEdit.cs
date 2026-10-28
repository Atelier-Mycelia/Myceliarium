using System;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// An edit the user already made to a field in a subwindow, as reported by one of
    /// its field edit trackers. The subwindow only reports these; the entry owns the
    /// state, so it decides how to apply and revert them (see ToCommand).
    /// </summary>
    public sealed class FieldEdit
    {
        public FieldEdit(VisualElement field, object context, object oldValue, object newValue)
        {
            Field = field;
            Context = context;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public VisualElement Field { get; }

        /// <summary>
        /// Whatever the subwindow said was being edited when the user started editing
        /// (see ControlPanelSubwindow.GetEditContext), so undoing still reaches the right
        /// object even if the selection has since changed.
        /// </summary>
        public object Context { get; }

        public object OldValue { get; }
        public object NewValue { get; }

        /// <summary>
        /// Builds a command out of this edit. <paramref name="apply"/> is given this edit
        /// and the value to put in place (the new one when redoing, the old one when
        /// undoing). It should update the state and refresh the UI without raising
        /// change events, so applying doesn't itself get recorded as an edit.
        /// </summary>
        public IUndoableCommand ToCommand(Action<FieldEdit, object> apply)
        {
            if (apply == null)
            {
                throw new ArgumentNullException(nameof(apply));
            }
            return new FieldEditCommand(this, apply);
        }
    }

    internal sealed class FieldEditCommand : IUndoableCommand
    {
        public FieldEditCommand(FieldEdit edit, Action<FieldEdit, object> apply)
        {
            _edit = edit;
            _apply = apply;
        }

        private readonly FieldEdit _edit;
        private readonly Action<FieldEdit, object> _apply;

        public void Execute() => _apply(_edit, _edit.NewValue);
        public void Undo() => _apply(_edit, _edit.OldValue);
    }

    /// <summary>
    /// Implemented by subwindows that report the user's edits, so their entry can
    /// make them undoable. ControlPanelSubwindow implements this.
    /// </summary>
    public interface IEditReportingSubwindow
    {
        event Action<FieldEdit> EditCommitted;

        /// <summary>
        /// Reports any edit still in progress (say, text typed into a field that
        /// hasn't lost focus yet). Does nothing if there isn't one.
        /// </summary>
        void CommitPendingEdits();
    }
}

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// A submodule of ControlPanelSubwindow that watches one kind of control for user
    /// edits and reports them. One class per control type (text, integer, ...) keeps
    /// the subwindow itself from growing with every control that becomes undoable.
    /// </summary>
    public interface IFieldEditTracker : IDisposable
    {
        /// <param name="root">What to watch; the subwindow's root.</param>
        /// <param name="contextResolver">Gives the object a field is editing, asked when
        /// an edit starts.</param>
        /// <param name="report">Called once for each finished edit.</param>
        void Attach(VisualElement root, Func<VisualElement, object> contextResolver,
            Action<FieldEdit> report);

        /// <summary>
        /// Finishes (and reports) the edit in progress, if there is one.
        /// </summary>
        void CommitPending();
    }

    /// <summary>
    /// For controls the user types into: everything entered between the control gaining
    /// and losing focus is one edit, so a typing session is one undo step instead of one
    /// per keystroke. Subclasses just name the control and value types; see
    /// TextFieldEditTracker.
    ///
    /// Focus events bubble, so a single callback on the root also covers fields that
    /// are created later (list items, for example).
    /// </summary>
    public abstract class FocusSessionEditTracker<TField, TValue> : IFieldEditTracker
        where TField : BaseField<TValue>
    {
        public void Attach(VisualElement root, Func<VisualElement, object> contextResolver,
            Action<FieldEdit> report)
        {
            Detach();
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _contextResolver = contextResolver;
            _report = report ?? throw new ArgumentNullException(nameof(report));
            _root.RegisterCallback<FocusInEvent>(OnFocusIn);
            _root.RegisterCallback<FocusOutEvent>(OnFocusOut);
        }

        private VisualElement _root;
        private Func<VisualElement, object> _contextResolver;
        private Action<FieldEdit> _report;

        private TField _field;
        private object _context;
        private TValue _originalValue;

        private void OnFocusIn(FocusInEvent evt)
        {
            var field = FindField(evt.target as VisualElement);
            if (field != null)
            {
                BeginEdit(field);
            }
        }

        private TField FindField(VisualElement from)
        {
            // Traveling up from the tree until we find the right field type.
            for (var visElem = from; visElem != null && visElem != _root; visElem = visElem.parent)
            {
                if (visElem is TField match)
                {
                    return match;
                }
            }
            return null;
        }

        /// <summary>
        /// Starts watching the field, remembering its current value as what an undo goes
        /// back to. If another edit is still pending, that one is finished first.
        /// </summary>
        internal void BeginEdit(TField field)
        {
            if (field == _field)
            {
                _endPending = false; // Got focus back before the edit was finished
                return;
            }

            CommitPending();
            _field = field;
            _originalValue = field.value;
            _context = _contextResolver?.Invoke(field);
        }

        public void CommitPending()
        {
            if (_field == null)
            {
                return;
            }

            var field = _field;
            var context = _context;
            TValue original = _originalValue;
            ClearEdit();

            TValue current = field.value;
            if (!EqualityComparer<TValue>.Default.Equals(current, original))
            {
                var edit = new FieldEdit(field, context, original, current);
                _report(edit);
            }
        }

        private void OnFocusOut(FocusOutEvent evt)
        {
            var field = FindField(evt.target as VisualElement);
            if (field == null || field != _field)
            {
                return;
            }

            // Focus moving to another part of the same field isn't the edit ending
            var next = FindField(evt.relatedTarget as VisualElement);
            if (next == field)
            {
                return;
            }

            // Fields that commit their value when they lose focus (a delayed text field,
            // say) do it in their own default action for this event, and those run only
            // after all the callbacks, this one included. So the value isn't final yet.
            // The edit gets finished on the next scheduler tick instead, or sooner if
            // something asks for it (CommitPending, or another edit beginning), which
            // would be after this event is done with.
            _endPending = true;
            _root.schedule.Execute(CommitIfEndPending);
        }

        private void CommitIfEndPending()
        {
            if (_endPending)
            {
                CommitPending();
            }
        }

        // Set when the field lost focus but the edit hasn't been finished yet
        private bool _endPending;

        private void ClearEdit()
        {
            _endPending = false;
            _field = null;
            _context = null;
            _originalValue = default;
        }

        private void Detach()
        {
            if (_root == null)
            {
                return;
            }

            _root.UnregisterCallback<FocusInEvent>(OnFocusIn);
            _root.UnregisterCallback<FocusOutEvent>(OnFocusOut);
            _root = null;
        }

        public void Dispose()
        {
            Detach();
            ClearEdit();
        }
    }
}

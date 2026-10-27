using System;
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
    /// Treats everything typed into a TextField between gaining and losing focus as one
    /// edit, so a typing session is one undo step instead of one per keystroke.
    ///
    /// Focus events bubble, so a single callback on the root also covers fields that
    /// are created later (list items, for example).
    /// </summary>
    public sealed class TextFieldEditTracker : IFieldEditTracker
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

        private TextField _field;
        private object _context;
        private string _originalValue;

        private void OnFocusIn(FocusInEvent evt)
        {
            var field = FindField(evt.target as VisualElement);
            if (field != null)
            {
                BeginEdit(field);
            }
        }

        private TextField FindField(VisualElement from)
        {
            for (var visElem = from; visElem != null && visElem != _root; visElem = visElem.parent)
            {
                if (visElem is TextField textField)
                {
                    return textField;
                }
            }
            return null;
        }

        /// <summary>
        /// Starts watching the field, remembering its current value as what an undo goes
        /// back to. If another edit is still pending, that one is finished first.
        /// </summary>
        internal void BeginEdit(TextField field)
        {
            if (field == _field)
            {
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
            string original = _originalValue;
            _field = null;
            _context = null;
            _originalValue = null;

            string current = field.value;
            if (current != original)
            {
                _report(new FieldEdit(field, context, original, current));
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

            CommitPending();
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
            _field = null;
            _context = null;
            _originalValue = null;
        }
    }
}

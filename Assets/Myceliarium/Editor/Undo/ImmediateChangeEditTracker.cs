using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// For controls where each user action is a finished edit (picking an object, ticking a
    /// toggle, choosing from a dropdown): there's no session to group, so each one is
    /// reported as soon as the field's ChangeEvent arrives. Subclasses just name the
    /// control and value types; see ObjectFieldEditTracker.
    ///
    /// ChangeEvents are only raised for changes that notify, so applying an undo with
    /// SetValueWithoutNotify isn't picked up as a new edit. Setting a field's value
    /// directly from code (which does notify) is, though, so code that updates a field
    /// on the user's behalf should use SetValueWithoutNotify.
    ///
    /// Change events bubble, so a single callback on the root covers every such field.
    /// </summary>
    public abstract class ImmediateChangeEditTracker<TField, TValue> : IFieldEditTracker
        where TField : BaseField<TValue>
    {
        public void Attach(VisualElement root, Func<VisualElement, object> contextResolver,
            Action<FieldEdit> report)
        {
            Detach();
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _contextResolver = contextResolver;
            _report = report ?? throw new ArgumentNullException(nameof(report));
            _root.RegisterCallback<ChangeEvent<TValue>>(OnChanged);
        }

        private VisualElement _root;
        private Func<VisualElement, object> _contextResolver;
        private Action<FieldEdit> _report;

        private void OnChanged(ChangeEvent<TValue> evt)
        {
            if (evt.target is TField field)
            {
                ReportChange(field, evt.previousValue, evt.newValue);
            }
        }

        internal void ReportChange(TField field, TValue previous, TValue current)
        {
            if (EqualityComparer<TValue>.Default.Equals(previous, current))
            {
                return;
            }

            object context = _contextResolver?.Invoke(field);
            _report(new FieldEdit(field, context, previous, current));
        }

        /// <summary>
        /// Nothing to do: there's never an edit in progress.
        /// </summary>
        public void CommitPending()
        {
        }

        private void Detach()
        {
            if (_root == null)
            {
                return;
            }

            _root.UnregisterCallback<ChangeEvent<TValue>>(OnChanged);
            _root = null;
        }

        public void Dispose()
        {
            Detach();
        }
    }
}

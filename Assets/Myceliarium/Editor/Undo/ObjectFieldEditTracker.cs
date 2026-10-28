using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Reports edits to ObjectFields. Unlike the typing controls there's no session to
    /// group: each pick is a finished edit, so it's reported as soon as the field's
    /// ChangeEvent arrives (which only the user's picks raise, not SetValueWithoutNotify).
    /// FieldEdit's old and new values are the objects (null when cleared).
    ///
    /// Change events bubble, so a single callback on the root covers every ObjectField.
    /// </summary>
    public sealed class ObjectFieldEditTracker : IFieldEditTracker
    {
        public void Attach(VisualElement root, Func<VisualElement, object> contextResolver,
            Action<FieldEdit> report)
        {
            Detach();
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _contextResolver = contextResolver;
            _report = report ?? throw new ArgumentNullException(nameof(report));
            _root.RegisterCallback<ChangeEvent<UnityObject>>(OnChanged);
        }

        private VisualElement _root;
        private Func<VisualElement, object> _contextResolver;
        private Action<FieldEdit> _report;

        private void OnChanged(ChangeEvent<UnityObject> evt)
        {
            if (evt.target is ObjectField field)
            {
                ReportChange(field, evt.previousValue, evt.newValue);
            }
        }

        internal void ReportChange(ObjectField field, UnityObject previous, UnityObject current)
        {
            if (previous == current)
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

            _root.UnregisterCallback<ChangeEvent<UnityObject>>(OnChanged);
            _root = null;
        }

        public void Dispose()
        {
            Detach();
        }
    }
}

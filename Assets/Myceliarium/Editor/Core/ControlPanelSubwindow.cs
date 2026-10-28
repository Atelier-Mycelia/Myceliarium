using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium
{
    public abstract class ControlPanelSubwindow : VisualElement, IControlPanelSubwindow,
        IEditReportingSubwindow, IDisposable
    {
        public virtual void Init()
        {
            if (_isInitted)
            {
                return;
            }

            LoadUxml();
            RegisterVisualElements();
            ConfigureVisualElements();
            SetSubs(false);
            SetSubs(true);
            AttachEditTrackers();
            _isInitted = true;
            _isDisposed = false;
        }

        public VisualElement Root
        {
            get => this;
        }

        protected bool _isInitted;

        public virtual void Show()
        {
            style.display = DisplayStyle.Flex;
        }

        public virtual void Hide()
        {
            style.display = DisplayStyle.None;
        }

        protected virtual void LoadUxml()
        {
            var vta = Resources.Load<VisualTreeAsset>(PathToUxml);
            if (vta == null)
            {
                string logMessage = $"Failed to load subwindow UXML at {PathToUxml} " +
                    $"for {GetType().Name}. Ensure the UXML file is placed in a " +
                    $"Resources folder and the path is correct.";
                throw new InvalidOperationException(logMessage);
            }

            vta.CloneTree(this);
        }

        public abstract string PathToUxml { get; }

        #region Registration and Binding of Visual Elements
        // Some of these by default do nothing. Subclasses are expected
        // to override them as appropriate.
        protected abstract void RegisterVisualElements();

        /// <summary>
        /// For when you want to change the visual elements' properties right after
        /// registration (for example: setting the objectType of an ObjectField).
        /// This is NOT for setting or removing callbacks. That's SetSubs's job.
        /// </summary>
        protected abstract void ConfigureVisualElements();

        protected bool _isDisposed;

        public virtual void Bind()
        {
            // Default: nothing. Subclasses override.
        }

        public virtual void Unbind()
        {
            // Default: nothing. Subclasses override.
        }

        /// <summary>
        /// For when you want to add or remove callbacks (usually related
        /// to this subwindow's registered VisualElements).
        /// </summary>
        protected virtual void SetSubs(bool wantsSubsActive)
        {
            // Say it with me: default: nothing! Subclasses override!
        } 
        #endregion

        #region Edit Tracking (for undo/redo)
        /// <summary>
        /// Raised when the user finishes an edit to one of the controls the trackers
        /// cover. The entry listens to this to make the edit undoable.
        /// </summary>
        public event Action<FieldEdit> EditCommitted = delegate { };

        /// <summary>
        /// Which control types get their edits reported. The default covers text
        /// fields; override to add trackers for other types (call the base to keep
        /// the default ones). Called once, when this is initialized.
        /// </summary>
        protected virtual IEnumerable<IFieldEditTracker> CreateEditTrackers()
        {
            yield return new TextFieldEditTracker();
            yield return new IntegerFieldEditTracker();
            yield return new LongFieldEditTracker();
            yield return new FloatFieldEditTracker();
            yield return new DoubleFieldEditTracker();
            yield return new ObjectFieldEditTracker();
            yield return new ToggleEditTracker();
            yield return new EnumFieldEditTracker();
            yield return new DropdownFieldEditTracker();
        }

        /// <summary>
        /// What the given control is editing, asked when the user starts editing it
        /// and handed back in the resulting FieldEdit.Context. Override to return, say,
        /// the item currently shown. Null by default.
        /// </summary>
        protected virtual object GetEditContext(VisualElement field)
        {
            return null;
        }

        public void CommitPendingEdits()
        {
            for (int i = 0; i < _editTrackers.Count; i++)
            {
                _editTrackers[i].CommitPending();
            }
        }

        private void AttachEditTrackers()
        {
            DisposeEditTrackers();
            _editTrackers.AddRange(CreateEditTrackers());
            
            for (int i = 0; i < _editTrackers.Count; i++)
            {
                var tracker = _editTrackers[i];
                tracker.Attach(this, GetEditContext, RaiseEditCommitted);
            }
        }

        private void RaiseEditCommitted(FieldEdit edit)
        {
            EditCommitted(edit);
        }

        private void DisposeEditTrackers()
        {
            for (int i = 0; i < _editTrackers.Count; i++)
            {
                _editTrackers[i].Dispose();
            }
            _editTrackers.Clear();
        }

        private readonly List<IFieldEditTracker> _editTrackers = new List<IFieldEditTracker>();
        #endregion

        public virtual void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            DisposeEditTrackers();
            Unbind();
            RemoveFromHierarchy();
            _isDisposed = true;
        }

        public virtual bool IsVisible => style.display == DisplayStyle.Flex;

        /// <summary>
        /// No-op by default. Subclasses can override to implement refreshing logic.
        /// </summary>
        public virtual void Refresh()
        {

        }
    }

    public interface IControlPanelSubwindow : IRefreshable
    {
        VisualElement Root { get; }

        void Init();
        void Bind();
        void Unbind();
        void Dispose();
        void Show();
        void Hide();
        /// <summary>
        /// Relative to Resources.
        /// </summary>
        string PathToUxml { get; }
        bool IsVisible { get; }
    }

}
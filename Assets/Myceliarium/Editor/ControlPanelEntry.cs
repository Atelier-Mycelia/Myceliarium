using System;
using System.Collections.Generic;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// These handle the logic for their own entries in the appropriate Control 
    /// Panel, represented by a tab on the left sidebar and its associated 
    /// subwindow in the appropriate holder.
    /// 
    /// These should be automatically found through reflection and added to the 
    /// CP when appropriate.
    /// </summary>
    public abstract class ControlPanelEntry : IControlPanelEntry, IDisposable
    {
        public virtual bool IsTopLevel => false;
        // ^Why false as the default? We expect that most entries will be
        // nested under others.
        public abstract string MainDisplayName { get; }

        public virtual void Init(bool forceReinit = false)
        {
            if (forceReinit)
            {
                ResetState();
            }

            if (forceReinit || !_isInitted)
            {
                _isDisposed = false;
                PrepareLeftSidebarTab();
                PrepareSubentries();
                PrepareSubwindow();
                ToggleSubs(true);
                _isInitted = true;
            }
        }

        public virtual bool IsInitted
        {
            get => _isInitted;
            protected set => _isInitted = value;
        }
        private bool _isInitted, _isDisposed;

        private void ResetState()
        {
            if (_tab != null)
            {
                ToggleSubs(false);
                _tab = null;
            }

            _subentries.Clear();
            _subwindow?.RemoveFromHierarchy();
            _subwindow = null;
            _isInitted = _isDisposed = false;
        }

        protected abstract void PrepareLeftSidebarTab();

        protected IControlPanelTab _tab;


        // Expected for subclasses to override this method if they have subentries.
        // The default implementation does nothing.
        protected virtual void PrepareSubentries() { }
        public virtual IReadOnlyList<IControlPanelEntry> GetSubentries(bool recursive = false)
        {
            List<IControlPanelEntry> result;

            if (recursive)
            {
                result = new List<IControlPanelEntry>(_subentries);
                for (int i = 0; i < _subentries.Count; i++)
                {
                    var directChild = _subentries[i];
                    if (directChild == null)
                    {
                        string logMessage = $"Subentry at index {i} of {GetType().Name} was null. " +
                            $"This should not happen if PrepareSubentries() has been called.";
                        throw new InvalidOperationException(logMessage);
                    }

                    // This is a depth-first traversal of the subentry tree.
                    // Note that this will include the direct child itself in the result, so
                    // we don't need to add it separately.
                    // This is because GetSubentries(true) will return a list that includes
                    // the entry itself as well as its subentries.
                    var childSubs = directChild.GetSubentries(true);
                    result.AddRange(childSubs);
                }
            }
            else
            {
                result = _subentries; // So we won't need as many allocations
            }

            return result;
        }
        protected readonly List<IControlPanelEntry> _subentries = new List<IControlPanelEntry>();

        protected virtual void PrepareSubwindow() { }

        public virtual bool IsMeantToHaveSubwindow => true; 
        // ^Most tabs are expected to have subwindows, so...

        protected IControlPanelSubwindow _subwindow;

        protected virtual void ToggleSubs(bool on)
        {
            if (on)
            {
                _tab.Clicked += OnTabClicked;
            }
            else
            {
                _tab.Clicked -= OnTabClicked;
            }
        }

        private void OnTabClicked(IControlPanelTab tabClicked)
        {
            ControlPanelSignals.OnEntryTabClicked(this);
        }

        /// <summary>
        /// Meant to be overridden by subclasses that have state that needs
        /// to be stringified for whatever purposes.
        /// </summary>
        public abstract string StringifiedState { get; }

        public virtual void Apply(string stringifiedState, out bool success)
        {
            success = false;
        }

        // Clients shouldn't even try to access the Subwindow or tab before
        // the Init call, hence why the getters here throw exceptions if the
        // subwindow or tab button is null. This is to help catch bugs.
        public virtual IControlPanelTab Tab
        {
            get
            {
                return _tab;
            }
            protected set => _tab = value;
        }

        public virtual IControlPanelSubwindow Subwindow
        {
            get
            {
                if (IsMeantToHaveSubwindow && _subwindow == null)
                {
                    string logMessage = $"Subwindow for {GetType().Name} was null. " +
                        $"This should not happen if Init() has been called.";
                    throw new InvalidOperationException(logMessage);
                }
                return _subwindow;
            }
            protected set
            {
                if (!IsMeantToHaveSubwindow)
                {
                    string logMessage = $"Attempted to set Subwindow for {GetType().Name}, " +
                        $"but this entry is not meant to have one.";
                    throw new InvalidOperationException(logMessage);
                }
                _subwindow = value;
            }
        }

        public virtual void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            ToggleSubs(false);

            // We don't want to null out the VisualElements here, given how each
            // entry is expected to persist even when the Control Panel window is
            // closed. We'll merely unattach the tabs and subwindows from the
            // hierarchy, and let the Control Panel window handle the rest.
            RemoveFromHierarchy();
            _isDisposed = true;
        }
        
        public virtual void RemoveFromHierarchy()
        {
            _tab?.RemoveFromHierarchy();
            _subwindow?.RemoveFromHierarchy();
        }

        public virtual void OnSelected()
        {
            // Default = no-op
        }

        public virtual void OnDeselected()
        {
            // Default = no-op
        }

        public virtual bool HasSubentries => _subentries.Count > 0;

    }

    public interface IControlPanelEntry
    {
        /// <summary>
        /// Functions as the constructor for this entry. Should be called once when the 
        /// entry is first created, and can be called again if the entry needs to 
        /// be reinitialized.
        /// </summary>
        void Init(bool forceReinit = false);

        /// <summary>
        /// The display name of this entry in English. This is used to help keep things
        /// consistently sorted in the tab sidebar on the left side of the Control Panel.
        /// </summary>
        string MainDisplayName { get; }

        IControlPanelTab Tab { get; }
        IControlPanelSubwindow Subwindow { get; }

        /// <summary>
        /// Some entries may have state that needs to be stringified for saving/loading purposes.
        /// This method returns a string representation of the entry's state.
        /// </summary>
        /// <returns></returns>
        string StringifiedState { get; }

        void Apply(string stringifiedState, out bool success);

        bool IsTopLevel { get; }

        IReadOnlyList<IControlPanelEntry> GetSubentries(bool recursive = false);
        bool IsMeantToHaveSubwindow { get; }
        bool IsInitted { get; }
        void RemoveFromHierarchy();

        void OnSelected();
        void OnDeselected();
        bool HasSubentries { get; }

    }

    public interface IAtMyceliaControlPanelEntry : IControlPanelEntry
    {
        // This interface can be used to mark entries that are specific
        // to the Atelycelia ecosys. This is to avoid needing to use magic
        // strings when filtering entries to attach and whatnot.
    }
}
using System;
using System.Collections;
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
        public virtual bool IsTestOnly => false;

        /// <summary>
        /// Affects how this is sorted in the Control Panel's left sidebar.
        /// Lower numbers are sorted higher.
        /// </summary>
        public virtual int SortingOrder => 100;

        public virtual bool IsTopLevel => false;
        // ^Why false as the default? We expect that most entries will be
        // nested under others.
        public abstract string SortingName { get; }

        public virtual bool ShouldPreloadOnInit => false; // opt-in, default false

        public virtual void Init(bool forceReinit = false)
        {
            if (forceReinit)
            {
                ResetState();
            }
            else if (_isDisposed)
            {
                return;
            }

            if (forceReinit || !_isInitted)
            {
                PrepareLeftSidebarTab();
                PrepareSubentries();
                PrepareSubwindow();
                PrepareLoader();
                SetSubs(false); // Taking reinitting into account
                SetSubs(true);
                _subwindow?.Hide();
                if (ShouldPreloadOnInit)
                {
                    HandleLoading();
                }
                SetCurrentStateAsInit();
                _isInitted = true;
            }
        }

        private bool _isDisposed;

        public virtual bool IsInitted
        {
            get => _isInitted;
            protected set => _isInitted = value;
        }
        private bool _isInitted;

        private void ResetState()
        {
            Dispose();
            _isDisposed = false;
            _isInitted = false;
        }

        public virtual void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            SetSubs(false);

            DisposeSubentries();
            void DisposeSubentries()
            {
                for (int i = 0; i < _subentries.Count; i++)
                {
                    _subentries[i].Dispose();
                }
                _subentries.Clear();
            }

            _tab.Dispose();
            _subwindow?.Dispose();
            _tab = null;
            _subwindow = null;

            RemoveFromHierarchy();
            _isDisposed = true;
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

        /// <summary>
        /// Initializes the loader for the control panel entry. The base implementation goes
        /// with the default loader, which does nothing but trigger the callback. Subclasses
        /// can override this to provide a custom loader if needed.
        /// </summary>
        protected virtual void PrepareLoader()
        {
            Loader = new DefaultControlPanelEntryLoader();
        }

        protected IControlPanelEntryLoader Loader { get; set; }

        /// <summary>
        /// For entries that have state that the user is meant to be able to edit. 
        /// This is to help decide when to show a "Save" button, and when to warn
        /// the user about unsaved changes.
        /// </summary>
        protected virtual void SetCurrentStateAsInit()
        {
            HasUnsavedChanges = false;
        }

        public virtual bool IsMeantToHaveSubwindow => true; 
        // ^Most tabs are expected to have subwindows, so...

        protected IControlPanelSubwindow _subwindow;

        protected virtual void SetSubs(bool on)
        {
            if (on)
            {
                ControlPanelSignals.ControlPanelOpened += OnControlPanelOpened;
                if (_tab != null)
                {
                    _tab.Clicked += OnTabClicked;
                }
            }
            else
            {
                ControlPanelSignals.ControlPanelOpened -= OnControlPanelOpened;
                if (_tab != null)
                {
                    _tab.Clicked -= OnTabClicked;
                }
            }
        }

        protected virtual void OnControlPanelOpened(IControlPanel panel)
        {
            // Default implementation does nothing. Subclasses can override this if they need to respond to
            // the Control Panel being opened.
        }


        protected void OnTabClicked(IControlPanelTab tabClicked)
        {
            ControlPanelSignals.EntryTabClicked(this);
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

        public virtual void RemoveFromHierarchy()
        {
            _tab?.Root.RemoveFromHierarchy();
            _subwindow?.Root.RemoveFromHierarchy();
        }

        public virtual void Select()
        {
            if (_tab == null || _tab.IsSelected)
            {
                return;
            }

            if (this.IsMeantToHaveSubwindow)
            {
                #region Deselect Subentries
                // So their subwindows don't get in the way of ours
                for (int i = 0; i < _subentries.Count; i++)
                {
                    var subentry = _subentries[i];
                    subentry.Deselect();
                }
                #endregion
            }

            _tab.IsSelected = true;
            HandleLoading();
        }

        /// <summary>
        /// Meant to be overridden by subclasses that need to do some loading 
        /// before their subwindow is shown. When overriding ControlPanelEntry's
        /// directly, best NOT call the base implementation.
        /// </summary>
        protected virtual void HandleLoading()
        {
            if (Loader != null)
            {
                Loader.Load(this, OnLoadingDone);
            }
            else
            {
                OnLoadingDone();
            }
        }

        protected object _lastLoadResult;

        /// <summary>
        /// If your entry cares about the load result, this is where it's expected to start
        /// working with it.
        /// </summary>
        protected virtual void OnLoadingDone()
        {
            _subwindow?.Refresh();
            _subwindow?.Show();
            SetCurrentStateAsInit();
        }

        public virtual void Deselect()
        {
            if (_tab == null || !_tab.IsSelected)
            {
                return;
            }

            _subwindow?.Hide();
            _tab.IsSelected = false;
        }

        public virtual bool HasSubentries => _subentries.Count > 0;
        public virtual bool HasUnsavedChanges { get; protected set; }

    }

    public interface IControlPanelEntry : IDisposable
    {
        /// <summary>
        /// Functions as the constructor for this entry. Should be called once when the 
        /// entry is first created, and can be called again if the entry needs to 
        /// be reinitialized (say, after being Disposed).
        /// </summary>
        void Init(bool forceReinit = false);
        bool HasUnsavedChanges { get; }

        /// <summary>
        /// Decides how this entry is sorted in the Control Panel's left sidebar.
        /// Lower numbers are sorted higher. When two entries have the same sorting order,
        /// they are sorted alphabetically by their SortingName.
        /// </summary>
        int SortingOrder { get; }
        /// <summary>
        /// When two ControlPanelEntries have the same SortingOrder, they are then sorted
        /// based on this. Alphabetically.
        /// </summary>
        string SortingName { get; }

        IControlPanelTab Tab { get; }

        bool IsMeantToHaveSubwindow { get; }
        IControlPanelSubwindow Subwindow { get; }

        bool IsTopLevel { get; }

        bool HasSubentries { get; }
        IReadOnlyList<IControlPanelEntry> GetSubentries(bool recursive = false);

        void RemoveFromHierarchy();

        void Select();
        void Deselect();

        bool IsTestOnly { get; }
    }

    public interface IAtMyceliaControlPanelEntry : IControlPanelEntry
    {
        // This interface can be used to mark entries that are specific
        // to the Atelycelia ecosys. This is to avoid needing to use magic
        // strings when filtering entries to attach and whatnot.
    }
}
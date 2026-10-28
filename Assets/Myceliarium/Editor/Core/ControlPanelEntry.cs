using System;
using System.Collections.Generic;
using ModuleFactory = AtMycelia.Myceliarium.ControlPanelModuleFactory;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// These handle the logic for their own entries in the appropriate Control
    /// Panel, represented by a tab on the sidebar and its associated
    /// subwindow in the appropriate holder.
    ///
    /// An entry is an orchestrator: it creates its modules (tab, subwindow, loader and
    /// logic, from the types its subclass names), wires them together and drives the
    /// load/select/deselect flow. The working state, undo history and unsaved changes
    /// belong to its logic module.
    ///
    /// These should be automatically found through reflection and added to the
    /// CP when appropriate.
    /// </summary>
    public abstract class ControlPanelEntry : IControlPanelEntry, IEntryLogicOwner, IDisposable
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

        #region Overridable submodules
        protected abstract Type SidebarTabType { get; } // Every entry needs its own tab.
        protected virtual Type SubwindowType => null;
        protected virtual Type LoaderType => typeof(DefaultControlPanelEntryLoader);
        protected virtual Type LogicType => typeof(DefaultControlPanelEntryLogic);
        #endregion

        #region Lifecycle
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
                PrepareMainSidebarTab();
                _subentries.Prepare(CreateSubentries, Tab);
                PrepareSubwindow();
                PrepareLoader();
                PrepareLogic(); // After the others, as it talks to them
                _subwindow?.Hide();
                if (ShouldPreloadOnInit)
                {
                    HandleLoading();
                }
                Logic.MarkSaved();
                SetSubs(false); // Taking reinitting into account
                SetSubs(true);
                _isInitted = true;
            }
        }

        protected bool _isDisposed;

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

            // Before the tab and subwindow, since it unsubscribes from the latter
            Logic?.Dispose();
            Logic = null; // Its state is rebuilt (and its history dropped) by a reinit

            // The subentry instances themselves are kept, so that reinitializing this
            // entry (Dispose, then Init with forceReinit) brings back the same objects
            // rather than replacing them.
            _subentries.DisposeAll();

            _tab?.Dispose();
            _subwindow?.Dispose();
            _tab = null;
            _subwindow = null;

            RemoveFromHierarchy();
            _isDisposed = true;
        }
        #endregion

        #region Modules
        // Each module is made the same way: one instance of the type that the subclass
        // names, which gets its Init call if it came out valid.

        protected virtual void PrepareMainSidebarTab()
        {
            _tab = ModuleFactory.Create<IControlPanelTab>(SidebarTabType, "tab");
            _tab?.Init();
        }

        protected IControlPanelTab _tab;

        // Virtual because not all entries have subwindows tied to them. Such subwindowless
        // ones are usually those meant to have other entries nested under them.
        protected virtual void PrepareSubwindow()
        {
            if (SubwindowType == null)
            {
                return;
            }

            _subwindow = ModuleFactory.Create<IControlPanelSubwindow>(SubwindowType,
            "subwindow");
            _subwindow?.Init();
        }

        protected IControlPanelSubwindow _subwindow;

        /// <summary>
        /// Initializes this instance's loader. The base implementation goes
        /// with the default one, which does nothing but trigger the onLoadComplete
        /// callback. Subclasses can override this to provide a custom loader if needed.
        /// </summary>
        protected virtual void PrepareLoader()
        {
            Loader = ModuleFactory.Create<IControlPanelEntryLoader>(
                LoaderType, "loader");
        }

        public IControlPanelEntryLoader Loader { get; protected set; }

        /// <summary>
        /// Creates the logic module: the part that owns the working state the user
        /// edits, its undo history and its unsaved changes flag. Every entry has one;
        /// if the one named by LogicType can't be made, the default (which has no state)
        /// takes its place. Gets called on every (re)initialization, so a reinit starts
        /// with fresh state.
        /// </summary>
        protected virtual void PrepareLogic()
        {
            Logic = ModuleFactory.Create<IControlPanelEntryLogic>(LogicType, "logic")
                ?? new DefaultControlPanelEntryLogic();
            Logic.Init(this);
        }

        public IControlPanelEntryLogic Logic { get; protected set; }
        
        #endregion

        #region Subentries
        /// <summary>
        /// Override this to give the entry subentries. It's called only once: the first
        /// time the entry is initialized. The instances it returns are kept for the
        /// entry's lifetime, meaning that reinitializing the entry reinitializes
        /// those same instances instead of creating new ones. The default has none.
        /// </summary>
        protected virtual IEnumerable<IControlPanelEntry> CreateSubentries()
        {
            return Array.Empty<IControlPanelEntry>();
        }

        public virtual IReadOnlyList<IControlPanelEntry> GetSubentries(bool recursive = false)
        {
            return _subentries.Get(recursive, GetType().Name);
        }

        public virtual bool HasSubentries => _subentries.Any;
        private readonly SubentryGroup _subentries = new SubentryGroup();
        #endregion

        public virtual bool IsMeantToHaveSubwindow => true;
        // ^Most tabs are expected to have subwindows, so...

        /// <summary>
        /// For the navigation side of things: the panel opening and the tab being
        /// clicked. What the logic needs to react to is its own business.
        /// </summary>
        protected virtual void SetSubs(bool on)
        {
            if (on)
            {
                ControlPanelSignals.ControlPanelOpened += OnControlPanelOpened;
                // The tab is null if this was never initialized (or already disposed), in
                // which case there's nothing for it to (un)subscribe from.
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

        public IControlPanelSubwindow SubwindowIfAny => _subwindow;

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

        #region Selection and Loading
        public virtual void Select()
        {
            if (_tab == null || _tab.IsSelected)
            {
                return;
            }

            if (this.IsMeantToHaveSubwindow)
            {
                // So their subwindows don't get in the way of ours
                _subentries.DeselectAll();
            }

            _tab.IsSelected = true;

            if (Logic.HasUnsavedChanges)
            {
                // Loading would replace the user's unsaved edits (and the history that
                // goes with them), so the subwindow is just shown as it was left.
                _subwindow?.Show();
                return;
            }

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

        /// <summary>
        /// Hands the loader's result to the logic (which marks it as the saved state),
        /// then shows the subwindow. Subclasses that need the subwindow to get something
        /// from the logic before it's shown can override this.
        /// </summary>
        protected virtual void OnLoadingDone()
        {
            if (Loader != null)
            {
                var loadResult = Loader.GetLoadResult<object>();
                Logic.Load(loadResult);
            }
            _subwindow?.Refresh();
            _subwindow?.Show();
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
        #endregion

        /// <summary>
        /// Whether the logic's state differs from the last loaded or saved one.
        /// </summary>
        public virtual bool HasUnsavedChanges => Logic != null && Logic.HasUnsavedChanges;
    }

    public interface IControlPanelEntry : IDisposable
    {
        /// <summary>
        /// Functions as the constructor for this entry. Should be called once when the
        /// entry is first created, and can be called again if the entry needs to
        /// be reinitialized (say, after being Disposed).
        /// </summary>
        void Init(bool forceReinit = false);

        /// <summary>
        /// Whether Init has run (and not been undone by a reinit in progress). Stays
        /// true after a Dispose, until the next forced Init.
        /// </summary>
        bool IsInitted { get; }

        bool HasUnsavedChanges { get; }

        /// <summary>
        /// Decides how this entry is sorted in the Control Panel's left sidebar.
        /// Lower numbers are sorted higher. When two entries have the same 
        /// sorting order, they are sorted alphabetically by their SortingName.
        /// </summary>
        int SortingOrder { get; }
        /// <summary>
        /// When two ControlPanelEntries have the same SortingOrder, they are then 
        /// sorted based on this. Alphabetically.
        /// </summary>
        string SortingName { get; }

        IControlPanelTab Tab { get; }

        bool IsMeantToHaveSubwindow { get; }
        IControlPanelSubwindow Subwindow { get; }
        IControlPanelEntryLoader Loader { get; }
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

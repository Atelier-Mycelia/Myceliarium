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
    public abstract class ControlPanelEntry : IControlPanelEntry, IUndoableEntry, IDisposable
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
                PrepareLogic();
                _subwindow?.Hide();
                if (ShouldPreloadOnInit)
                {
                    HandleLoading();
                }
                SetCurrentStateAsBase();
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
            UndoHistory.Clear(); // The state it refers to is about to be rebuilt
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

            // The subentry instances themselves are kept, so that reinitializing this
            // entry (Dispose, then Init with forceReinit) brings back the same objects
            // rather than replacing them.
            for (int i = 0; i < _subentries.Count; i++)
            {
                _subentries[i].Dispose();
            }

            _tab?.Dispose();
            _subwindow?.Dispose();
            _tab = null;
            _subwindow = null;
            Logic = null; // Its state is rebuilt (and its history dropped) by a reinit

            RemoveFromHierarchy();
            _isDisposed = true;
        }

        protected abstract void PrepareLeftSidebarTab();

        protected IControlPanelTab _tab;

        /// <summary>
        /// Creates the subentries if that hasn't happened yet, (re)initializes them, and
        /// registers their tabs under this entry's tab. Private on purpose: this is the
        /// only place subentries get added, which is what keeps their instances stable.
        /// See CreateSubentries.
        /// </summary>
        private void PrepareSubentries()
        {
            if (!_subentriesCreated)
            {
                _subentries.AddRange(CreateSubentries());
                _subentriesCreated = true;
            }

            for (int i = 0; i < _subentries.Count; i++)
            {
                var elem = _subentries[i];
                // Subentries that have been initialized before (and so maybe disposed
                // along with this entry) need the forced reinit to come back up.
                elem.Init(forceReinit: elem.IsInitted);
                if (elem.Tab != null)
                {
                    Tab.Register(elem.Tab);
                }
            }
        }

        private bool _subentriesCreated;

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
                            $"This should not happen if Init() has been called.";
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
        private readonly List<IControlPanelEntry> _subentries = new List<IControlPanelEntry>();

        // Virtual because not all entries have subwindows tied to them. Such subwindowless
        // ones are usually those meant to have other entries nested under them.
        protected virtual void PrepareSubwindow() { }

        /// <summary>
        /// Initializes this instance's loader. The base implementation goes
        /// with the default one, which does nothing but trigger the onLoadComplete 
        /// callback. Subclasses can override this to provide a custom loader if needed.
        /// </summary>
        protected virtual void PrepareLoader()
        {
            Loader = new DefaultControlPanelEntryLoader();
        }

        protected IControlPanelEntryLoader Loader { get; set; }

        /// <summary>
        /// Opt-in. Override to give the entry a logic module: the part that owns the
        /// working state the user edits, its undo history and its unsaved changes flag.
        /// Why have such a module? So that this class can stay an orchestrator. <br>
        /// This func gets called on every (re)initialization, so a reinit starts with
        /// fresh state. Entries that don't override this keep the older behavior
        /// (HasUnsavedChanges and so on handled by the entry itself).
        /// </summary>
        protected virtual void PrepareLogic() { }

        protected IControlPanelEntryLogic Logic { get; set; }

        /// <summary>
        /// For entries that have state that the user is meant to be able to edit. 
        /// This is to help decide when to warn the user about unsaved changes.
        /// </summary>
        protected virtual void SetCurrentStateAsBase()
        {
            HasUnsavedChanges = false;
        }

        public virtual bool IsMeantToHaveSubwindow => true; 
        // ^Most tabs are expected to have subwindows, so...

        protected IControlPanelSubwindow _subwindow;

        protected virtual void SetSubs(bool on)
        {
            // The tab is null if this was never initialized (or already disposed), in
            // which case there's nothing for it to (un)subscribe from.
            if (on)
            {
                ControlPanelSignals.ControlPanelOpened += OnControlPanelOpened;
                ControlPanelSignals.SaveCompleted += OnSaveCompleted;
                if (_tab != null)
                {
                    _tab.Clicked += OnTabClicked;
                }
                if (_subwindow is IEditReportingSubwindow reporter)
                {
                    reporter.EditCommitted += OnSubwindowEdit;
                }
                if (Logic != null)
                {
                    Logic.UnsavedChangesMayHaveChanged += NotifyUnsavedChangesChanged;
                }
            }
            else
            {
                ControlPanelSignals.ControlPanelOpened -= OnControlPanelOpened;
                ControlPanelSignals.SaveCompleted -= OnSaveCompleted;
                if (_tab != null)
                {
                    _tab.Clicked -= OnTabClicked;
                }
                if (_subwindow is IEditReportingSubwindow reporter)
                {
                    reporter.EditCommitted -= OnSubwindowEdit;
                }
                if (Logic != null)
                {
                    Logic.UnsavedChangesMayHaveChanged -= NotifyUnsavedChangesChanged;
                }
            }
        }

        protected virtual void OnControlPanelOpened(IControlPanel panel)
        {
            // Default implementation does nothing. Subclasses can override this if they need to respond to
            // the Control Panel being opened.
        }

        protected virtual void OnSaveCompleted(IControlPanelEntry entry)
        {
            if (entry == this)
            {
                Logic?.MarkSaved();
                SetCurrentStateAsBase();
                HasUnsavedChanges = false;
            }
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

            bool keepWorkingState = Logic != null && Logic.HasUnsavedChanges;
            if (keepWorkingState)
            {
                // Loading would replace the user's unsaved edits (and the history that
                // goes with them), so the subwindow is just shown as it was left.
                _subwindow?.Show();
                return;
            }

            if (Logic == null)
            {
                UndoHistory.Clear(); // Loading replaces the state the history refers to
            }
            HandleLoading();
        }

        #region Undo
        /// <summary>
        /// This entry's own undo/redo history. Ctrl+Z / Ctrl+Y while the Control Panel is
        /// focused act on the displayed entry's history only, never on Unity's global Undo.
        /// </summary>
        protected UndoStack UndoHistory => Logic?.History ?? _ownUndoHistory;
        private readonly UndoStack _ownUndoHistory = new UndoStack();

        /// <summary>
        /// Executes the command and records it in UndoHistory. Entries (or their
        /// subwindows, through the entry) should route undoable edits through this.
        /// </summary>
        protected void ExecuteUndoable(IUndoableCommand command)
        {
            UndoHistory.Push(command);
        }

        /// <summary>
        /// Like ExecuteUndoable, but for changes that have already happened (such as an
        /// edit the user made in the subwindow), so the command isn't executed.
        /// </summary>
        protected void RecordUndoable(IUndoableCommand command)
        {
            UndoHistory.Record(command);
        }

        /// <summary>
        /// Called when the subwindow reports an edit the user finished making. Does
        /// nothing by default; override to make it undoable, usually with
        /// RecordUndoable(edit.ToCommand(apply)). Needs the base SetSubs to have run.
        /// By default, hands the edit to the logic module (if there is one), which
        /// records the command it makes.
        /// </summary>
        protected virtual void OnSubwindowEdit(FieldEdit edit)
        {
            var command = Logic?.CreateEditCommand(edit);
            if (command != null)
            {
                RecordUndoable(command);
            }
        }

        public void CommitPendingEdits()
        {
            (_subwindow as IEditReportingSubwindow)?.CommitPendingEdits();
        }

        /// <summary>
        /// Entries with no state worth undoing can leave these as they are: the history
        /// stays empty, so CanUndo/CanRedo are false. Override all four to take over.
        /// </summary>
        public virtual bool CanUndo => UndoHistory.CanUndo;
        public virtual bool CanRedo => UndoHistory.CanRedo;

        public virtual void Undo()
        {
            if (!UndoHistory.Undo())
            {
                UnityEngine.Debug.LogWarning($"{GetType().Name} was asked to undo, " +
                    $"but it has nothing to undo. Check CanUndo first.");
            }
        }

        public virtual void Redo()
        {
            if (!UndoHistory.Redo())
            {
                UnityEngine.Debug.LogWarning($"{GetType().Name} was asked to redo, " +
                    $"but it has nothing to redo. Check CanRedo first.");
            }
        }
        #endregion

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
            if (Logic != null && Loader != null)
            {
                var loadResult = Loader.GetLoadResult<object>();
                Logic.Load(loadResult);
            }
            _subwindow?.Refresh();
            _subwindow?.Show();
            SetCurrentStateAsBase();
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
        /// <summary>
        /// Setting this raises ControlPanelSignals.UnsavedChangesChanged when the value
        /// actually changes. Subclasses that override this to compute their own value
        /// should call NotifyUnsavedChangesChanged whenever it may have changed.
        /// </summary>
        public virtual bool HasUnsavedChanges
        {
            // With a logic module, that's what knows (derived from its history); setting
            // this then has no effect on what's reported.
            get => Logic != null ? 
            Logic.HasUnsavedChanges : 
            _hasUnsavedChanges;
            protected set
            {
                if (_hasUnsavedChanges == value)
                {
                    return;
                }

                _hasUnsavedChanges = value;
                NotifyUnsavedChangesChanged();
            }
        }
        private bool _hasUnsavedChanges;

        protected void NotifyUnsavedChangesChanged()
        {
            ControlPanelSignals.UnsavedChangesChanged(this);
        }

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
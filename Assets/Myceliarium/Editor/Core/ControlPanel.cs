using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Type = System.Type;
using UnityDebug = UnityEngine.Debug;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Serves as a hub for the Database-esque UI that other plugins may want to set up.
    /// </summary>
    public abstract class ControlPanel : EditorWindow, IControlPanel
    {
        #region Configurable Properties
        protected abstract string DisplayName { get; }

        /// <summary>
        /// The path to the uxml for the control panel's root window. This is relative
        /// to Resources.
        /// </summary>
        protected abstract string PathToUxml { get; }

        public virtual Vector2 MinWindowSize => DefaultWindowSize;
        protected virtual Vector2 DefaultWindowSize => new Vector2(1280, 800);
        public virtual Vector2 MaxWindowSize => DefaultWindowSize;
        #endregion

        /// <summary>
        /// Alias for DisplayName.
        /// </summary>
        protected string WindowTitle => DisplayName;

        protected virtual void OnEnable()
        {
            SetGlobalSubs(true);
            saveChangesMessage = $"{DisplayName} has unsaved changes. Save them before closing?";
            ControlPanelSignals.ControlPanelOpened.Invoke(this);
        }

        protected virtual void SetGlobalSubs(bool on)
        {
            if (on)
            {
                ControlPanelSignals.SaveRequested += OnSaveRequested;
                ControlPanelSignals.CloseRequested += OnCloseRequested;
                ControlPanelSignals.ReinitRequested += OnReinitRequested;
                ControlPanelSignals.LanguageChanged += OnLanguageChanged;
            }
            else
            {
                ControlPanelSignals.SaveRequested -= OnSaveRequested;
                ControlPanelSignals.CloseRequested -= OnCloseRequested;
                ControlPanelSignals.ReinitRequested -= OnReinitRequested;
                ControlPanelSignals.LanguageChanged -= OnLanguageChanged;
            }
        }

        protected virtual void OnSaveRequested(IControlPanel cPanel)
        {
            if (!ReferenceEquals(cPanel, this))
            {
                return;
            }

            _saveCoordinator.SaveAll();
        }

        private readonly SaveCoordinator _saveCoordinator = new SaveCoordinator();

        protected IReadOnlyDictionary<IControlPanelEntrySaver, IList<IControlPanelEntry>> Savers =>
            _saveCoordinator.Savers;

        protected virtual void OnCloseRequested(IControlPanel cPanel)
        {
            if (!ReferenceEquals(cPanel, this))
            {
                return;
            }

            this.Close(); // Leads to OnDestroy, hence why we don't invoke ControlPanelClosed here.
        }

        protected virtual void OnReinitRequested(IControlPanel cPanel)
        {
            if (!ReferenceEquals(cPanel, this))
            {
                return;
            }

            OnReinitConfirmed();
        }

        public void OnReinitConfirmed()
        {
            ControlPanelReinitializer.Reinit(_entries, _attacher, _selectionController);
            // Fresh entries start out clean without signalling anything, so check again
            _unsavedChangesTracker?.Refresh();
        }

        private readonly EntryCollection _entries = new EntryCollection();

        private readonly ControlPanelEntryAttacher _attacher = new ControlPanelEntryAttacher();

        private UnsavedChangesTracker _unsavedChangesTracker;

        private readonly ControlPanelTabSelectionController _selectionController =
            new ControlPanelTabSelectionController();

        /// <summary>
        /// Called when this panel's footer reports a language change. Does nothing by
        /// default; override to react to it.
        /// </summary>
        protected virtual void OnLanguageChanged(IControlPanel cPanel, string language)
        {
        }

        /// <summary>
        /// Called by Unity when the user picks "Save" in the prompt shown for closing
        /// the window (e.g. via its X button) while hasUnsavedChanges is true.
        /// </summary>
        public override void SaveChanges()
        {
            OnSaveRequested(this);
            base.SaveChanges();
        }

        /// <summary>
        /// Called by Unity when the user picks "Discard" in that same prompt. Mirrors
        /// what the Cancel button does after its confirmation; Unity closes the window
        /// afterwards.
        /// </summary>
        public override void DiscardChanges()
        {
            OnReinitConfirmed();
            base.DiscardChanges();
        }

        /// <summary>
        /// Local to the VisualElements registered under this ControlPanel. The footers'
        /// elements are handled by the footers themselves.
        /// </summary>
        protected virtual void SetLocalSubs(bool on)
        {
        }

        public virtual void CreateGUI()
        {
            PreRootPrep(out bool success);
            if (!success)
            {
                AbortInit();
                return;
            }

            RootPrep();
        }

        /// <summary>
        /// By default, this func has success set to true. When overriding this, you
        /// might want it to be false if some critical initialization fails, so that
        /// the window doesn't open in a broken state.
        /// </summary>
        protected virtual void PreRootPrep(out bool success)
        {
            SetTitleContent();
            SetWindowSizeBounds();
            RefreshTopLevelEntryCache();
            // ^Why just the top-level ones? Because after those are done being attached
            // to this ControlPanel, we expect them to handle attaching their subs
            success = true;
        }

        private void SetTitleContent()
        {
            titleContent = new GUIContent(DisplayName);
        }

        private void SetWindowSizeBounds()
        {
            minSize = MinWindowSize;
            maxSize = MaxWindowSize;
        }

        private void AbortInit()
        {
            UnityDebug.LogError($"{DisplayName} failed to initialize. " +
                $"Aborting GUI creation.");
            this.Close();
        }

        protected virtual void RootPrep()
        {
            TryAddBaseWindow(out bool success);
            if (!success)
            {
                AbortInit();
                return;
            }

            RegisterVisualElements();
            SetLocalSubs(false);
            SetLocalSubs(true);
            DoEntryPreps();
            RefreshFooter();
            PrepUnsavedChangesSync();
        }

        private void TryAddBaseWindow(out bool success)
        {
            success = false;

            ValidateUxml(out success, out VisualTreeAsset vTreeAsset);
            if (!success)
            {
                return;
            }

            VisualElement baseWindow = vTreeAsset.Instantiate();
            Root.Add(baseWindow);
            success = true;
        }

        private void ValidateUxml(out bool success, out VisualTreeAsset vTreeAsset)
        {
            vTreeAsset = Resources.Load<VisualTreeAsset>(PathToUxml);
            bool loaded = vTreeAsset != null;
            success = loaded;
            if (!loaded)
            {
                UnityDebug.LogError($"Failed to load uxml at path {PathToUxml}. " +
                    $"Please ensure the path is correct and the file exists.");
            }
        }

        protected virtual void RegisterVisualElements()
        {
            _bottomBar = Root.Q<VisualElement>("BottomBar");
        }

        protected VisualElement _bottomBar;

        private void DoEntryPreps()
        {
            PrepAttacher();
            RegisterSubentries();
            PrepSelectionController();
            RefreshSaverCache();
        }

        private void PrepAttacher()
        {
            _attacher.Dispose();
            _attacher.Init(rootVisualElement);

            _entries.SortTopLevel(Sort);
            _attacher.Attach(_entries.TopLevel);
        }

        /// <summary>
        /// Default implementation sorts the entries by SortingOrder first,
        /// then by SortingName. Subclasses can override to provide
        /// different sorting logic for the entries before they are
        /// attached to the control panel.
        /// </summary>
        protected virtual void Sort(IList<IControlPanelEntry> entries)
        {
            EntryCollection.SortByDefault(entries);
        }

        protected virtual void RegisterSubentries()
        {
            _entries.RebuildFlattened();
        }

        private void PrepSelectionController()
        {
            _selectionController.Dispose();
            _selectionController.Init(_entries);
        }

        protected virtual void RefreshSaverCache()
        {
            _saveCoordinator.Refresh(SaverSuperType, _entries.AllWithSubwindows);
        }

        protected virtual void RefreshFooter()
        {
            _footer?.Dispose();
            _footer = ControlPanelFooterRegistry.CreateFooterOfType(FooterSuperType);
            if (_footer == null)
            {
                UnityDebug.LogError($"Failed to create footer of type {FooterSuperType}. " +
                    $"Please ensure that a concrete class implementing that interface exists.");
                return;
            }
            _footer.Init(this);
        }

        protected IControlPanelFooter _footer;

        /// <summary>
        /// Unity reads hasUnsavedChanges when the window is closed through its own X button
        /// (prompting Save/Discard/Cancel), so it has to be current before that happens.
        /// The tracker keeps it in step by reacting to the entries' signals.
        /// </summary>
        private void PrepUnsavedChangesSync()
        {
            _unsavedChangesTracker?.Dispose();
            
            _unsavedChangesTracker = new UnsavedChangesTracker(_entries, UpdateHasUnsavedChanges);
            void UpdateHasUnsavedChanges(bool dirty)
            {
                hasUnsavedChanges = dirty;
            }

            _unsavedChangesTracker.Start();
        }

        protected virtual void RefreshTopLevelEntryCache()
        {
            var toCheck = ControlPanelEntryRegistry.GetEntriesOfType(EntrySuperType);
            _entries.SetTopLevel(toCheck);
        }

        /// <summary>
        /// The type of entries that this Control Panel is responsible for. This is used to
        /// help this get only the ones it needs to work with. When overriding, best
        /// set this to the interface type that your entries implement (rather than any
        /// one concrete type). 
        /// </summary>
        protected abstract Type EntrySuperType { get; }

        /// <summary>
        /// The type of savers that this Control Panel is responsible for. Same sort of
        /// logic as EntrySuperType; take a look at its summary for more details.
        /// </summary>
        protected abstract Type SaverSuperType { get; }

        /// <summary>
        /// The type of footer that this Control Panel is responsible for. Same sort of
        /// logic as EntrySuperType; take a look at its summary for more details. Defaults
        /// to the base footer; feel free to override it to go for a different one.
        /// </summary>
        protected virtual Type FooterSuperType => typeof(IControlPanelFooter);

        public virtual VisualElement Root => rootVisualElement;

        public IReadOnlyList<IControlPanelEntry> TopLevelEntries => _entries.TopLevel;
        protected IReadOnlyList<IControlPanelEntry> AllEntriesWithSubwindows =>
            _entries.AllWithSubwindows;

        protected virtual void OnDisable()
        {
            SetGlobalSubs(false);
            SetLocalSubs(false);
        }

        protected virtual void OnDestroy()
        {
            ControlPanelSignals.PreControlPanelClosed(this);

            _attacher.Dispose();
            _footer?.Dispose();
            _selectionController.Dispose();

            _footer = null;

            _unsavedChangesTracker?.Dispose();
        }

        public bool Contains(IControlPanelEntry entry)
        {
            return _entries.Contains(entry);
        }

        /// <summary>
        /// Sends an undo/redo request (from Ctrl+Z and friends while this panel is
        /// focused) to the displayed entry only; Unity's global Undo is not involved.
        /// </summary>
        internal void HandleUndoRequest(bool redo)
        {
            // Ending the typing session first puts it on the entry's history, so the
            // undo below takes back what was just typed. It also means the next
            // keystroke starts a fresh edit instead of continuing a stale one.
            bool nothingToWorkWith = rootVisualElement == null ||
            rootVisualElement.panel == null ||
            rootVisualElement.panel.focusController == null;
            if (nothingToWorkWith)
            {
                return;
            }

            var panel = rootVisualElement.panel;
            var focusController = panel.focusController;
			if (focusController.focusedElement is VisualElement focusedElem)
			{
				focusedElem.Blur();
			}

			ControlPanelUndoRouter.Route(_selectionController.DisplayedEntry, redo);
        }

    }

    public interface IControlPanel
    {
        VisualElement Root { get; }
        IReadOnlyList<IControlPanelEntry> TopLevelEntries { get; }
        bool Contains(IControlPanelEntry entry);
    }

}
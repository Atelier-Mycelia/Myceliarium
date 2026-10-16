using System.Collections.Generic;
using System;
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

            foreach (var kvp in _savers)
            {
                var saver = kvp.Key;
                var compatibleEntries = kvp.Value;
                foreach (var entry in compatibleEntries)
                {
                    saver.Save(entry);
                }
            }
        }

        protected IReadOnlyDictionary<IControlPanelEntrySaver, IList<IControlPanelEntry>> Savers =>
            _savers;

        private readonly Dictionary<IControlPanelEntrySaver, IList<IControlPanelEntry>> _savers =
            new Dictionary<IControlPanelEntrySaver, IList<IControlPanelEntry>>();

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
            foreach (var entry in _topLevelEntries)
            {
                entry.Dispose();
                entry.Init(forceReinit: true);
            }

            _attacher.Attach(_allEntriesWithSubwindows);
            _selectionController.OnAllEntriesReinitted();
        }

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

        /// <summary>
        /// Checks the given entries and all their descendants.
        /// </summary>
        public static bool AnyEntryHasUnsavedChanges(IEnumerable<IControlPanelEntry> entries)
        {
            if (entries == null)
            {
                return false;
            }

            foreach (var entry in entries)
            {
                if (entry == null)
                {
                    continue;
                }

                if (entry.HasUnsavedChanges ||
                    AnyEntryHasUnsavedChanges(entry.GetSubentries()))
                {
                    return true;
                }
            }

            return false;
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

        private void AbortInit()
        {
            UnityDebug.LogError($"{DisplayName} failed to initialize. " +
                $"Aborting GUI creation.");
            this.Close();
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

        protected virtual void RefreshTopLevelEntryCache()
        {
            _topLevelEntries.Clear();
            var toCheck = ControlPanelEntryRegistry.GetEntriesOfType(EntrySuperType);
            var topLevelOnes = FilterTopLevelEntries(toCheck);
            for (int i = 0; i < topLevelOnes.Count; i++)
            {
                var toAdd = topLevelOnes[i];
                _topLevelEntries.Add(toAdd);
            }
        }

        /// <summary>
        /// Pure filtering logic, isolated from the registry/cache so it can be
        /// unit tested without needing a live ControlPanel/EditorWindow.
        /// </summary>
        public static IList<IControlPanelEntry> FilterTopLevelEntries(IList<IControlPanelEntry> toCheck)
        {
            var result = new List<IControlPanelEntry>();
            if (toCheck == null)
            {
                return result;
            }

            for (int i = 0; i < toCheck.Count; i++)
            {
                var elem = toCheck[i];
                if (elem != null && elem.IsTopLevel)
                {
                    result.Add(elem);
                }
            }
            return result;
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
        /// The type of loaders that this control panel is responsible for. Same sort of
        /// logic as EntrySuperType; take a look at its summary for more details.
        /// </summary>
        protected abstract Type LoaderSuperType { get; }

        /// <summary>
        /// The type of footer that this Control Panel is responsible for. Same sort of
        /// logic as EntrySuperType; take a look at its summary for more details. Defaults
        /// to the base footer; feel free to override it to go for a different one.
        /// </summary>
        protected virtual Type FooterSuperType => typeof(IControlPanelFooter);

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

        protected virtual void RefreshSaverCache()
        {
            _savers.Clear();
            var found = CpEntrySaverReg.GetSaversOfType(SaverSuperType);
            for (int i = 0; i < found.Count; i++)
            {
                var elem = found[i];
                var compatibleEntries = EntriesCompatibleWith(elem);
                _savers.Add(elem, compatibleEntries);
            }
        }

        private IList<IControlPanelEntry> EntriesCompatibleWith(IControlPanelEntrySaver saver)
        {
            return FilterCompatibleEntries(_allEntriesWithSubwindows, saver.IsCompatibleWith);
        }

        /// <summary>
        /// Pure filtering logic, isolated so it can be unit tested without
        /// needing a live ControlPanel/EditorWindow, concrete savers, or loaders.
        /// </summary>
        public static IList<IControlPanelEntry> FilterCompatibleEntries(
            IList<IControlPanelEntry> allEntries,
            Func<IControlPanelEntry, bool> isCompatible)
        {
            var compatibleEntries = new List<IControlPanelEntry>();
            if (allEntries == null || isCompatible == null)
            {
                return compatibleEntries;
            }

            for (int i = 0; i < allEntries.Count; i++)
            {
                var entry = allEntries[i];
                if (isCompatible(entry))
                {
                    compatibleEntries.Add(entry);
                }
            }
            return compatibleEntries;
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

        #region Registering base window
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
        #endregion

        public virtual VisualElement Root => rootVisualElement;

        public IReadOnlyList<IControlPanelEntry> TopLevelEntries => _topLevelEntries;
        protected virtual void RegisterVisualElements()
        {
            _bottomBar = Root.Q<VisualElement>("BottomBar");
        }

        protected VisualElement _bottomBar;

        private void PrepUnsavedChangesSync()
        {
            _unsavedChangesSync?.Pause();
            SyncHasUnsavedChanges();
            _unsavedChangesSync = rootVisualElement.schedule.Execute(SyncHasUnsavedChanges).Every(100);
        }

        /// <summary>
        /// Unity reads hasUnsavedChanges when the window is closed through its own X button
        /// (prompting Save/Discard/Cancel), so it has to be current before that happens.
        /// </summary>
        private void SyncHasUnsavedChanges()
        {
            bool dirty = AnyEntryHasUnsavedChanges(_topLevelEntries);
            if (hasUnsavedChanges != dirty)
            {
                hasUnsavedChanges = dirty;
            }
        }

        private IVisualElementScheduledItem _unsavedChangesSync;


        #region Entry Preps
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

            Sort(_topLevelEntries);
            _attacher.Attach(_topLevelEntries);
        }

        private void PrepSelectionController()
        {
            _selectionController.Dispose();
            _selectionController.Init(_allEntriesWithSubwindows);
        }

        private readonly ControlPanelEntryAttacher _attacher = new ControlPanelEntryAttacher();
        private readonly ControlPanelTabSelectionController _selectionController =
            new ControlPanelTabSelectionController();

        /// <summary>
        /// Default implementation sorts the entries by SortingOrder first,
        /// then by MainDisplayName. Subclasses can override to provide
        /// different sorting logic for the entries before they are
        /// attached to the control panel.
        /// </summary>
        protected virtual void Sort(IList<IControlPanelEntry> entries)
        {
            if (entries is List<IControlPanelEntry> list)
            {
                list.Sort(CompareEntries);
                return;
            }

            var buffer = new List<IControlPanelEntry>(entries);
            buffer.Sort(CompareEntries);
            entries.Clear();
            for (int i = 0; i < buffer.Count; i++)
            {
                entries.Add(buffer[i]);
            }
        }

        /// <summary>
        /// Pure comparison logic, isolated so it can be unit tested directly
        /// without needing a live ControlPanel/EditorWindow.
        /// </summary>
        public static int CompareEntries(IControlPanelEntry a, IControlPanelEntry b)
        {
            int sortingOrderComparison = a.SortingOrder.CompareTo(b.SortingOrder);
            if (sortingOrderComparison != 0)
            {
                return sortingOrderComparison;
            }

            return string.Compare(a.SortingName, b.SortingName, System.StringComparison.Ordinal);
        }
        protected readonly List<IControlPanelEntry> _topLevelEntries = new List<IControlPanelEntry>();

        protected virtual void RegisterSubentries()
        {
            var collected = CollectAllEntriesWithSubwindows(_topLevelEntries);
            for (int i = 0; i < collected.Count; i++)
            {
                var entry = collected[i];
                if (_allEntriesWithSubwindowsSet.Add(entry))
                {
                    _allEntriesWithSubwindows.Add(entry);
                }
            }

            AssertNoDuplicateEntryTypes();
        }

        /// <summary>
        /// RPG Maker's Database window never has more than one tab for the same
        /// data category (e.g. only ever one "Actors" tab). Since Myceliarium
        /// entries are discovered via reflection, it's possible for a user to
        /// accidentally register the same entry type as a subentry under two
        /// different top-level parents. This guards against that.
        /// </summary>
        protected virtual void AssertNoDuplicateEntryTypes()
        {
            var allEntries = GetAllEntries();
            ControlPanelEntryTypeValidator.AssertNoDuplicateEntryTypes(allEntries);
        }

        private IList<IControlPanelEntry> GetAllEntries()
        {
            var allEntries = new List<IControlPanelEntry>(_topLevelEntries);
            var seen = new HashSet<IControlPanelEntry>(_topLevelEntries);
            for (int i = 0; i < _allEntriesWithSubwindows.Count; i++)
            {
                var sub = _allEntriesWithSubwindows[i];
                if (seen.Add(sub))
                {
                    allEntries.Add(sub);
                }
            }
            return allEntries;
        }

        /// <summary>
        /// Pure logic for gathering every top-level entry's (recursive) subentries
        /// into a single flat, de-duplicated list. Isolated so it can be unit
        /// tested without needing a live ControlPanel/EditorWindow.
        /// </summary>
        public static IList<IControlPanelEntry> CollectAllEntriesWithSubwindows(
            IList<IControlPanelEntry> topLevelEntries)
        {
            var result = new List<IControlPanelEntry>();
            if (topLevelEntries == null)
            {
                return result;
            }

            var seen = new HashSet<IControlPanelEntry>();
            for (int i = 0; i < topLevelEntries.Count; i++)
            {
                var topLevelElem = topLevelEntries[i];
                if (topLevelElem.IsMeantToHaveSubwindow && seen.Add(topLevelElem))
                {
                    result.Add(topLevelElem);
                }

                var subentries = topLevelElem.GetSubentries(recursive: true);
                for (int j = 0; j < subentries.Count; j++)
                {
                    var subentry = subentries[j];
                    if (subentry.IsMeantToHaveSubwindow && seen.Add(subentry))
                    {
                        result.Add(subentry);
                    }
                }
            }
            return result;
        }


        protected IReadOnlyList<IControlPanelEntry> AllEntriesWithSubwindows =>
            _allEntriesWithSubwindows;

        // The set mirrors the list, for O(1) membership checks.
        private readonly List<IControlPanelEntry> _allEntriesWithSubwindows = new List<IControlPanelEntry>();
        private readonly HashSet<IControlPanelEntry> _allEntriesWithSubwindowsSet =
            new HashSet<IControlPanelEntry>();
        #endregion

        protected virtual void OnDestroy()
        {
            ControlPanelSignals.PreControlPanelClosed(this);

            _attacher.Dispose();
            _footer?.Dispose();
            _selectionController.Dispose();

            _footer = null;

            _unsavedChangesSync?.Pause();
        }

        protected virtual void OnDisable()
        {
            SetGlobalSubs(false);
            SetLocalSubs(false);
        }

        public bool Contains(IControlPanelEntry entry)
        {
            bool result = entry != null && (_topLevelEntries.Contains(entry) ||
                _allEntriesWithSubwindowsSet.Contains(entry));

            return result;
        }

    }

    public interface IControlPanel
    {
        VisualElement Root { get; }
        IReadOnlyList<IControlPanelEntry> TopLevelEntries { get; }
        bool Contains(IControlPanelEntry entry);
    }

}
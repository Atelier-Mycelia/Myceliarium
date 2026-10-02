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
            ToggleGlobalSubs(true);
        }

        protected virtual void ToggleGlobalSubs(bool on)
        {
            if (on)
            {
                ControlPanelSignals.SaveRequested += OnSaveRequested;
                ControlPanelSignals.LoadRequested += OnLoadRequested;
                ControlPanelSignals.CloseRequested += OnCloseRequested;
            }
            else
            {
                ControlPanelSignals.SaveRequested -= OnSaveRequested;
                ControlPanelSignals.LoadRequested -= OnLoadRequested;
                ControlPanelSignals.CloseRequested -= OnCloseRequested;
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

        protected readonly IDictionary<IControlPanelEntrySaver, IList<IControlPanelEntry>> _savers =
            new Dictionary<IControlPanelEntrySaver, IList<IControlPanelEntry>>();

        protected virtual void OnLoadRequested(IControlPanel cPanel)
        {
            if (!ReferenceEquals(cPanel, this))
            {
                return;
            }

            foreach (var kvp in _loaders)
            {
                var loader = kvp.Key;
                var compatibleEntries = kvp.Value;
                foreach (var entry in compatibleEntries)
                {
                    loader.Load(entry);
                }
            }
        }

        protected readonly IDictionary<IControlPanelEntryLoader, IList<IControlPanelEntry>> _loaders =
            new Dictionary<IControlPanelEntryLoader, IList<IControlPanelEntry>>();

        protected virtual void OnCloseRequested(IControlPanel cPanel)
        {
            if (!ReferenceEquals(cPanel, this))
            {
                return;
            }

            _cancelButton.SetEnabled(false);
            this.Close();
        }

        protected Button _cancelButton;

        /// <summary>
        /// Local to the VisualElements registered under this ControlPanel.
        /// </summary>
        protected virtual void ToggleLocalSubs(bool on)
        {
            if (on)
            {
                if (_saveButton != null)
                {
                    _saveButton.clicked += OnSaveButtonClicked;
                }

                if (_cancelButton != null)
                {
                    _cancelButton.clicked += OnCancelButtonClicked;
                }
            }
            else
            {
                if (_saveButton != null)
                {
                    _saveButton.clicked -= OnSaveButtonClicked;
                }

                if (_cancelButton != null)
                {
                    _cancelButton.clicked -= OnCancelButtonClicked;
                }
            }
        }

        protected virtual void OnSaveButtonClicked()
        {
            ControlPanelSignals.SaveRequested(this);
        }

        protected virtual void OnCancelButtonClicked()
        {
            ControlPanelSignals.CloseRequested(this);
        }

        public virtual void CreateGUI()
        {
            PreRootPrep(out bool success);
            if (!success)
            {
                string logMessage = $"{DisplayName} failed to initialize. " +
                    $"Aborting GUI creation.";
                UnityDebug.LogError(logMessage);
                this.Close();
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

        protected virtual void RefreshTopLevelEntryCache()
        {
            _topLevelEntries.Clear();
            var toCheck = ControlPanelEntryRegistry.GetEntriesOfType(EntrySuperType);
            var topLevelOnes = FilterTopLevelEntries(toCheck);
            for (int i = 0; i < topLevelOnes.Count; i++)
            {
                _topLevelEntries.Add(topLevelOnes[i]);
            }
        }

        /// <summary>
        /// Pure filtering logic, isolated from the registry/cache so it can be
        /// unit tested without needing a live ControlPanel/EditorWindow.
        /// </summary>
        public static IList<IControlPanelEntry> FilterTopLevelEntries(
            IList<IControlPanelEntry> toCheck)
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
            return FilterCompatibleEntries(_allEntries, saver.IsCompatibleWith);
        }

        private IList<IControlPanelEntry> EntriesCompatibleWith(IControlPanelEntryLoader loader)
        {
            return FilterCompatibleEntries(_allEntries, loader.IsCompatibleWith);
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

        protected virtual void RefreshLoaderCache()
        {
            _loaders.Clear();
            var found = CpEntryLoaderReg.GetLoadersOfType(LoaderSuperType);
            for (int i = 0; i < found.Count; i++)
            {
                var elem = found[i];
                var compatibleEntries = EntriesCompatibleWith(elem);
                _loaders.Add(elem, compatibleEntries);
            }
        }

        protected virtual void RootPrep()
        {
            TryAddBaseWindow(out bool success);
            if (!success)
            {
                string logMessage = $"{DisplayName} failed to initialize. " +
                    $"Aborting GUI creation.";
                UnityDebug.LogError(logMessage);
                this.Close();
                return;
            }

            RegisterVisualElements();
            ToggleLocalSubs(false);
            ToggleLocalSubs(true);
            DoEntryPreps();
            HandleLanguageDropdown();
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
            _saveButton = Root.Q<Button>("SaveButton");
            _cancelButton = Root.Q<Button>("CancelButton");
            _bottomBar = Root.Q<VisualElement>("BottomBar");
            _langDropdown = Root.Q<DropdownField>("LanguageDropdown");
        }

        protected Button _saveButton;
        protected VisualElement _bottomBar;
        protected DropdownField _langDropdown;

        #region Entry Preps
        private void DoEntryPreps()
        {
            PrepAttacher();
            RegisterSubentries();
            RefreshSaverCache();
            RefreshLoaderCache();
        }

        private void PrepAttacher()
        {
            _attacher?.Dispose();
            _attacher.Init(rootVisualElement);

            Sort(_topLevelEntries);
            _attacher.Attach(_topLevelEntries);

            _selectionController?.Dispose();
            _selectionController.Init(_attacher.Entries);
        }

        private ControlPanelEntryAttacher _attacher = new ControlPanelEntryAttacher();
        private ControlPanelTabSelectionController _selectionController =
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
            var collected = CollectAllEntries(_topLevelEntries);
            for (int i = 0; i < collected.Count; i++)
            {
                var entry = collected[i];
                if (!_allEntries.Contains(entry))
                {
                    _allEntries.Add(entry);
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
            var everyEntry = new List<IControlPanelEntry>(_topLevelEntries);
            everyEntry.AddRange(_allEntries);
            ControlPanelEntryTypeValidator.AssertNoDuplicateEntryTypes(everyEntry);
        }

        /// <summary>
        /// Pure logic for gathering every top-level entry's (recursive) subentries
        /// into a single flat, de-duplicated list. Isolated so it can be unit
        /// tested without needing a live ControlPanel/EditorWindow.
        /// </summary>
        public static IList<IControlPanelEntry> CollectAllEntries(
            IList<IControlPanelEntry> topLevelEntries)
        {
            var result = new List<IControlPanelEntry>();
            if (topLevelEntries == null)
            {
                return result;
            }

            for (int i = 0; i < topLevelEntries.Count; i++)
            {
                var topLevelElem = topLevelEntries[i];
                var subentries = topLevelElem.GetSubentries(recursive: true);
                for (int j = 0; j < subentries.Count; j++)
                {
                    var subentry = subentries[j];
                    if (!result.Contains(subentry))
                    {
                        result.Add(subentry);
                    }
                }
            }
            return result;
        }

        
        protected readonly List<IControlPanelEntry> _allEntries = new List<IControlPanelEntry>();
        #endregion

        protected virtual void OnDestroy()
        {
            _attacher?.Dispose();
            _attacher = null;
            _selectionController?.Dispose();
            _selectionController = null;
        }

        protected virtual void HandleLanguageDropdown()
        {
            _langDropdown.choices.Clear();
            _langDropdown.choices.Add("English");
            _langDropdown.value = "English";
        }

        protected virtual void OnDisable()
        {
            ToggleGlobalSubs(false);
            ToggleLocalSubs(false);
        }
    }

    public interface IControlPanel
    {
        VisualElement Root { get; }
        IReadOnlyList<IControlPanelEntry> TopLevelEntries { get; }
    }

}
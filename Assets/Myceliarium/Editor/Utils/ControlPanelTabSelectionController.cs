using System;
using System.Collections.Generic;
using System.Linq;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Owns the tab-selection state machine for a ControlPanel: listens for
    /// <see cref="ControlPanelSignals.EntryTabClicked"/>, shows/hides the
    /// appropriate subwindow, and keeps tab selection state (IsSelected) in
    /// sync across the whole entry tree.
    ///
    /// Split out from ControlPanelEntryAttacher, which is responsible only for
    /// attaching entries' UI elements to the ControlPanel's VisualElement tree.
    /// </summary>
    public sealed class ControlPanelTabSelectionController : IDisposable
    {
        /// <summary>
        /// Works with the given entries, finding nested ones by walking their
        /// subentries on each click.
        /// </summary>
        public void Init(IReadOnlyList<IControlPanelEntry> entries)
        {
            _isDisposed = false;
            _collection = null;
            _entries = entries ?? Array.Empty<IControlPanelEntry>();
            _entryBeingDisplayed = null;
            SetSubs(true);
        }

        /// <summary>
        /// Works with the entries of the given collection, using its (set-based)
        /// Contains rather than walking subentries on each click. The collection is
        /// read live, so it can be rebuilt without needing to call this again.
        /// </summary>
        public void Init(EntryCollection collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            Init(collection.AllWithSubwindows);
            _collection = collection;
        }

        private EntryCollection _collection;
        private bool _isDisposed = true;
        private IReadOnlyList<IControlPanelEntry> _entries = Array.Empty<IControlPanelEntry>();
        private IControlPanelEntry _entryBeingDisplayed;

        private void SetSubs(bool on)
        {
            if (on)
            {
                ControlPanelSignals.EntryTabClicked += OnEntryTabClicked;
            }
            else
            {
                ControlPanelSignals.EntryTabClicked -= OnEntryTabClicked;
            }
        }

        private void OnEntryTabClicked(IControlPanelEntry entryForClicked)
        {
            bool ignoreIt = _entries == null ||
                !WeHave(entryForClicked) ||
                !entryForClicked.IsMeantToHaveSubwindow;
            if (ignoreIt)
            {
                return;
            }

            bool currentlyShowingEntry = _entryBeingDisplayed != null;
            bool switchToOtherOne = entryForClicked != _entryBeingDisplayed;

            if (switchToOtherOne)
            {
                _entryBeingDisplayed = entryForClicked;
                DeselectAllEntries();
                _entryBeingDisplayed.Select();
                LastSelected = _entryBeingDisplayed;
            }
        }

        private IControlPanelEntry LastSelected { get; set; }

        /// <summary>
        /// Reinitting replaces entry instances (e.g. the subentries a parent creates for
        /// itself), so the previously selected one is swapped for its new counterpart
        /// of the same type before being selected again.
        /// </summary>
        public void OnAllEntriesReinitted()
        {
            if (LastSelected == null)
            {
                return;
            }

            LastSelected = FindOfSameType(LastSelected);
            _entryBeingDisplayed = LastSelected;
            LastSelected?.Select();
        }

        private IControlPanelEntry FindOfSameType(IControlPanelEntry old)
        {
            var type = old.GetType();
            for (int i = 0; i < _entries.Count; i++)
            {
                var elem = _entries[i];
                if (elem.GetType() == type)
                {
                    return elem;
                }

                var subentries = elem.GetSubentries(recursive: true);
                for (int j = 0; j < subentries.Count; j++)
                {
                    if (subentries[j].GetType() == type)
                    {
                        return subentries[j];
                    }
                }
            }
            return null;
        }

        private bool WeHave(IControlPanelEntry entry)
        {
            if (_collection != null)
            {
                return _collection.Contains(entry);
            }

            // Need to do a recursive search because some entries are subentries of other entries.
            if (_entries == null || _entries.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                var elem = _entries[i];
                if (elem == entry)
                {
                    return true;
                }
                var subentries = elem.GetSubentries(recursive: true);
                if (subentries.Contains(entry))
                {
                    return true;
                }
            }

            return false;
        }

        private void DeselectAllEntries()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var elem = _entries[i];
                elem.Deselect();
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            SetSubs(false);
            _entryBeingDisplayed = null;
            _collection = null;
            _entries = Array.Empty<IControlPanelEntry>();
            _isDisposed = true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Owns the tab-selection state machine for a ControlPanel: listens for
    /// <see cref="ControlPanelSignals.OnEntryTabClicked"/>, shows/hides the
    /// appropriate subwindow, and keeps tab selection state (IsSelected) in
    /// sync across the whole entry tree.
    ///
    /// Split out from ControlPanelEntryAttacher, which is responsible only for
    /// attaching entries' UI elements to the ControlPanel's VisualElement tree.
    /// </summary>
    public sealed class ControlPanelTabSelectionController : IDisposable
    {
        public void Init(IReadOnlyList<IControlPanelEntry> entries)
        {
            _isDisposed = false;
            _entries = entries ?? Array.Empty<IControlPanelEntry>();
            _entryBeingDisplayed = null;
            ToggleSubs(true);
        }

        private bool _isDisposed = true;
        private IReadOnlyList<IControlPanelEntry> _entries = Array.Empty<IControlPanelEntry>();
        private IControlPanelEntry _entryBeingDisplayed;

        private void ToggleSubs(bool on)
        {
            if (on)
            {
                ControlPanelSignals.OnEntryTabClicked += OnEntryTabClicked;
            }
            else
            {
                ControlPanelSignals.OnEntryTabClicked -= OnEntryTabClicked;
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
            }
        }

        private bool WeHave(IControlPanelEntry entry)
        {
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

            ToggleSubs(false);
            _entryBeingDisplayed = null;
            _entries = Array.Empty<IControlPanelEntry>();
            _isDisposed = true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Handles attaching IControlPanelEntry instances to the ControlPanel window.
    /// Initializes entries and adds their UI elements to the appropriate containers.
    /// </summary>
    public sealed class ControlPanelEntryAttacher : IDisposable
    {
        public void Init(VisualElement rootElement)
        {
            _isDisposed = false;
            GetVisualElements();
            void GetVisualElements()
            {
                _rootElement = rootElement ?? throw new ArgumentNullException(nameof(rootElement));
                _mainTabSet = _rootElement.Q<VisualElement>("MainTabSet");
                _subwindowDisplay = _rootElement.Q<ScrollView>("CategorySubwindowDisplay");
            }

            ReportErrorsAsNeeded();
            void ReportErrorsAsNeeded()
            {
                string errorMessage = "";
                if (_mainTabSet == null)
                {
                    errorMessage += "Could not find 'MainTabSet' in " +
                        "ControlPanel UXML";
                }

                if (_subwindowDisplay == null)
                {
                    errorMessage += "\n\nCould not find 'CategorySubwindowDisplay' " +
                        "in ControlPanel UXML";
                }

                bool anyErrorsFound = !string.IsNullOrEmpty(errorMessage);
                if (anyErrorsFound)
                {
                    throw new InvalidOperationException(errorMessage);
                }
            }

            ToggleSubs(true);
        }

        private bool _isDisposed = false;
        private VisualElement _rootElement;
        private VisualElement _mainTabSet;
        private ScrollView _subwindowDisplay;

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
            bool shouldHideCurrentOneFirst = currentlyShowingEntry && switchToOtherOne;
            if (shouldHideCurrentOneFirst)
            {
                var subwindowShowing = _entryBeingDisplayed.Subwindow;
                subwindowShowing.Hide();
            }

            if (switchToOtherOne)
            {
                _entryBeingDisplayed = entryForClicked;
                var subwindow = entryForClicked.Subwindow;
                subwindow.Show();
                DeselectAllTabsExceptFor(entryForClicked.Tab);
            }
        }

        private bool WeHave(IControlPanelEntry entry)
        {
            // Need to do a recursive search because some entries are subentries of other entries.
            if (_entries == null || _entries.Count == 0)
            {
                return false;
            }

            bool foundIt = false;
            for (int i = 0; i < _entries.Count; i++)
            {
                var elem = _entries[i];
                if (elem == entry)
                {
                    foundIt = true;
                    break;
                }
                var subentries = elem.GetSubentries(recursive: true);
                if (subentries.Contains(entry))
                {
                    foundIt = true;
                    break;
                }
            }

            return foundIt;
        }
        private IControlPanelEntry _entryBeingDisplayed;

        private void DeselectAllTabsExceptFor(IControlPanelTab toLeaveAlone)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var elem = _entries[i];
                var tab = elem.Tab;
                tab.IsSelected = tab == toLeaveAlone;
                
                var subentries = elem.GetSubentries(recursive: true);
                for (int j = 0; j < subentries.Count; j++)
                {
                    var subentry = subentries[j];
                    var subtab = subentry.Tab;
                    subtab.IsSelected = subtab == toLeaveAlone;
                }
            }
        }

        public void Attach(IList<IControlPanelEntry> toAttach)
        {
            _entries = toAttach ?? throw new ArgumentNullException(nameof(toAttach));
            foreach (var elem in _entries)
            {
                if (!elem.IsTopLevel)
                {
                    // We expect the top level entries to handle their subentries
                    continue;
                }
                Attach(elem);//
            }
        }

        private IList<IControlPanelEntry> _entries;

        private void Attach(IControlPanelEntry entry)
        {
            // Remember, each entry fetched from the registry is expected to be unique.
            // They're also not supposed to have their state wiped until either
            // assemblies reload or Unity closes. Thus, it's possible that the entry
            // passed here already has its VisualElements prepped.
            if (!entry.IsInitted)
            {
                try
                {
                    entry.Init();
                }
                catch (Exception ex)
                {
                    string logMessage = $"Failed to attach {entry.GetType().Name} " +
                        $"to ControlPanel: {ex.Message}";
                    Debug.LogError(logMessage);
                }
            }

            _mainTabSet.Add(entry.Tab.Root);
            RegisterSubwindowsOf(entry);

        }

        private void RegisterSubwindowsOf(IControlPanelEntry entry)
        {
            // We want the subwindows parented to the holder, but until
            // the user clicks on the tab, we don't want them to be visible.
            var subwindow = entry.Subwindow;
            if (subwindow != null) // But as not all tabs are meant to have
                                   // subwindows tied to them...
            {
                subwindow.Hide();
                _subwindowDisplay.Add(subwindow.Root);
            }

            var subentries = entry.GetSubentries(recursive: true);
            for (int i = 0; i < subentries.Count; i++)
            {
                var subentry = subentries[i];
                subwindow = subentry.Subwindow;
                if (subwindow != null)
                {
                    subwindow.Hide();
                    _subwindowDisplay.Add(subwindow.Root);
                }
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
            _entries = null;
            _mainTabSet = null;
            _subwindowDisplay = null;
            _rootElement = null;
            _isDisposed = true;
        }

    }
}
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityDebug = UnityEngine.Debug;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Handles attaching IControlPanelEntry instances to the ControlPanel window.
    /// Initializes entries and adds their UI elements (tabs and subwindows) to
    /// the appropriate containers.
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
        }

        private bool _isDisposed = false;
        private VisualElement _rootElement;
        private VisualElement _mainTabSet;
        private ScrollView _subwindowDisplay;

        public void Attach(IList<IControlPanelEntry> toAttach)
        {
            foreach (var elem in toAttach)
            {
                if (!elem.IsTopLevel)
                {
                    // We expect the top-level entries to handle attaching their subs
                    continue;
                }
                Attach(elem);
            }
        }

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
                    UnityDebug.LogError(logMessage);
                }
            }

            _mainTabSet.Add(entry.Tab.Root);
            RegisterSubwindowsOf(entry);
        }

        private void RegisterSubwindowsOf(IControlPanelEntry entry)
        {
            var subwindow = entry.Subwindow;
            if (subwindow != null && !_subwindowDisplay.Contains(subwindow.Root))
            {
                _subwindowDisplay.Add(subwindow.Root);
            }

            var subentries = entry.GetSubentries(recursive: true);
            for (int i = 0; i < subentries.Count; i++)
            {
                var subentry = subentries[i];
                subwindow = subentry.Subwindow;
                if (subwindow != null)
                {
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

            _mainTabSet = null;
            _subwindowDisplay = null;
            _rootElement = null;
            _isDisposed = true;
        }

    }
}

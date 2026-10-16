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

        public void Attach(IEnumerable<IControlPanelEntry> toAttach)
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
            bool alreadyAttached = entry.Tab != null && _mainTabSet.Contains(entry.Tab.Root);
            // ^The tab being null means that the entry was either never initialized or was disposed.
            // In either case, we should attempt to initialize it again.
            if (alreadyAttached)
            {
                return;
            }

            try
            {
                entry.Init();
            }
            catch (Exception ex)
            {
                string logMessage = $"Failed to attach entry of type {entry.GetType().Name} " + 
                $"to ControlPanel. Message: {ex.Message}";
                UnityDebug.LogError(logMessage);
            }

            // If Init failed before the tab was made, there's nothing to attach, and
            // dereferencing it would only bury the error logged above.
            if (entry.Tab == null)
            {
                UnityDebug.LogError($"Entry of type {entry.GetType().Name} has no Tab " +
                    $"after Init(), so it can't be attached to ControlPanel.");
                return;
            }

            _mainTabSet.Add(entry.Tab.Root);
            RegisterSubwindowsOf(entry);
        }

        private void RegisterSubwindowsOf(IControlPanelEntry entry)
        {
            // An entry that failed to initialize properly can throw when asked for its
            // subwindow (or subentries). That shouldn't keep the others from attaching.
            RegisterSubwindow(entry);

            IReadOnlyList<IControlPanelEntry> subentries;
            try
            {
                subentries = entry.GetSubentries(recursive: true);
            }
            catch (Exception ex)
            {
                UnityDebug.LogError($"Failed to get the subentries of " +
                    $"{entry.GetType().Name} while attaching its subwindows. " +
                    $"Message: {ex.Message}");
                return;
            }

            for (int i = 0; i < subentries.Count; i++)
            {
                RegisterSubwindow(subentries[i]);
            }
        }

        private void RegisterSubwindow(IControlPanelEntry entry)
        {
            IControlPanelSubwindow subwindow;
            try
            {
                subwindow = entry.Subwindow;
            }
            catch (Exception ex)
            {
                UnityDebug.LogError($"Failed to get the subwindow of " +
                    $"{entry.GetType().Name} while attaching it. Message: {ex.Message}");
                return;
            }

            if (ShouldRegister(subwindow))
            {
                _subwindowDisplay.Add(subwindow.Root);
            }
        }

        private bool ShouldRegister(IControlPanelSubwindow subwindow)
        {
            if (subwindow == null)
            {
                return false;
            }

            bool alreadyRegistered = _subwindowDisplay.Contains(subwindow.Root);
            if (alreadyRegistered)
            {
                return false;
            }
            
            return true;
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

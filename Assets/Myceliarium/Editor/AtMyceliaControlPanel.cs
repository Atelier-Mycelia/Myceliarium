using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Type = System.Type;
using System.Collections.Generic;

namespace AtMycelia.Myceliarium
{
    public class AtMyceliaControlPanel : ControlPanel
    {
        #region Configurable Properties
        protected override string PathToUxml => "Editor/Uxml/ControlPanel";
        protected override string WindowTitle => "Atelier Mycelia Control Panel";
        #endregion

        [MenuItem("Window/Atelier Mycelia/Control Panel", priority = 0)]
        public static void BringUp()
        {
            if (S != null)
            {
                S.Focus();
                return;
            }

            var wnd = GetWindow<AtMyceliaControlPanel>();
        }

        public static ControlPanel S { get; private set; }

        protected override void RefreshEntryCache()
        {
            _entries.Clear();
            var toCheck = ControlPanelEntryRegistry.GetEntriesOfType(_entriesForOurEcosys);
            // We only want the top-level ones, not the subs
            for (int i = 0; i < toCheck.Count; i++)
            {
                var elem = toCheck[i];
                if (elem.IsTopLevel)
                {
                    _entries.Add(elem);
                }
            }
        }

        private static readonly Type _entriesForOurEcosys = typeof(IAtMyceliaControlPanelEntry);

        protected override void HandleLanguageDropdown()
        {
            string barName = "LanguageDropdown";
            var barRoot = Root.Q<VisualElement>(barName);
            if (barRoot == null)
            {
                Debug.LogError($"Failed to find {barName} in the Control Panel root.");
                return;
            }

            var barDropdown = barRoot.Q<DropdownField>();
            barDropdown.choices.Add("English");
            // Have that choice be selected
            barDropdown.value = "English";
            /*barRoot.style.display = DisplayStyle.None;*/
        }

        protected override void OnSaveRequested(IControlPanelEntry entry)
        {
            for (int i = 0; i < _savers.Count; i++)
            {
                var saver = _savers[i];
                if (saver.IsCompatibleWith(entry))
                {
                    saver.Save(entry);
                }
            }
        }

        protected override void OnLoadRequested(IControlPanelEntry entry)
        {
            for (int i = 0; i < _loaders.Count; i++)
            {
                var loader = _loaders[i];
                if (loader.IsCompatibleWith(entry))
                {
                    loader.Load(entry);
                }
            }
        }

        protected override void RefreshEntrySaverCache()
        {
            _savers.Clear();
            var found = CpEntrySaverReg.GetSaversOfType(_saversForOurEcosys);
            for (int i = 0; i < found.Count; i++)
            {
                var elem = found[i];
                var atMyceliaSaver = elem as IAtMyceliaControlPanelEntrySaver;
                bool validElem = atMyceliaSaver != null;
                if (!validElem)
                {
                    string logMessage = $"Found a saver of type {elem.GetType().Name} that is" +
                        $"not an IAtMyceliaControlPanelEntrySaver.";
                    Debug.LogError(logMessage);
                    continue;
                }
                _savers.Add(atMyceliaSaver);
            }
        }

        private readonly List<IAtMyceliaControlPanelEntrySaver> _savers = 
            new List<IAtMyceliaControlPanelEntrySaver>();

        private static readonly Type _saversForOurEcosys = typeof(IAtMyceliaControlPanelEntrySaver);

        protected override void RefreshEntryLoaderCache()
        {
            _loaders.Clear();
            var found = CpEntryLoaderReg.GetLoadersOfType(_loadersForOurEcosys);
            for (int i = 0; i < found.Count; i++)
            {
                var elem = found[i];
                var atMyceliaLoader = elem as IAtMyceliaControlPanelEntryLoader;
                bool validElem = atMyceliaLoader != null;
                if (!validElem)
                {
                    string logMessage = $"Found a loader of type {elem.GetType().Name} that is" +
                        $"not an IAtMyceliaControlPanelEntryLoader.";
                    Debug.LogError(logMessage);
                    continue;
                }
                _loaders.Add(atMyceliaLoader);
            }
        }

        private readonly List<IAtMyceliaControlPanelEntryLoader> _loaders = 
            new List<IAtMyceliaControlPanelEntryLoader>();

        private static readonly Type _loadersForOurEcosys = typeof(IAtMyceliaControlPanelEntryLoader);
    }

}
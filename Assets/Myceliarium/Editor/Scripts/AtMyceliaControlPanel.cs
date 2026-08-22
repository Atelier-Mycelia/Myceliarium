using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Type = System.Type;

namespace AtMycelia.Myceliarium
{
    public class AtMyceliaControlPanel : ControlPanel
    {
        #region Configurable Properties
        protected override string PathToUxml => "Editor/UIToolkitTemplates/ControlPanel";
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

        protected override IEnumerable<IControlPanelEntry> GetEntriesToAttach()
        {
            var filteredEntries = ControlPanelEntryRegistry.GetEntriesOfType(_forOurEcosys);
            // We only want the top-level entries, not the subs
            for (int i = 0; i < filteredEntries.Count; i++)
            {
                var elem = filteredEntries[i];
                if (!elem.IsTopLevel)
                {
                    filteredEntries.RemoveAt(i);
                    i--; // To avoid going out of bounds after removing an item
                }
            }

            return filteredEntries;
        }

        private static readonly Type _forOurEcosys = typeof(IAtMyceliaControlPanelEntry);

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
    }

}
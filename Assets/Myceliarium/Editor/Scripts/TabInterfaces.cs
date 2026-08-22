using System.Collections.Generic;
using UnityEngine.UIElements;

namespace AtMycelia.ControlPanel
{
    public interface IMainTab
    {
        string DisplayName { get; }

        /// Called when the main tab is clicked.
        /// Should return the default content for the right panel
        /// (if no subtab is selected).
        VisualElement CreateDefaultContent();

        /// Optional: subtabs discovered via reflection.
        IReadOnlyList<ISubtab> Subtabs { get; set; }
    }

    public interface ISubtab
    {
        string DisplayName { get; }

        /// <summary>
        /// Shown under the main tab owning this. Called when this subtab
        /// is clicked. Should return the content for the subwindow
        /// part of the Control Panel (which should be on the right).
        /// </summary>
        VisualElement CreateContent();
    }

}
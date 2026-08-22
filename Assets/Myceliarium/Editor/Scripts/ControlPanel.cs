using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Serves as a hub for the Database-esque UI that other plugins may want to set up.
    /// </summary>
    public abstract class ControlPanel : EditorWindow, IControlPanel
    {
        #region Configurable Properties
        protected virtual string WindowTitle => "Control Panel";

        /// <summary>
        /// The path to the uxml for the control panel's root window. This is relative
        /// to Resources.
        /// </summary>
        protected abstract string PathToUxml { get; }

        public virtual Vector2 MinWindowSize => DefaultWindowSize;
        protected virtual Vector2 DefaultWindowSize => new Vector2(1280, 800);
        public virtual Vector2 MaxWindowSize => DefaultWindowSize;
        #endregion

        public virtual void CreateGUI()
        {
            PreRootPrep(out bool success);
            if (!success)
            {
                string logMessage = $"{WindowTitle} failed to initialize. " +
                    $"Aborting GUI creation.";
                Debug.LogError(logMessage);
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
            success = true;
        }

        private void SetTitleContent()
        {
            titleContent = new GUIContent(WindowTitle);
        }

        private void SetWindowSizeBounds()
        {
            minSize = MinWindowSize;
            maxSize = MaxWindowSize;
        }

        protected virtual void RootPrep()
        {
            var vTreeAsset = Resources.Load<VisualTreeAsset>(PathToUxml);
            bool loaded = vTreeAsset != null;
            if (!loaded)
            {
                Debug.LogError($"Failed to load uxml at path {PathToUxml}. " +
                    $"Please ensure the path is correct and the file exists.");
                this.Close();
                return;
            }
            VisualElement baseWindow = vTreeAsset.Instantiate();
            Root.Add(baseWindow);
            GetEntriesAttached();
            HandleLanguageDropdown();
        }

        public virtual VisualElement Root => rootVisualElement;

        private void GetEntriesAttached()
        {
            _attacher?.Dispose();
            _attacher.Init(rootVisualElement);

            var toAttach = GetEntriesToAttach().ToList();
            Sort(toAttach);
            _attacher.Attach(toAttach);
        }

        private ControlPanelEntryAttacher _attacher = new ControlPanelEntryAttacher();

        protected abstract IEnumerable<IControlPanelEntry> GetEntriesToAttach();

        /// <summary>
        /// Default implementation does nothing. Subclasses can override to
        /// provide sorting logic for the entries before they are
        /// attached to the control panel.
        /// </summary>
        protected virtual void Sort(IEnumerable<IControlPanelEntry> entries)
        {
        }

        protected virtual void OnDestroy()
        {
            _attacher?.Dispose();
            _attacher = null;
        }

        protected virtual void HandleLanguageDropdown()
        {
            // Default implementation does nothing. Subclasses can override to provide
            // logic for handling a language bar if needed.
        }
    }

    public interface IControlPanel
    {
        VisualElement Root { get; }
    }

}
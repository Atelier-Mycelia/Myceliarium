using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Handles the editor-wide moments that would otherwise strand unsaved work in open
    /// ControlPanels: entering Play Mode (the user gets asked what to do with unsaved
    /// changes) and assembly reloads (panels close themselves and come back afterwards,
    /// since a panel that survives a reload comes back without its entries). Panels know
    /// nothing about either; they just offer <see cref="IGuardedPanel"/>.
    /// </summary>
    [InitializeOnLoad]
    public static class ControlPanelLifecycleGuard
    {
        private const string ReopenSessionKey = "AtMycelia.Myceliarium.PanelsToReopen";
        private const char TypeNameSeparator = '|';

        static ControlPanelLifecycleGuard()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            AssemblyReloadEvents.beforeAssemblyReload -= CloseAllForAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += CloseAllForAssemblyReload;

            EditorApplication.delayCall += ReopenRememberedPanels;
        }

        // Panels are looked up when needed rather than tracked through their open/close
        // signals, because restored windows can enable before this class is initialized.
        private static List<IGuardedPanel> FindPanels(bool onlyDirty)
        {
            var result = new List<IGuardedPanel>();
            var panels = Resources.FindObjectsOfTypeAll<ControlPanel>();
            for (int i = 0; i < panels.Length; i++)
            {
                IGuardedPanel panel = panels[i];
                if (!onlyDirty || panel.IsDirty)
                {
                    result.Add(panel);
                }
            }
            return result;
        }

        #region Play Mode
        private static readonly PlayModeGate _playModeGate = new PlayModeGate(
            () => FindPanels(onlyDirty: true),
            AskAboutUnsavedChanges,
            playing => EditorApplication.isPlaying = playing,
            action => EditorApplication.delayCall += () => action());

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
            {
                _playModeGate.OnExitingEditMode();
            }
        }

        private static UnsavedChangesChoice AskAboutUnsavedChanges()
        {
            int choice = EditorUtility.DisplayDialogComplex(
                "Unsaved changes",
                "There are unsaved changes in an open Control Panel. " +
                "Save them before entering Play Mode?",
                "Save", "Keep Editing", "Discard");

            switch (choice)
            {
                case SaveChoice:
                    return UnsavedChangesChoice.Save;
                case DiscardChoice:
                    return UnsavedChangesChoice.Discard;
                default:
                    return UnsavedChangesChoice.KeepEditing;
            }
        }

        private const int SaveChoice = 0;
        private const int DiscardChoice = 2;
        #endregion

        #region Assembly Reloads
        private static void CloseAllForAssemblyReload()
        {
            foreach (IGuardedPanel panel in FindPanels(onlyDirty: false))
            {
                RememberToReopen(panel.GetType());
                panel.CloseForAssemblyReload();
            }
        }

        private static void RememberToReopen(Type panelType)
        {
            string name = panelType.AssemblyQualifiedName;
            string existing = SessionState.GetString(ReopenSessionKey, string.Empty);
            string sessionKey = existing.Length == 0 ?
                name :
                existing + TypeNameSeparator + name;
            SessionState.SetString(ReopenSessionKey, sessionKey);
        }

        private static void ReopenRememberedPanels()
        {
            string remembered = SessionState.GetString(ReopenSessionKey, string.Empty);
            SessionState.EraseString(ReopenSessionKey);
            if (remembered.Length == 0)
            {
                return;
            }

            // A panel opened before the registry is done would come up with no entries,
            // or with stale ones.
            ControlPanelEntryRegistry.WhenReady(() => OpenPanels(remembered));
        }

        private static void OpenPanels(string remembered)
        {
            foreach (string typeName in remembered.Split(TypeNameSeparator))
            {
                Type panelType = Type.GetType(typeName);
                if (panelType != null && typeof(ControlPanel).IsAssignableFrom(panelType))
                {
                    EditorWindow.GetWindow(panelType);
                }
            }
        }
        #endregion
    }
}

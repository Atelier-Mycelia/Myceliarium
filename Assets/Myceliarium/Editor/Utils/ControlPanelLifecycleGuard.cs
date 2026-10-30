using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Handles the editor-wide moments that would otherwise strand unsaved work in open
    /// ControlPanels: entering Play Mode (the user gets asked what to do with unsaved
    /// changes) and assembly reloads (panels close themselves and come back afterwards).
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
            EditorApplication.delayCall += ReopenRememberedPanels;
        }

        #region Play Mode
        private static bool _resolving;

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode || _resolving)
            {
                return;
            }

            if (_skipNextCheck)
            {
                _skipNextCheck = false;
                return;
            }

            var dirtyPanels = FindPanelsWithUnsavedChanges();
            if (dirtyPanels.Count == 0)
            {
                return;
            }

            // Play Mode can't be vetoed outright, so it's stopped here and restarted
            // later if the user's answer allows for it.
            _resolving = true;
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => AskAbout(dirtyPanels);
        }

        private static List<ControlPanel> FindPanelsWithUnsavedChanges()
        {
            var result = new List<ControlPanel>();
            var panels = Resources.FindObjectsOfTypeAll<ControlPanel>();
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i].hasUnsavedChanges)
                {
                    result.Add(panels[i]);
                }
            }
            return result;
        }

        private static void AskAbout(List<ControlPanel> dirtyPanels)
        {
            int choice = EditorUtility.DisplayDialogComplex(
                "Unsaved changes",
                "There are unsaved changes in an open Control Panel. " +
                "Save them before entering Play Mode?",
                "Save", "Keep Editing", "Discard");

            switch (choice)
            {
                case SaveChoice:
                    SaveThenPlay(dirtyPanels);
                    break;
                case DiscardChoice:
                    Discard(dirtyPanels);
                    EnterPlayMode();
                    break;
                default:
                    _resolving = false;
                    break;
            }
        }

        private const int SaveChoice = 0;
        private const int DiscardChoice = 2;

        private static void SaveThenPlay(List<ControlPanel> dirtyPanels)
        {
            int pending = dirtyPanels.Count;
            for (int i = 0; i < dirtyPanels.Count; i++)
            {
                dirtyPanels[i].SaveAllThen(OnPanelSaved);
            }

            void OnPanelSaved()
            {
                pending--;
                if (pending == 0)
                {
                    EnterPlayMode();
                }
            }
        }

        private static void Discard(List<ControlPanel> dirtyPanels)
        {
            for (int i = 0; i < dirtyPanels.Count; i++)
            {
                dirtyPanels[i].DiscardChanges();
            }
        }

        private static void EnterPlayMode()
        {
            // Deferred because savers invoke onComplete before they raise SaveCompleted,
            // so the panels would still look dirty right now and ask all over again.
            EditorApplication.delayCall += () =>
            {
                _resolving = false;
                _skipNextCheck = true;
                EditorApplication.isPlaying = true;
            };
        }

        private static bool _skipNextCheck;
        #endregion

        #region Assembly Reloads
        internal static void RememberToReopen(Type panelType)
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

            // The entry registry fills itself on delayCalls of its own (twice, in fact:
            // the second pass replaces the entry instances), so a panel opened before
            // they're done would come up with no entries, or with stale ones.
            int ticksSinceEntriesAppeared = 0;
            int ticksWaited = 0;
            EditorApplication.CallbackFunction waitForRegistry = null;
            waitForRegistry = () =>
            {
                ticksWaited++;
                if (ControlPanelEntryRegistry.Entries.Count > 0)
                {
                    ticksSinceEntriesAppeared++;
                }

                bool settled = ticksSinceEntriesAppeared >= TicksToSettleAfterEntries;
                bool gaveUp = ticksWaited >= MaxTicksToWaitForEntries;
                if (!settled && !gaveUp)
                {
                    return;
                }

                EditorApplication.update -= waitForRegistry;
                OpenPanels(remembered);
            };
            EditorApplication.update += waitForRegistry;
        }

        private const int TicksToSettleAfterEntries = 3;
        private const int MaxTicksToWaitForEntries = 300;

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

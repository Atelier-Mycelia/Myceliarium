using System;
using System.Collections.Generic;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Decides what happens when Play Mode is about to start while panels have unsaved
    /// changes. Play Mode can't be vetoed outright, so the gate stops it, asks, and
    /// starts it again if the answer allows for it. Everything editor-specific comes in
    /// through the constructor, so this can be unit tested directly.
    /// </summary>
    internal sealed class PlayModeGate
    {
        /// <param name="findDirtyPanels">Returns the panels that have unsaved changes.</param>
        /// <param name="ask">Asks the user what to do about them.</param>
        /// <param name="setPlaying">Starts or stops Play Mode.</param>
        /// <param name="defer">Runs the given action a little later (not in the current
        /// call stack).</param>
        public PlayModeGate(Func<IReadOnlyList<IGuardedPanel>> findDirtyPanels,
            Func<UnsavedChangesChoice> ask,
            Action<bool> setPlaying,
            Action<Action> defer)
        {
            _findDirtyPanels = findDirtyPanels;
            _ask = ask;
            _setPlaying = setPlaying;
            _defer = defer;
        }

        private readonly Func<IReadOnlyList<IGuardedPanel>> _findDirtyPanels;
        private readonly Func<UnsavedChangesChoice> _ask;
        private readonly Action<bool> _setPlaying;
        private readonly Action<Action> _defer;

        private State _state = State.Idle;

        private enum State
        {
            Idle,
            AwaitingAnswer,
            Restarting
        }

        /// <summary>
        /// To be called when the editor reports that it's leaving Edit Mode.
        /// </summary>
        public void OnExitingEditMode()
        {
            switch (_state)
            {
                case State.AwaitingAnswer:
                    // Stopping Play Mode below raises this again
                    return;
                case State.Restarting:
                    // This is the Play Mode we started ourselves, with the question
                    // already settled
                    _state = State.Idle;
                    return;
            }

            var dirtyPanels = _findDirtyPanels();
            if (dirtyPanels.Count == 0)
            {
                return;
            }

            _state = State.AwaitingAnswer;
            _setPlaying(false);
            _defer(() => Resolve(dirtyPanels));
        }

        private void Resolve(IReadOnlyList<IGuardedPanel> dirtyPanels)
        {
            var unsavedChangeChoice = _ask();
            switch (unsavedChangeChoice)
            {
                case UnsavedChangesChoice.Save:
                    SaveThenRestart(dirtyPanels);
                    break;
                case UnsavedChangesChoice.Discard:
                    for (int i = 0; i < dirtyPanels.Count; i++)
                    {
                        dirtyPanels[i].Discard();
                    }
                    Restart();
                    break;
                default:
                    _state = State.Idle;
                    break;
            }
        }

        private void SaveThenRestart(IReadOnlyList<IGuardedPanel> dirtyPanels)
        {
            int pending = dirtyPanels.Count;
            
            for (int i = 0; i < dirtyPanels.Count; i++)
            {
                var panel = dirtyPanels[i];
                panel.SaveThen(OnPanelSaved);
            }

            void OnPanelSaved()
            {
                pending--;
                if (pending == 0)
                {
                    Restart();
                }
            }
        }

        private void Restart()
        {
            // Deferred because savers invoke onComplete before they raise SaveCompleted,
            // so the panels would still look dirty right now and ask all over again.
            _defer(() =>
            {
                _state = State.Restarting;
                _setPlaying(true);
            });
        }
    }

    internal enum UnsavedChangesChoice
    {
        Save,
        KeepEditing,
        Discard
    }

    /// <summary>
    /// What a panel offers to the editor-wide lifecycle handling process (see
    /// <see cref="ControlPanelLifecycleGuard"/>). Implemented explicitly by ControlPanel.
    /// </summary>
    internal interface IGuardedPanel
    {
        bool IsDirty { get; }

        /// <summary>
        /// Saves everything, then calls onSaved once all the savers are done.
        /// </summary>
        void SaveThen(Action onSaved);

        void Discard();

        /// <summary>
        /// Closes without any dialog, discarding unsaved changes, since a prompt would
        /// block the reload.
        /// </summary>
        void CloseForAssemblyReload();
    }

}

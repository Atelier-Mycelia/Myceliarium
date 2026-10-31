using System;
using System.Collections.Generic;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Tracks whether some asynchronous setup (done in passes) has finished, and lets
    /// interested parties wait for that without guessing at timing. Ready means at
    /// least one pass has completed and none is pending. Has no dependency on the
    /// editor, so it can be unit tested directly.
    /// </summary>
    internal sealed class ReadinessGate
    {
        public bool IsReady => _hasCompletedAPass && !_passPending;

        /// <summary>
        /// Marks a pass as scheduled. Returns false if one already is, in which case
        /// the caller shouldn't schedule another (it would do the same work).
        /// </summary>
        public bool TryBeginPass()
        {
            if (_passPending)
            {
                return false;
            }

            _passPending = true;
            return true;
        }

        private bool _passPending;

        /// <summary>
        /// Marks the scheduled pass as done, and runs whatever was waiting on readiness.
        /// </summary>
        public void EndPass()
        {
            _passPending = false;
            _hasCompletedAPass = true;
            RunWaiting();
        }

        private bool _hasCompletedAPass;

        private void RunWaiting()
        {
            // Copied and cleared first, since something in the waiting list
            // may want to add to said list. 
            var toRun = new List<Action>(_waiting);
            _waiting.Clear();
            for (int i = 0; i < toRun.Count; i++)
            {
                toRun[i]();
            }
        }

        private readonly List<Action> _waiting = new List<Action>();

        /// <summary>
        /// Runs the action right away if ready; otherwise once the pending pass ends.
        /// </summary>
        public void RunWhenReady(Action action)
        {
            if (action == null)
            {
                return;
            }

            if (IsReady)
            {
                action();
                return;
            }

            _waiting.Add(action);
        }

    }
}

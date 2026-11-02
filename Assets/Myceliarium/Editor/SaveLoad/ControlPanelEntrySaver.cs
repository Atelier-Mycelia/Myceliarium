using System;
using System.Collections;
using UnityEngine;

namespace AtMycelia.Myceliarium
{
    public abstract class ControlPanelEntrySaver : IControlPanelEntrySaver
    {
        public void Save(EntrySaveRequest request)
        {
            if (!IsCompatibleWith(request.Entry))
            {
                request.ReportFailure();
                return;
            }
            var coroutine = SaveProcessSequence(request);
            EditorCoroutineUtility.StartCoroutine(coroutine, this);
        }
        // ^Non-virtual because with the architecture as it is, clients
        // shouldn't override it.

        public abstract bool IsCompatibleWith(IControlPanelEntry toSaveFor);

        /// <summary>
        /// Ends by reporting the request as a success or a failure, whatever happens in
        /// the process.
        /// </summary>
        internal IEnumerator SaveProcessSequence(EntrySaveRequest request)
        {
            yield return GiveUiControlsTimeToFinish();

            var entry = request.Entry;
            if (entry.IsLoaded)
            {
                Exception error = null;
                var guarded = CoroutineGuard.Run(SaveProcess(request), thrown => error = thrown);
                while (guarded.MoveNext())
                {
                    yield return guarded.Current;
                }

                if (error != null)
                {
                    Debug.LogException(error);
                    request.ReportFailure();
                    yield break;
                }

                ControlPanelSignals.SaveCompleted(entry);
            }
            // ^An entry that isn't loaded (never selected since the panel was built, say)
            // can't have been edited, so it has nothing of the user's to save. That's not
            // a failure, but there's no saved state to announce either.

            request.ReportSuccess();
        }

        private IEnumerator GiveUiControlsTimeToFinish()
        {
            // 2 frames should be enough.
            yield return null; yield return null;
        }

        /// <summary>
        /// The one method subclasses override: the saving itself. It only runs when the
        /// entry's IsLoaded is true, so you shouldn't run into NREs that come from
        /// its state not having been loaded. Raising an exception here counts as the save failing.
        /// Reporting the outcome is taken care of afterwards, so don't do that here.
        /// </summary>
        protected virtual IEnumerator SaveProcess(EntrySaveRequest request)
        {
            yield break;
        }
    }

    public interface IControlPanelEntrySaver
    {
        /// <summary>
        /// Saves the request's entry. Exactly one of the request's outcome reports
        /// (ReportSuccess or ReportFailure) has to come out of this, sooner or later.
        /// </summary>
        void Save(EntrySaveRequest request);
        bool IsCompatibleWith(IControlPanelEntry toSaveFor);
    }

    /// <summary>
    /// One entry's save, and how its outcome gets reported back to whoever asked for it.
    /// Meant to be made per entry (never reused), and reports at most once.
    /// </summary>
    public sealed class EntrySaveRequest
    {
        /// <param name="onComplete">Called if the entry was saved.</param>
        /// <param name="onFail">Called, with the entry so the response code can check
        /// its state, if it couldn't be saved.</param>
        public EntrySaveRequest(IControlPanelEntry entry, Action onComplete = null,
            Action<IControlPanelEntry> onFail = null)
        {
            Entry = entry;
            _onComplete = onComplete;
            _onFail = onFail;
        }

        public IControlPanelEntry Entry { get; }

        private readonly Action _onComplete;
        private readonly Action<IControlPanelEntry> _onFail;

        public bool IsReported { get; private set; }

        public void ReportSuccess()
        {
            if (TryMarkReported())
            {
                _onComplete?.Invoke();
            }
        }

        private bool TryMarkReported()
        {
            if (IsReported)
            {
                return false;
            }

            IsReported = true;
            return true;
        }

        public void ReportFailure()
        {
            if (TryMarkReported())
            {
                _onFail?.Invoke(Entry);
            }
        }

    }

    public interface IAtMyceliaControlPanelEntrySaver : IControlPanelEntrySaver
    {
        // This interface is a marker interface for savers that are specific
        // to the AtMycelia ecosys.
    }
}

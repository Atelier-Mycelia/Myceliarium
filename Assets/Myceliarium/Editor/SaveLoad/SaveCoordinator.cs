using System;
using System.Collections.Generic;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Keeps track of which savers apply to which entries for one ControlPanel, and
    /// runs them on request. Has no dependency on EditorWindow, so it can be unit
    /// tested directly.
    /// </summary>
    public class SaveCoordinator
    {
        /// <summary>
        /// Maps each saver to the entries it's compatible with.
        /// </summary>
        public IReadOnlyDictionary<IControlPanelEntrySaver, IList<IControlPanelEntry>> Savers =>
            _savers;

        /// <summary>
        /// Rebuilds the saver-to-entries map. <paramref name="savers"/> are the savers
        /// the owning panel is responsible for; each gets paired with whichever of
        /// <paramref name="entries"/> it reports as compatible.
        /// </summary>
        public void Refresh(IList<IControlPanelEntrySaver> savers,
            IReadOnlyList<IControlPanelEntry> entries)
        {
            _savers.Clear();
            if (savers == null)
            {
                return;
            }

            for (int i = 0; i < savers.Count; i++)
            {
                var saver = savers[i];
                _savers[saver] = FilterCompatibleEntries(entries, saver.IsCompatibleWith);
            }
        }

        /// <summary>
        /// Convenience overload that pulls the savers of the given type from
        /// CpEntrySaverReg.
        /// </summary>
        public void Refresh(Type saverSuperType, IReadOnlyList<IControlPanelEntry> entries)
        {
            Refresh(CpEntrySaverReg.GetSaversOfType(saverSuperType), entries);
        }

        /// <summary>
        /// Has every saver save each of its compatible entries.
        /// </summary>
        /// <param name="onAllFinished">Called once, after every save has been reported
        /// as either a success or a failure (see <see cref="EntrySaveRequest"/>), with
        /// the entries that couldn't be saved (none, if all went well). Called right away
        /// if there's nothing to save. Savers are expected to report every request.</param>
        public void SaveAll(Action<IReadOnlyList<IControlPanelEntry>> onAllFinished = null)
        {
            int pending = 0;
            foreach (var kvp in _savers)
            {
                pending += kvp.Value.Count;
            }

            var failed = new List<IControlPanelEntry>();
            if (pending == 0)
            {
                onAllFinished?.Invoke(failed);
                return;
            }

            void OnOneFinished()
            {
                pending--;
                if (pending == 0)
                {
                    onAllFinished?.Invoke(failed);
                }
            }

            void OnOneFailed(IControlPanelEntry entry)
            {
                failed.Add(entry);
                OnOneFinished();
            }

            // Copied, in case a saver completes synchronously and a handler refreshes this
            var pairs = new List<KeyValuePair<IControlPanelEntrySaver, IList<IControlPanelEntry>>>(
                _savers);
            foreach (var kvp in pairs)
            {
                var saver = kvp.Key;
                var compatibleEntries = kvp.Value;
                for (int i = 0; i < compatibleEntries.Count; i++)
                {
                    // One per entry, as savers get to it a few frames later
                    var request = new EntrySaveRequest(compatibleEntries[i], OnOneFinished,
                        OnOneFailed);
                    saver.Save(request);
                }
            }
        }

        /// <summary>
        /// Pure filtering logic, isolated so it can be unit tested without
        /// needing a live ControlPanel/EditorWindow, concrete savers, or loaders.
        /// </summary>
        public static IList<IControlPanelEntry> FilterCompatibleEntries(
            IReadOnlyList<IControlPanelEntry> allEntries,
            Func<IControlPanelEntry, bool> isCompatible)
        {
            var compatibleEntries = new List<IControlPanelEntry>();
            if (allEntries == null || isCompatible == null)
            {
                return compatibleEntries;
            }

            for (int i = 0; i < allEntries.Count; i++)
            {
                var entry = allEntries[i];
                if (isCompatible(entry))
                {
                    compatibleEntries.Add(entry);
                }
            }
            return compatibleEntries;
        }

        private readonly Dictionary<IControlPanelEntrySaver, IList<IControlPanelEntry>> _savers =
            new Dictionary<IControlPanelEntrySaver, IList<IControlPanelEntry>>();
    }
}

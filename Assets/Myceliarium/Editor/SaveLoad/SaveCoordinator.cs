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
        public void SaveAll()
        {
            foreach (var kvp in _savers)
            {
                var saver = kvp.Key;
                var compatibleEntries = kvp.Value;
                for (int i = 0; i < compatibleEntries.Count; i++)
                {
                    saver.Save(compatibleEntries[i]);
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

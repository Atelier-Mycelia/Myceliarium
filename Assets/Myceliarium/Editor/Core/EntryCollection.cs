using System;
using System.Collections.Generic;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Holds the entries of one ControlPanel: the top-level ones, plus a flattened,
    /// de-duplicated list of every entry (top-level or nested) that has a subwindow.
    /// Has no dependency on EditorWindow, so it can be unit tested directly.
    /// </summary>
    public class EntryCollection
    {
        /// <summary>
        /// Replaces the top-level entries with whichever of the candidates are
        /// top-level. Doesn't touch the flattened list; call RebuildFlattened for that.
        /// </summary>
        public void SetTopLevel(IList<IControlPanelEntry> candidates)
        {
            _topLevel.Clear();
            _topLevelSet.Clear();

            var filtered = FilterTopLevelEntries(candidates);
            for (int i = 0; i < filtered.Count; i++)
            {
                if (_topLevelSet.Add(filtered[i]))
                {
                    _topLevel.Add(filtered[i]);
                }
            }
        }

        private readonly List<IControlPanelEntry> _topLevel = new List<IControlPanelEntry>();
        private readonly HashSet<IControlPanelEntry> _topLevelSet = new HashSet<IControlPanelEntry>();

        /// <summary>
        /// Pure filtering logic, isolated so it can be unit tested without
        /// needing a live ControlPanel/EditorWindow.
        /// </summary>
        public static IList<IControlPanelEntry> FilterTopLevelEntries(IList<IControlPanelEntry> toCheck)
        {
            var result = new List<IControlPanelEntry>();
            if (toCheck == null)
            {
                return result;
            }

            for (int i = 0; i < toCheck.Count; i++)
            {
                var elem = toCheck[i];
                if (elem != null && elem.IsTopLevel)
                {
                    result.Add(elem);
                }
            }
            return result;
        }

        /// <summary>
        /// Recollects the flattened list from the current top-level entries, then
        /// asserts that no concrete entry type shows up more than once overall.
        /// </summary>
        public void RebuildFlattened()
        {
            var collected = CollectAllEntriesWithSubwindows(_topLevel);
            _allWithSubwindows.Clear();
            _allWithSubwindowsSet.Clear();
            for (int i = 0; i < collected.Count; i++)
            {
                _allWithSubwindows.Add(collected[i]);
                _allWithSubwindowsSet.Add(collected[i]);
            }

            ControlPanelEntryTypeValidator.AssertNoDuplicateEntryTypes(GetAll());
        }

        /// <summary>
        /// Pure logic for gathering every top-level entry's (recursive) subentries
        /// into a single flat, de-duplicated list.
        /// </summary>
        public static IList<IControlPanelEntry> CollectAllEntriesWithSubwindows(
            IList<IControlPanelEntry> topLevelEntries)
        {
            var result = new List<IControlPanelEntry>();
            if (topLevelEntries == null)
            {
                return result;
            }

            var seen = new HashSet<IControlPanelEntry>();
            for (int i = 0; i < topLevelEntries.Count; i++)
            {
                var topLevelElem = topLevelEntries[i];
                if (topLevelElem.IsMeantToHaveSubwindow && seen.Add(topLevelElem))
                {
                    result.Add(topLevelElem);
                }

                var subentries = topLevelElem.GetSubentries(recursive: true);
                for (int j = 0; j < subentries.Count; j++)
                {
                    var subentry = subentries[j];
                    if (subentry.IsMeantToHaveSubwindow && seen.Add(subentry))
                    {
                        result.Add(subentry);
                    }
                }
            }
            return result;
        }

        private readonly HashSet<IControlPanelEntry> _allWithSubwindowsSet =
            new HashSet<IControlPanelEntry>();

        /// <summary>
        /// Lets the given sorter reorder the top-level entries in place. Defaults to
        /// SortByDefault.
        /// </summary>
        public void SortTopLevel(Action<IList<IControlPanelEntry>> sorter = null)
        {
            (sorter ?? SortByDefault)(_topLevel);
        }

        /// <summary>
        /// Sorts the entries by SortingOrder first, then by SortingName.
        /// </summary>
        public static void SortByDefault(IList<IControlPanelEntry> entries)
        {
            if (entries is List<IControlPanelEntry> list)
            {
                list.Sort(CompareEntries);
                return;
            }

            var buffer = new List<IControlPanelEntry>(entries);
            buffer.Sort(CompareEntries);
            entries.Clear();
            for (int i = 0; i < buffer.Count; i++)
            {
                entries.Add(buffer[i]);
            }
        }

        /// <summary>
        /// Pure comparison logic, isolated so it can be unit tested directly.
        /// </summary>
        public static int CompareEntries(IControlPanelEntry first, IControlPanelEntry second)
        {
            int sortingOrderComparison = first.SortingOrder.CompareTo(second.SortingOrder);
            if (sortingOrderComparison != 0)
            {
                return sortingOrderComparison;
            }

            return string.Compare(first.SortingName, second.SortingName, StringComparison.Ordinal);
        }

        private readonly List<IControlPanelEntry> _allWithSubwindows = new List<IControlPanelEntry>();

        /// <summary>
        /// The top-level entries followed by whichever flattened ones aren't already
        /// among them.
        /// </summary>
        public IList<IControlPanelEntry> GetAll()
        {
            var all = new List<IControlPanelEntry>(_topLevel);
            for (int i = 0; i < _allWithSubwindows.Count; i++)
            {
                var entry = _allWithSubwindows[i];
                if (!_topLevelSet.Contains(entry))
                {
                    all.Add(entry);
                }
            }
            return all;
        }

        public bool AnyHaveUnsavedChanges()
        {
            return AnyEntryHasUnsavedChanges(_topLevel);
        }

        /// <summary>
        /// Checks the given entries and all their descendants.
        /// </summary>
        public static bool AnyEntryHasUnsavedChanges(IEnumerable<IControlPanelEntry> entries)
        {
            if (entries == null)
            {
                return false;
            }

            foreach (var entry in entries)
            {
                if (entry == null)
                {
                    continue;
                }

                if (entry.HasUnsavedChanges ||
                    AnyEntryHasUnsavedChanges(entry.GetSubentries()))
                {
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyList<IControlPanelEntry> TopLevel => _topLevel;

        /// <summary>
        /// Every entry, top-level or nested, that's meant to have a subwindow.
        /// Filled by RebuildFlattened.
        /// </summary>
        public IReadOnlyList<IControlPanelEntry> AllWithSubwindows => _allWithSubwindows;

        public bool Contains(IControlPanelEntry entry)
        {
            return entry != null &&
                (_topLevelSet.Contains(entry) || _allWithSubwindowsSet.Contains(entry));
        }
    }
}

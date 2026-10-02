using System;
using System.Collections.Generic;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Validates that a flattened collection of IControlPanelEntry instances does
    /// not contain more than one entry of the exact same concrete type.
    ///
    /// This mirrors how RPG Maker's Database window works: each tab (e.g. Actors,
    /// Skills) represents a distinct data category, and there's never more than
    /// one tab for the same category. Since Myceliarium entries are discovered
    /// via reflection and a user could accidentally register the same entry type
    /// as a subentry of two different top-level parents (or both top-level and
    /// nested), this flags that case before it causes confusing duplicate tabs
    /// at runtime.
    /// </summary>
    public static class ControlPanelEntryTypeValidator
    {
        /// <summary>
        /// Pure logic, isolated so it can be unit tested without needing a live
        /// ControlPanel/EditorWindow.
        /// </summary>
        public static IList<Type> FindDuplicateEntryTypes(IList<IControlPanelEntry> entries)
        {
            var duplicates = new List<Type>();
            if (entries == null || entries.Count == 0)
            {
                return duplicates;
            }

            var seenTypes = new HashSet<Type>();
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                var entryType = entry.GetType();
                bool alreadySeen = !seenTypes.Add(entryType);
                if (alreadySeen && !duplicates.Contains(entryType))
                {
                    duplicates.Add(entryType);
                }
            }

            return duplicates;
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if any concrete entry
        /// type appears more than once in <paramref name="entries"/>.
        /// </summary>
        public static void AssertNoDuplicateEntryTypes(IList<IControlPanelEntry> entries)
        {
            var duplicates = FindDuplicateEntryTypes(entries);
            if (duplicates.Count == 0)
            {
                return;
            }

            var duplicateNames = string.Join(", ", GetNames(duplicates));
            throw new InvalidOperationException(
                $"Duplicate IControlPanelEntry types found: {duplicateNames}. " +
                $"Each entry type should only ever appear once across all top-level " +
                $"entries and their subentries within a single ControlPanel.");
        }

        private static IEnumerable<string> GetNames(IList<Type> types)
        {
            for (int i = 0; i < types.Count; i++)
            {
                yield return types[i].Name;
            }
        }
    }
}

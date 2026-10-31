using System;
using System.Collections.Generic;
using UnityEditor;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Remembers which entry a ControlPanel had selected last, so the panel can select
    /// it again when it's next opened. Where the answer is kept comes in through the
    /// constructor, so the logic can be unit tested without the editor.
    /// </summary>
    public sealed class LastSelectionMemory
    {
        /// <summary>
        /// Panels with fewer selectable entries than this don't get their selection
        /// restored. With just one entry there's nothing to choose between, and leaving
        /// those panels alone keeps manual selection easy to test in the editor.
        /// </summary>
        public const int MinEntriesForRestoring = 2;

        private const string PrefsKeyPrefix = "AtMycelia.Myceliarium.LastSelected.";

        /// <param name="read">Returns what was last written (null or empty if nothing).</param>
        /// <param name="write">Stores the given value.</param>
        public LastSelectionMemory(Func<string> read, Action<string> write)
        {
            _read = read ?? throw new ArgumentNullException(nameof(read));
            _write = write ?? throw new ArgumentNullException(nameof(write));
        }

        private readonly Func<string> _read;
        private readonly Action<string> _write;

        /// <summary>
        /// Keeps the answer in EditorPrefs (per panel type), so it belongs to the
        /// machine's user rather than the project: it survives the panel being closed,
        /// assembly reloads and editor restarts, and teammates each have their own.
        /// </summary>
        public static LastSelectionMemory InEditorPrefs(Type panelType)
        {
            string prefsKey = PrefsKeyPrefix + panelType.FullName;
            return new LastSelectionMemory(
                () => EditorPrefs.GetString(prefsKey, string.Empty),
                value => EditorPrefs.SetString(prefsKey, value));
        }

        public void Remember(IControlPanelEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            var key = KeyFor(entry);
            _write(key);
        }

        /// <summary>
        /// Picks the remembered entry out of the given ones. Returns null if the panel
        /// is too small to bother (see <see cref="MinEntriesForRestoring"/>), nothing was
        /// remembered, or the remembered entry isn't among them anymore.
        /// </summary>
        public IControlPanelEntry FindToRestore(IReadOnlyList<IControlPanelEntry> selectable)
        {
            bool tooFewToBother = selectable == null ||
                selectable.Count < MinEntriesForRestoring;
            if (tooFewToBother)
            {
                return null;
            }

            string remembered = _read();
            if (string.IsNullOrEmpty(remembered))
            {
                return null;
            }

            for (int i = 0; i < selectable.Count; i++)
            {
                var entry = selectable[i];
                if (KeyFor(entry) == remembered)
                {
                    return entry;
                }
            }
            return null;
        }

        // Entry types are unique within a panel (the type validator enforces it), and
        // unlike instances they stay the same across reloads.
        private static string KeyFor(IControlPanelEntry entry)
        {
            return entry.GetType().FullName;
        }
    }
}

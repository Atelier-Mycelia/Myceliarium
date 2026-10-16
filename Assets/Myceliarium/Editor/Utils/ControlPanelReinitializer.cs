using System;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// The sequence a ControlPanel goes through when its entries are reinitialized.
    /// Reinitting makes top-level entries recreate their subentries, so anything
    /// derived from the old instances (the flattened entry list, what's attached to
    /// the UI, the saver pairings, the selection) has to be refreshed in step.
    /// Isolated from ControlPanel so it can be unit tested without a live EditorWindow.
    /// </summary>
    public static class ControlPanelReinitializer
    {
        /// <param name="rebuildFlattened">Refreshes the entries' flattened list.</param>
        /// <param name="refreshSavers">Re-pairs the savers with the new entries.</param>
        public static void Reinit(
            EntryCollection entries,
            ControlPanelEntryAttacher attacher,
            ControlPanelTabSelectionController selectionController,
            Action rebuildFlattened,
            Action refreshSavers)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            if (attacher == null) throw new ArgumentNullException(nameof(attacher));
            if (selectionController == null)
            {
                throw new ArgumentNullException(nameof(selectionController));
            }

            var topLevel = entries.TopLevel;
            for (int i = 0; i < topLevel.Count; i++)
            {
                var entry = topLevel[i];
                entry.Dispose();
                entry.Init(forceReinit: true);
            }

            rebuildFlattened?.Invoke();
            // Top-level on purpose: the attacher skips everything else, and a top-level
            // entry with no subwindow of its own isn't in the flattened list.
            attacher.Attach(entries.TopLevel);
            refreshSavers?.Invoke();
            selectionController.OnAllEntriesReinitted();
        }
    }
}

using System;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// The sequence a ControlPanel goes through when its entries are reinitialized.
    /// Entries and subs get recycled instead of thrown away after their disposal, 
    /// but their tabs and subwindows still get rebuilt. Thus, the entries need to be
    /// re-attached. The one previously selected needs to be reselected, too. <br></br>
    /// Isolated this class from ControlPanel so it can be unit tested without a live EditorWindow.
    /// </summary>
    public static class ControlPanelReinitializer
    {
        public static void Reinit(
            EntryCollection entries,
            ControlPanelEntryAttacher attacher,
            ControlPanelTabSelectionController selectionController)
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

            // Top-level on purpose: the attacher skips everything else, and a top-level
            // entry with no subwindow of its own isn't in the flattened list.
            attacher.Attach(entries.TopLevel);
            selectionController.OnAllEntriesReinitted();
        }
    }
}

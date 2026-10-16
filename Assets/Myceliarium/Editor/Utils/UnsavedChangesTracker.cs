using System;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Keeps track of whether any entry of a ControlPanel has unsaved changes, and
    /// reports whenever that flips. Rather than polling, it re-checks when it hears
    /// about something that could change the answer: an entry reporting a change
    /// (<see cref="ControlPanelSignals.UnsavedChangesChanged"/>), or a save, load or
    /// tab click, which also catches entries that override HasUnsavedChanges without
    /// notifying. Has no dependency on EditorWindow, so it can be unit tested directly.
    /// </summary>
    public sealed class UnsavedChangesTracker : IDisposable
    {
        /// <param name="onDirtyChanged">Called with the new state whenever it changes.
        /// Also called once by Start with the initial state.</param>
        public UnsavedChangesTracker(EntryCollection entries, Action<bool> onDirtyChanged)
        {
            _entries = entries ?? throw new ArgumentNullException(nameof(entries));
            OnDirtyChanged = onDirtyChanged;
        }

        private readonly EntryCollection _entries;
        private readonly Action<bool> OnDirtyChanged = delegate { };

        /// <summary>
        /// Starts listening, and reports the current state right away.
        /// </summary>
        public void Start()
        {
            SetSubs(false); // So calling this twice doesn't double-subscribe
            SetSubs(true);
            _started = true;
            Refresh(force: true);
        }

        private void SetSubs(bool on)
        {
            if (on)
            {
                ControlPanelSignals.UnsavedChangesChanged += OnEntrySignal;
                ControlPanelSignals.SaveCompleted += OnEntrySignal;
                ControlPanelSignals.LoadCompleted += OnEntrySignal;
                ControlPanelSignals.EntryTabClicked += OnEntrySignal;
            }
            else
            {
                ControlPanelSignals.UnsavedChangesChanged -= OnEntrySignal;
                ControlPanelSignals.SaveCompleted -= OnEntrySignal;
                ControlPanelSignals.LoadCompleted -= OnEntrySignal;
                ControlPanelSignals.EntryTabClicked -= OnEntrySignal;
            }
        }

        // Deliberately not filtered down to this panel's entries: entries that aren't
        // in the flattened list (say, subentries with no subwindow) still count toward
        // the answer, and a re-check is cheap since this isn't running on a timer.
        private void OnEntrySignal(IControlPanelEntry entry)
        {
            Refresh();
        }

        /// <summary>
        /// Re-checks the entries. Mostly for after changes that don't produce a signal,
        /// like the entries having been reinitialized.
        /// </summary>
        public void Refresh(bool force = false)
        {
            bool dirty = _entries.AnyHaveUnsavedChanges();
            if (dirty == IsDirty && !force)
            {
                return;
            }

            IsDirty = dirty;
            OnDirtyChanged?.Invoke(dirty);
        }

        public bool IsDirty { get; private set; }

        private bool _started;

        public void Dispose()
        {
            if (!_started)
            {
                return;
            }

            SetSubs(false);
            _started = false;
        }

    }
}

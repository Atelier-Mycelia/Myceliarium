using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace AtMycelia.Myceliarium.Tests
{
    public class UnsavedChangesTrackerTests
    {
        private EntryCollection _entries;
        private FakeEntry _entry;
        private List<bool> _reports;
        private UnsavedChangesTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _entry = new FakeEntry();
            _entries = new EntryCollection();
            _entries.SetTopLevel(new List<IControlPanelEntry> { _entry });
            _reports = new List<bool>();
            _tracker = new UnsavedChangesTracker(_entries, _reports.Add);
        }

        [TearDown]
        public void TearDown()
        {
            _tracker.Dispose();
        }

        [Test]
        public void Start_ReportsTheInitialStateRightAway()
        {
            _tracker.Start();

            Assert.That(_reports, Is.EqualTo(new[] { false }));
        }

        [Test]
        public void Start_WithAlreadyDirtyEntries_ReportsDirty()
        {
            _entry.Dirty = true;

            _tracker.Start();

            Assert.That(_tracker.IsDirty, Is.True);
            Assert.That(_reports, Is.EqualTo(new[] { true }));
        }

        [Test]
        public void UnsavedChangesChangedSignal_ReportsNewState()
        {
            _tracker.Start();

            _entry.Dirty = true;
            ControlPanelSignals.UnsavedChangesChanged(_entry);
            _entry.Dirty = false;
            ControlPanelSignals.UnsavedChangesChanged(_entry);

            Assert.That(_reports, Is.EqualTo(new[] { false, true, false }));
        }

        [Test]
        public void Signal_WhenNothingChanged_DoesNotReportAgain()
        {
            _tracker.Start();

            ControlPanelSignals.UnsavedChangesChanged(_entry);
            ControlPanelSignals.UnsavedChangesChanged(_entry);

            Assert.That(_reports, Is.EqualTo(new[] { false }));
        }

        [Test]
        public void NestedEntryChanges_AreCounted()
        {
            var nested = new FakeNestedEntry();
            var parent = new FakeEntry(subentries: new List<IControlPanelEntry> { nested });
            _entries.SetTopLevel(new List<IControlPanelEntry> { parent });
            _tracker.Start();

            nested.Dirty = true;
            ControlPanelSignals.UnsavedChangesChanged(nested);

            Assert.That(_tracker.IsDirty, Is.True);
        }

        [TestCase("SaveCompleted")]
        [TestCase("LoadCompleted")]
        [TestCase("EntryTabClicked")]
        public void OtherSignals_AlsoTriggerARecheck(string signal)
        {
            _tracker.Start();
            _entry.Dirty = true; // As if an override changed without notifying

            switch (signal)
            {
                case "SaveCompleted": ControlPanelSignals.SaveCompleted(_entry); break;
                case "LoadCompleted": ControlPanelSignals.LoadCompleted(_entry); break;
                default: ControlPanelSignals.EntryTabClicked(_entry); break;
            }

            Assert.That(_tracker.IsDirty, Is.True);
        }

        [Test]
        public void Refresh_PicksUpChangesThatSignalledNothing()
        {
            _tracker.Start();
            _entry.Dirty = true;

            _tracker.Refresh();

            Assert.That(_reports, Is.EqualTo(new[] { false, true }));
        }

        [Test]
        public void Dispose_StopsListening()
        {
            _tracker.Start();
            _tracker.Dispose();

            _entry.Dirty = true;
            ControlPanelSignals.UnsavedChangesChanged(_entry);

            Assert.That(_reports, Is.EqualTo(new[] { false }));
        }

        [Test]
        public void Start_CalledTwice_DoesNotReportEachSignalTwice()
        {
            _tracker.Start();
            _tracker.Start();
            _reports.Clear();

            _entry.Dirty = true;
            ControlPanelSignals.UnsavedChangesChanged(_entry);

            Assert.That(_reports, Is.EqualTo(new[] { true }));
        }

        [Test]
        public void Dispose_WithoutStart_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _tracker.Dispose());
        }

        [Test]
        public void NullEntries_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new UnsavedChangesTracker(null, _ => { }));
        }

        #region Test Doubles
        private class FakeNestedEntry : FakeEntry { }

        private class FakeEntry : IControlPanelEntry
        {
            private readonly IReadOnlyList<IControlPanelEntry> _subentries;

            public FakeEntry(IReadOnlyList<IControlPanelEntry> subentries = null)
            {
                _subentries = subentries ?? Array.Empty<IControlPanelEntry>();
            }

            public bool Dirty { get; set; }

            public bool IsTestOnly => true;
            public int SortingOrder => 0;
            public string SortingName => string.Empty;
            public IControlPanelTab Tab => null;
            public IControlPanelSubwindow Subwindow => null;
            public bool IsTopLevel => true;
            public bool IsMeantToHaveSubwindow => false;
            public bool IsInitted => false;
            public bool HasSubentries => _subentries.Count > 0;
            public bool HasUnsavedChanges => Dirty;
            public IControlPanelEntryLoader Loader => null;

            public void Dispose() { }
            public void Init(bool forceReinit = false) { }
            public IReadOnlyList<IControlPanelEntry> GetSubentries(bool recursive = false) =>
                _subentries;
            public void RemoveFromHierarchy() { }
            public void Select() { }
            public void Deselect() { }
        }
        #endregion
    }
}

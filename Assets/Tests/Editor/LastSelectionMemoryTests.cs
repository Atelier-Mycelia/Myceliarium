using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace AtMycelia.Myceliarium.Tests
{
    public class LastSelectionMemoryTests
    {
        private string _stored;
        private LastSelectionMemory _memory;

        [SetUp]
        public void SetUp()
        {
            _stored = null;
            _memory = new LastSelectionMemory(() => _stored, value => _stored = value);
        }

        private static List<IControlPanelEntry> Entries(params IControlPanelEntry[] entries)
        {
            return new List<IControlPanelEntry>(entries);
        }

        [Test]
        public void FindToRestore_NothingRemembered_ReturnsNull()
        {
            var entries = Entries(new EntryA(), new EntryB());

            Assert.That(_memory.FindToRestore(entries), Is.Null);
        }

        [Test]
        public void FindToRestore_AfterRemembering_ReturnsThatEntry()
        {
            var a = new EntryA();
            var b = new EntryB();
            _memory.Remember(b);

            Assert.That(_memory.FindToRestore(Entries(a, b)), Is.SameAs(b));
        }

        [Test]
        public void FindToRestore_MatchesByTypeSoItWorksWithFreshInstances()
        {
            _memory.Remember(new EntryB());
            var freshB = new EntryB();

            Assert.That(_memory.FindToRestore(Entries(new EntryA(), freshB)),
                Is.SameAs(freshB));
        }

        [Test]
        public void FindToRestore_RememberedEntryIsGone_ReturnsNull()
        {
            _memory.Remember(new EntryC());

            Assert.That(_memory.FindToRestore(Entries(new EntryA(), new EntryB())), Is.Null);
        }

        [Test]
        public void FindToRestore_FewerThanTheMinimumEntries_ReturnsNull()
        {
            var only = new EntryA();
            _memory.Remember(only);

            Assert.That(_memory.FindToRestore(Entries(only)), Is.Null);
        }

        [Test]
        public void FindToRestore_ExactlyTheMinimumEntries_Restores()
        {
            var a = new EntryA();
            var b = new EntryB();
            _memory.Remember(a);

            Assert.That(LastSelectionMemory.MinEntriesForRestoring, Is.EqualTo(2));
            Assert.That(_memory.FindToRestore(Entries(a, b)), Is.SameAs(a));
        }

        [Test]
        public void FindToRestore_NullList_ReturnsNull()
        {
            _memory.Remember(new EntryA());

            Assert.That(_memory.FindToRestore(null), Is.Null);
        }

        [Test]
        public void Remember_Null_IsIgnored()
        {
            _memory.Remember(new EntryA());

            _memory.Remember(null);

            Assert.That(_memory.FindToRestore(Entries(new EntryA(), new EntryB())),
                Is.Not.Null);
        }

        [Test]
        public void Remember_KeepsOnlyTheLatest()
        {
            _memory.Remember(new EntryA());
            _memory.Remember(new EntryB());
            var a = new EntryA();
            var b = new EntryB();

            Assert.That(_memory.FindToRestore(Entries(a, b)), Is.SameAs(b));
        }

        [Test]
        public void Constructor_NullDelegates_Throw()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new LastSelectionMemory(null, _ => { }));
            Assert.Throws<ArgumentNullException>(() =>
                new LastSelectionMemory(() => null, null));
        }

        #region Test Doubles
        // Distinct types, since entries are told apart by their type
        private class EntryA : FakeEntry { }
        private class EntryB : FakeEntry { }
        private class EntryC : FakeEntry { }

        private abstract class FakeEntry : IControlPanelEntry
        {
            public int SortingOrder => 0;
            public string SortingName => string.Empty;
            public IControlPanelTab Tab => null;
            public IControlPanelSubwindow Subwindow => null;
            public bool IsTopLevel => true;
            public bool IsMeantToHaveSubwindow => true;
            public bool IsInitted => true;
            public bool IsLoaded => true;
            public bool IsTestOnly => true;
            public bool HasSubentries => false;
            public bool HasUnsavedChanges => false;
            public IControlPanelEntryLoader Loader => null;

            public void Dispose() { }
            public void Init(bool forceReinit = false) { }

            public IReadOnlyList<IControlPanelEntry> GetSubentries(bool recursive = false)
            {
                return Array.Empty<IControlPanelEntry>();
            }

            public void RemoveFromHierarchy() { }
            public void Select() { }
            public void Deselect() { }
        }
        #endregion
    }
}

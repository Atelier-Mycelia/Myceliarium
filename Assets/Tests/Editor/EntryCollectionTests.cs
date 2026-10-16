using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace AtMycelia.Myceliarium.Tests
{
    public class EntryCollectionTests
    {
        [Test]
        public void SetTopLevel_KeepsOnlyTopLevelEntries()
        {
            var top = new FakeEntry(isTopLevel: true);
            var nested = new FakeEntry();
            var collection = new EntryCollection();

            collection.SetTopLevel(new List<IControlPanelEntry> { top, nested });

            Assert.That(collection.TopLevel, Is.EqualTo(new[] { top }));
        }

        [Test]
        public void SetTopLevel_ReplacesPreviousEntries()
        {
            var first = new FakeEntry(isTopLevel: true);
            var second = new FakeEntry(isTopLevel: true);
            var collection = new EntryCollection();
            collection.SetTopLevel(new List<IControlPanelEntry> { first });

            collection.SetTopLevel(new List<IControlPanelEntry> { second });

            Assert.That(collection.TopLevel, Is.EqualTo(new[] { second }));
            Assert.That(collection.Contains(first), Is.False);
        }

        [Test]
        public void SortTopLevel_DefaultsToSortingOrderThenName()
        {
            var late = new FakeEntry(isTopLevel: true, sortingOrder: 2);
            var earlyB = new FakeEntry(isTopLevel: true, sortingOrder: 1, sortingName: "B");
            var earlyA = new FakeEntry(isTopLevel: true, sortingOrder: 1, sortingName: "A");
            var collection = new EntryCollection();
            collection.SetTopLevel(new List<IControlPanelEntry> { late, earlyB, earlyA });

            collection.SortTopLevel();

            Assert.That(collection.TopLevel, Is.EqualTo(new[] { earlyA, earlyB, late }));
        }

        [Test]
        public void SortTopLevel_UsesCustomSorter()
        {
            var a = new FakeEntry(isTopLevel: true, sortingOrder: 1);
            var b = new FakeEntry(isTopLevel: true, sortingOrder: 2);
            var collection = new EntryCollection();
            collection.SetTopLevel(new List<IControlPanelEntry> { a, b });

            collection.SortTopLevel(entries => ((List<IControlPanelEntry>)entries).Reverse());

            Assert.That(collection.TopLevel, Is.EqualTo(new[] { b, a }));
        }

        [Test]
        public void RebuildFlattened_IncludesTopLevelAndNestedEntriesWithSubwindows()
        {
            var nested = new FakeNestedEntry();
            var noWindow = new FakeEntry();
            var top = new FakeEntry(isTopLevel: true, isMeantToHaveSubwindow: true,
                subentries: new List<IControlPanelEntry> { nested, noWindow });
            var collection = new EntryCollection();
            collection.SetTopLevel(new List<IControlPanelEntry> { top });

            collection.RebuildFlattened();

            Assert.That(collection.AllWithSubwindows, Is.EqualTo(new[] { top, nested }));
            Assert.That(collection.Contains(nested), Is.True);
            Assert.That(collection.Contains(noWindow), Is.False);
        }

        [Test]
        public void RebuildFlattened_CalledTwice_DropsStaleEntries()
        {
            var oldTop = new FakeEntry(isTopLevel: true, isMeantToHaveSubwindow: true);
            var newTop = new FakeEntry(isTopLevel: true, isMeantToHaveSubwindow: true);
            var collection = new EntryCollection();
            collection.SetTopLevel(new List<IControlPanelEntry> { oldTop });
            collection.RebuildFlattened();

            collection.SetTopLevel(new List<IControlPanelEntry> { newTop });
            collection.RebuildFlattened();

            Assert.That(collection.AllWithSubwindows, Is.EqualTo(new[] { newTop }));
            Assert.That(collection.Contains(oldTop), Is.False);
        }

        [Test]
        public void RebuildFlattened_DuplicateEntryTypes_Throws()
        {
            var a = new FakeEntry(isTopLevel: true, isMeantToHaveSubwindow: true);
            var b = new FakeEntry(isTopLevel: true, isMeantToHaveSubwindow: true);
            var collection = new EntryCollection();
            collection.SetTopLevel(new List<IControlPanelEntry> { a, b });

            Assert.Throws<InvalidOperationException>(() => collection.RebuildFlattened());
        }

        [Test]
        public void GetAll_ReturnsTopLevelFollowedByNestedWithoutRepeats()
        {
            var nested = new FakeNestedEntry();
            var top = new FakeEntry(isTopLevel: true, isMeantToHaveSubwindow: true,
                subentries: new List<IControlPanelEntry> { nested });
            var collection = new EntryCollection();
            collection.SetTopLevel(new List<IControlPanelEntry> { top });
            collection.RebuildFlattened();

            Assert.That(collection.GetAll(), Is.EqualTo(new IControlPanelEntry[] { top, nested }));
        }

        [Test]
        public void Contains_Null_ReturnsFalse()
        {
            Assert.That(new EntryCollection().Contains(null), Is.False);
        }

        [Test]
        public void AnyHaveUnsavedChanges_ChecksNestedEntries()
        {
            var dirtyNested = new FakeEntry(hasUnsavedChanges: true);
            var top = new FakeEntry(isTopLevel: true,
                subentries: new List<IControlPanelEntry> { dirtyNested });
            var collection = new EntryCollection();
            collection.SetTopLevel(new List<IControlPanelEntry> { top });

            Assert.That(collection.AnyHaveUnsavedChanges(), Is.True);
        }

        #region Test Doubles
        private class FakeNestedEntry : FakeEntry
        {
            public FakeNestedEntry() : base(isMeantToHaveSubwindow: true) { }
        }

        private class FakeEntry : IControlPanelEntry
        {
            private readonly IReadOnlyList<IControlPanelEntry> _subentries;

            public FakeEntry(bool isTopLevel = false, int sortingOrder = 0,
                string sortingName = "", IReadOnlyList<IControlPanelEntry> subentries = null,
                bool isMeantToHaveSubwindow = false, bool hasUnsavedChanges = false)
            {
                IsTopLevel = isTopLevel;
                SortingOrder = sortingOrder;
                SortingName = sortingName;
                _subentries = subentries ?? Array.Empty<IControlPanelEntry>();
                IsMeantToHaveSubwindow = isMeantToHaveSubwindow;
                HasUnsavedChanges = hasUnsavedChanges;
            }

            public bool IsTestOnly => true;
            public int SortingOrder { get; }
            public string SortingName { get; }
            public IControlPanelTab Tab => null;
            public IControlPanelSubwindow Subwindow => null;
            public bool IsTopLevel { get; }
            public bool IsMeantToHaveSubwindow { get; }
            public bool IsInitted => false;
            public bool HasSubentries => _subentries.Count > 0;
            public bool HasUnsavedChanges { get; }

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

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace AtMycelia.Myceliarium.Tests
{
    public class ControlPanelPureLogicTests
    {
        #region FilterTopLevelEntries
        [Test]
        public void FilterTopLevelEntries_OnlyIncludesTopLevelEntries()
        {
            var topLevel = new FakeEntry(isTopLevel: true);
            var nested = new FakeEntry(isTopLevel: false);
            var toCheck = new List<IControlPanelEntry> { topLevel, nested };

            var result = ControlPanel.FilterTopLevelEntries(toCheck);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0], Is.SameAs(topLevel));
        }

        [Test]
        public void FilterTopLevelEntries_IgnoresNullEntriesInList()
        {
            var topLevel = new FakeEntry(isTopLevel: true);
            var toCheck = new List<IControlPanelEntry> { null, topLevel };

            var result = ControlPanel.FilterTopLevelEntries(toCheck);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0], Is.SameAs(topLevel));
        }

        [Test]
        public void FilterTopLevelEntries_NullInput_ReturnsEmptyList()
        {
            var result = ControlPanel.FilterTopLevelEntries(null);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void FilterTopLevelEntries_EmptyInput_ReturnsEmptyList()
        {
            var result = ControlPanel.FilterTopLevelEntries(new List<IControlPanelEntry>());

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void FilterTopLevelEntries_NoneTopLevel_ReturnsEmptyList()
        {
            var toCheck = new List<IControlPanelEntry>
            {
                new FakeEntry(isTopLevel: false),
                new FakeEntry(isTopLevel: false)
            };

            var result = ControlPanel.FilterTopLevelEntries(toCheck);

            Assert.That(result, Is.Empty);
        }
        #endregion

        #region FilterCompatibleEntries
        [Test]
        public void FilterCompatibleEntries_OnlyIncludesMatchingPredicate()
        {
            var compatible = new FakeEntry(sortingName: "Compatible");
            var incompatible = new FakeEntry(sortingName: "Incompatible");
            var allEntries = new List<IControlPanelEntry> { compatible, incompatible };

            var result = ControlPanel.FilterCompatibleEntries(
                allEntries, entry => entry == compatible);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0], Is.SameAs(compatible));
        }

        [Test]
        public void FilterCompatibleEntries_NullAllEntries_ReturnsEmptyList()
        {
            var result = ControlPanel.FilterCompatibleEntries(null, entry => true);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void FilterCompatibleEntries_NullPredicate_ReturnsEmptyList()
        {
            var allEntries = new List<IControlPanelEntry> { new FakeEntry() };

            var result = ControlPanel.FilterCompatibleEntries(allEntries, null);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void FilterCompatibleEntries_NoMatches_ReturnsEmptyList()
        {
            var allEntries = new List<IControlPanelEntry> { new FakeEntry() };

            var result = ControlPanel.FilterCompatibleEntries(allEntries, entry => false);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void FilterCompatibleEntries_AllMatch_ReturnsAllInOriginalOrder()
        {
            var first = new FakeEntry(sortingName: "First");
            var second = new FakeEntry(sortingName: "Second");
            var allEntries = new List<IControlPanelEntry> { first, second };

            var result = ControlPanel.FilterCompatibleEntries(allEntries, entry => true);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0], Is.SameAs(first));
            Assert.That(result[1], Is.SameAs(second));
        }
        #endregion

        #region CompareEntries
        [Test]
        public void CompareEntries_DifferingSortingOrder_ComparesBySortingOrder()
        {
            var lower = new FakeEntry(sortingOrder: 1);
            var higher = new FakeEntry(sortingOrder: 2);

            Assert.That(ControlPanel.CompareEntries(lower, higher), Is.LessThan(0));
            Assert.That(ControlPanel.CompareEntries(higher, lower), Is.GreaterThan(0));
        }

        [Test]
        public void CompareEntries_SameSortingOrder_FallsBackToSortingNameOrdinalComparison()
        {
            var a = new FakeEntry(sortingOrder: 5, sortingName: "Alpha");
            var b = new FakeEntry(sortingOrder: 5, sortingName: "Beta");

            Assert.That(ControlPanel.CompareEntries(a, b), Is.LessThan(0));
            Assert.That(ControlPanel.CompareEntries(b, a), Is.GreaterThan(0));
        }

        [Test]
        public void CompareEntries_SameSortingOrderAndName_ReturnsZero()
        {
            var a = new FakeEntry(sortingOrder: 5, sortingName: "Same");
            var b = new FakeEntry(sortingOrder: 5, sortingName: "Same");

            Assert.That(ControlPanel.CompareEntries(a, b), Is.EqualTo(0));
        }

        [Test]
        public void CompareEntries_UsedWithListSort_ProducesStableOrdering()
        {
            var entries = new List<IControlPanelEntry>
            {
                new FakeEntry(sortingOrder: 2, sortingName: "B"),
                new FakeEntry(sortingOrder: 1, sortingName: "Z"),
                new FakeEntry(sortingOrder: 1, sortingName: "A"),
            };

            entries.Sort(ControlPanel.CompareEntries);

            Assert.That(entries[0].SortingName, Is.EqualTo("A"));
            Assert.That(entries[1].SortingName, Is.EqualTo("Z"));
            Assert.That(entries[2].SortingName, Is.EqualTo("B"));
        }
        #endregion

        #region CollectAllEntries
        [Test]
        public void CollectAllEntries_CollectsRecursiveSubentriesFromEachTopLevelEntry()
        {
            var subA = new FakeEntry(sortingName: "SubA", isMeantToHaveSubwindow: true);
            var subB = new FakeEntry(sortingName: "SubB", isMeantToHaveSubwindow: true);
            var topA = new FakeEntry(sortingName: "TopA", subentries: new List<IControlPanelEntry> { subA });
            var topB = new FakeEntry(sortingName: "TopB", subentries: new List<IControlPanelEntry> { subB });

            var result = ControlPanel.CollectAllEntriesWithSubwindows(
                new List<IControlPanelEntry> { topA, topB });

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result, Does.Contain(subA));
            Assert.That(result, Does.Contain(subB));
        }

        [Test]
        public void CollectAllEntries_DeduplicatesSubentriesSharedAcrossTopLevelEntries()
        {
            var sharedSub = new FakeEntry(sortingName: "Shared", isMeantToHaveSubwindow: true);
            var topA = new FakeEntry(subentries: new List<IControlPanelEntry> { sharedSub });
            var topB = new FakeEntry(subentries: new List<IControlPanelEntry> { sharedSub });

            var result = ControlPanel.CollectAllEntriesWithSubwindows(
                new List<IControlPanelEntry> { topA, topB });

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0], Is.SameAs(sharedSub));
        }

        [Test]
        public void CollectAllEntries_NullInput_ReturnsEmptyList()
        {
            var result = ControlPanel.CollectAllEntriesWithSubwindows(null);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void CollectAllEntries_TopLevelEntryWithNoSubentries_ReturnsEmptyList()
        {
            var top = new FakeEntry(subentries: new List<IControlPanelEntry>());

            var result = ControlPanel.CollectAllEntriesWithSubwindows(
                new List<IControlPanelEntry> { top });

            Assert.That(result, Is.Empty);
        }
        #endregion

        #region Test Double
        private class FakeEntry : IControlPanelEntry
        {
            public virtual bool IsTestOnly => true;
            private readonly IReadOnlyList<IControlPanelEntry> _subentries;

            public FakeEntry()
            {
                IsTopLevel = false;
                SortingOrder = 0;
                SortingName = string.Empty;
                _subentries = Array.Empty<IControlPanelEntry>();
                IsMeantToHaveSubwindow = false;
            }

            public FakeEntry(bool isTopLevel = false, int sortingOrder = 0,
                string sortingName = "", IReadOnlyList<IControlPanelEntry> subentries = null,
                bool isMeantToHaveSubwindow = false)
            {
                IsTopLevel = isTopLevel;
                SortingOrder = sortingOrder;
                SortingName = sortingName;
                _subentries = subentries ?? Array.Empty<IControlPanelEntry>();
                IsMeantToHaveSubwindow = isMeantToHaveSubwindow;
            }

            public int SortingOrder { get; }
            public string SortingName { get; }
            public IControlPanelTab Tab => null;
            public IControlPanelSubwindow Subwindow => null;
            public bool IsTopLevel { get; }
            public bool IsMeantToHaveSubwindow { get; }
            public bool IsInitted => false;
            public bool HasSubentries => _subentries.Count > 0;

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

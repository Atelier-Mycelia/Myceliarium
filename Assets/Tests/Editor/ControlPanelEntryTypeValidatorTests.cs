using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace AtMycelia.Myceliarium.Tests
{
    public class ControlPanelEntryTypeValidatorTests
    {
        #region FindDuplicateEntryTypes
        [Test]
        public void FindDuplicateEntryTypes_NoDuplicates_ReturnsEmptyList()
        {
            var entries = new List<IControlPanelEntry> { new EntryA(), new EntryB() };

            var result = ControlPanelEntryTypeValidator.FindDuplicateEntryTypes(entries);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void FindDuplicateEntryTypes_SameConcreteTypeTwice_ReturnsThatType()
        {
            var entries = new List<IControlPanelEntry> { new EntryA(), new EntryA() };

            var result = ControlPanelEntryTypeValidator.FindDuplicateEntryTypes(entries);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0], Is.EqualTo(typeof(EntryA)));
        }

        [Test]
        public void FindDuplicateEntryTypes_SameTypeMultipleTimes_ReturnsTypeOnlyOnce()
        {
            var entries = new List<IControlPanelEntry>
            {
                new EntryA(), new EntryA(), new EntryA()
            };

            var result = ControlPanelEntryTypeValidator.FindDuplicateEntryTypes(entries);

            Assert.That(result.Count, Is.EqualTo(1));
        }

        [Test]
        public void FindDuplicateEntryTypes_MultipleDuplicateTypes_ReturnsAllOfThem()
        {
            var entries = new List<IControlPanelEntry>
            {
                new EntryA(), new EntryA(),
                new EntryB(), new EntryB()
            };

            var result = ControlPanelEntryTypeValidator.FindDuplicateEntryTypes(entries);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result, Does.Contain(typeof(EntryA)));
            Assert.That(result, Does.Contain(typeof(EntryB)));
        }

        [Test]
        public void FindDuplicateEntryTypes_IgnoresNullEntries()
        {
            var entries = new List<IControlPanelEntry> { null, new EntryA(), null };

            var result = ControlPanelEntryTypeValidator.FindDuplicateEntryTypes(entries);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void FindDuplicateEntryTypes_NullInput_ReturnsEmptyList()
        {
            var result = ControlPanelEntryTypeValidator.FindDuplicateEntryTypes(null);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void FindDuplicateEntryTypes_EmptyInput_ReturnsEmptyList()
        {
            var result = ControlPanelEntryTypeValidator.FindDuplicateEntryTypes(
                new List<IControlPanelEntry>());

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void FindDuplicateEntryTypes_DifferentConcreteTypesImplementingSameInterface_AreNotDuplicates()
        {
            // EntryA and EntryB both implement IControlPanelEntry directly but are
            // distinct concrete types, so they should not be flagged against each other.
            var entries = new List<IControlPanelEntry> { new EntryA(), new EntryB() };

            var result = ControlPanelEntryTypeValidator.FindDuplicateEntryTypes(entries);

            Assert.That(result, Is.Empty);
        }
        #endregion

        #region AssertNoDuplicateEntryTypes
        [Test]
        public void AssertNoDuplicateEntryTypes_NoDuplicates_DoesNotThrow()
        {
            var entries = new List<IControlPanelEntry> { new EntryA(), new EntryB() };

            Assert.DoesNotThrow(() =>
                ControlPanelEntryTypeValidator.AssertNoDuplicateEntryTypes(entries));
        }

        [Test]
        public void AssertNoDuplicateEntryTypes_HasDuplicates_ThrowsInvalidOperationException()
        {
            var entries = new List<IControlPanelEntry> { new EntryA(), new EntryA() };

            var ex = Assert.Throws<InvalidOperationException>(() =>
                ControlPanelEntryTypeValidator.AssertNoDuplicateEntryTypes(entries));

            Assert.That(ex.Message, Does.Contain(nameof(EntryA)));
        }

        [Test]
        public void AssertNoDuplicateEntryTypes_NullInput_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                ControlPanelEntryTypeValidator.AssertNoDuplicateEntryTypes(null));
        }
        #endregion

        #region Test Doubles
        private class EntryA : IControlPanelEntry
        {
            public virtual bool IsTestOnly => true;
            public int SortingOrder => 0;
            public string SortingName => nameof(EntryA);
            public IControlPanelTab Tab => null;
            public IControlPanelSubwindow Subwindow => null;
            public bool IsTopLevel => false;
            public bool IsMeantToHaveSubwindow => false;
            public bool IsInitted => false;
            public bool HasSubentries => false;

            public void Init(bool forceReinit = false) { }
            public IReadOnlyList<IControlPanelEntry> GetSubentries(bool recursive = false) =>
                Array.Empty<IControlPanelEntry>();
            public void RemoveFromHierarchy() { }
            public void Select() { }
            public void Deselect() { }
        }

        private class EntryB : IControlPanelEntry
        {
            public virtual bool IsTestOnly => true;
            public int SortingOrder => 0;
            public string SortingName => nameof(EntryB);
            public IControlPanelTab Tab => null;
            public IControlPanelSubwindow Subwindow => null;
            public bool IsTopLevel => false;
            public bool IsMeantToHaveSubwindow => false;
            public bool IsInitted => false;
            public bool HasSubentries => false;

            public void Init(bool forceReinit = false) { }
            public IReadOnlyList<IControlPanelEntry> GetSubentries(bool recursive = false) =>
                Array.Empty<IControlPanelEntry>();
            public void RemoveFromHierarchy() { }
            public void Select() { }
            public void Deselect() { }
        }
        #endregion
    }
}

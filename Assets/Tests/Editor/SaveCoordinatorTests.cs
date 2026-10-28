using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace AtMycelia.Myceliarium.Tests
{
    public class SaveCoordinatorTests
    {
        [Test]
        public void Refresh_PairsEachSaverWithItsCompatibleEntries()
        {
            var a = new FakeEntry();
            var b = new FakeEntry();
            var saver = new FakeSaver(entry => entry == a);
            var coordinator = new SaveCoordinator();

            coordinator.Refresh(new List<IControlPanelEntrySaver> { saver },
                new List<IControlPanelEntry> { a, b });

            Assert.That(coordinator.Savers.Count, Is.EqualTo(1));
            Assert.That(coordinator.Savers[saver], Is.EqualTo(new[] { a }));
        }

        [Test]
        public void Refresh_ReplacesPreviousState()
        {
            var entry = new FakeEntry();
            var coordinator = new SaveCoordinator();
            coordinator.Refresh(new List<IControlPanelEntrySaver> { new FakeSaver(e => true) },
                new List<IControlPanelEntry> { entry });

            coordinator.Refresh(new List<IControlPanelEntrySaver>(),
                new List<IControlPanelEntry> { entry });

            Assert.That(coordinator.Savers, Is.Empty);
        }

        [Test]
        public void Refresh_NullSavers_LeavesCoordinatorEmpty()
        {
            var coordinator = new SaveCoordinator();

            Assert.DoesNotThrow(() => coordinator.Refresh(
                (IList<IControlPanelEntrySaver>)null, new List<IControlPanelEntry>()));
            Assert.That(coordinator.Savers, Is.Empty);
        }

        [Test]
        public void SaveAll_SavesOnlyCompatibleEntriesPerSaver()
        {
            var a = new FakeEntry();
            var b = new FakeEntry();
            var saverA = new FakeSaver(entry => entry == a);
            var saverB = new FakeSaver(entry => true);
            var coordinator = new SaveCoordinator();
            coordinator.Refresh(new List<IControlPanelEntrySaver> { saverA, saverB },
                new List<IControlPanelEntry> { a, b });

            coordinator.SaveAll();

            Assert.That(saverA.Saved, Is.EqualTo(new[] { a }));
            Assert.That(saverB.Saved, Is.EqualTo(new IControlPanelEntry[] { a, b }));
        }

        [Test]
        public void SaveAll_WithoutRefresh_DoesNothing()
        {
            Assert.DoesNotThrow(() => new SaveCoordinator().SaveAll());
        }

        #region Test Doubles
        private class FakeSaver : IControlPanelEntrySaver
        {
            private readonly Func<IControlPanelEntry, bool> _isCompatible;
            public readonly List<IControlPanelEntry> Saved = new List<IControlPanelEntry>();

            // The parameterless ctor is needed because CpEntrySaverReg's TypeCache scan
            // picks this class up and tries to instantiate it.
            public FakeSaver() : this(entry => true) { }

            public FakeSaver(Func<IControlPanelEntry, bool> isCompatible)
            {
                _isCompatible = isCompatible;
            }

            public void Save(IControlPanelEntry toSaveFor, Action onComplete = null)
            {
                Saved.Add(toSaveFor);
                onComplete?.Invoke();
            }

            public bool IsCompatibleWith(IControlPanelEntry toSaveFor) => _isCompatible(toSaveFor);
        }

        private class FakeEntry : IControlPanelEntry
        {
            public bool IsTestOnly => true;
            public int SortingOrder => 0;
            public string SortingName => string.Empty;
            public IControlPanelTab Tab => null;
            public IControlPanelSubwindow Subwindow => null;
            public bool IsTopLevel => false;
            public bool IsMeantToHaveSubwindow => false;
            public bool IsInitted => false;
            public bool HasSubentries => false;
            public bool HasUnsavedChanges => false;
            public IControlPanelEntryLoader Loader => null;

            public void Dispose() { }

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

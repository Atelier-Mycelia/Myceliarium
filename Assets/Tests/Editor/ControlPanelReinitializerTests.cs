using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium.Tests
{
    /// <summary>
    /// Runs the reinit sequence against real ControlPanelEntry instances laid out like
    /// MyceliariumRootEntry / MyceliariumAboutEntry: a top-level entry with no subwindow
    /// that has a subentry.
    /// </summary>
    public class ControlPanelReinitializerTests
    {
        private EntryCollection _entries;
        private ControlPanelEntryAttacher _attacher;
        private ControlPanelTabSelectionController _selection;
        private SaveCoordinator _saveCoordinator;
        private VisualElement _mainTabSet;
        private ScrollView _subwindowDisplay;
        private FakeSaver _saver;
        private RootEntry _root;

        [SetUp]
        public void SetUp()
        {
            var uiRoot = new VisualElement();
            _mainTabSet = new VisualElement { name = "MainTabSet" };
            _subwindowDisplay = new ScrollView { name = "CategorySubwindowDisplay" };
            uiRoot.Add(_mainTabSet);
            uiRoot.Add(_subwindowDisplay);

            _entries = new EntryCollection();
            _attacher = new ControlPanelEntryAttacher();
            _selection = new ControlPanelTabSelectionController();
            _saveCoordinator = new SaveCoordinator();
            _saver = new FakeSaver();
            _root = new RootEntry();

            _attacher.Init(uiRoot);
            _entries.SetTopLevel(new List<IControlPanelEntry> { _root });
            _attacher.Attach(_entries.TopLevel);
            RebuildFlattened();
            _selection.Init(_entries);
            RefreshSavers();
        }

        [TearDown]
        public void TearDown()
        {
            _selection.Dispose();
            _attacher.Dispose();
            _root.Dispose();
            ControlPanelSignals.EntryTabClicked = delegate { };
        }

        private void RebuildFlattened() => _entries.RebuildFlattened();

        private void RefreshSavers() => _saveCoordinator.Refresh(
            new List<IControlPanelEntrySaver> { _saver }, _entries.AllWithSubwindows);

        private void Reinit()
        {
            ControlPanelReinitializer.Reinit(_entries, _attacher, _selection);
        }

        private ChildEntry CurrentChild => (ChildEntry)_root.GetSubentries()[0];

        [Test]
        public void Reinit_ReattachesTopLevelEntryTab()
        {
            Reinit();

            Assert.That(_mainTabSet.Contains(_root.Tab.Root), Is.True);
        }

        [Test]
        public void Reinit_ReattachesSubentrySubwindow()
        {
            Reinit();

            Assert.That(_subwindowDisplay.Contains(CurrentChild.Subwindow.Root), Is.True);
        }

        [Test]
        public void Reinit_LeavesNoStaleSubwindowsBehind()
        {
            var oldSubwindowRoot = CurrentChild.Subwindow.Root;

            Reinit();

            Assert.That(_subwindowDisplay.Contains(oldSubwindowRoot), Is.False);
        }

        [Test]
        public void Reinit_KeepsTheSameSubentryInstanceInTheCollection()
        {
            var oldChild = CurrentChild;

            Reinit();

            Assert.That(CurrentChild, Is.SameAs(oldChild));
            Assert.That(_entries.Contains(oldChild), Is.True);
        }

        [Test]
        public void Reinit_ReinitializesTheSubentry()
        {
            var oldTab = CurrentChild.Tab;

            Reinit();

            Assert.That(CurrentChild.IsInitted, Is.True);
            Assert.That(CurrentChild.Tab, Is.Not.SameAs(oldTab));
        }

        [Test]
        public void Reinit_LeavesSaversPairedWithTheEntries()
        {
            Reinit();

            Assert.That(_saveCoordinator.Savers[_saver], Has.Member(CurrentChild));
        }

        [Test]
        public void Reinit_ReselectsTheSelectedEntry()
        {
            ControlPanelSignals.EntryTabClicked(CurrentChild);
            Assert.That(CurrentChild.Tab.IsSelected, Is.True);

            Reinit();

            Assert.That(CurrentChild.Tab.IsSelected, Is.True);
            Assert.That(((FakeSubwindow)CurrentChild.Subwindow).IsShown, Is.True);
        }

        [Test]
        public void Reinit_NothingSelectedBefore_DoesNotSelectAnything()
        {
            Reinit();

            Assert.That(CurrentChild.Tab.IsSelected, Is.False);
        }

        [Test]
        public void Reinit_CalledTwice_StillKeepsEverythingAttached()
        {
            Reinit();
            Reinit();

            Assert.That(_mainTabSet.Contains(_root.Tab.Root), Is.True);
            Assert.That(_subwindowDisplay.Contains(CurrentChild.Subwindow.Root), Is.True);
            Assert.That(_subwindowDisplay.contentContainer.childCount, Is.EqualTo(1));
        }

        [Test]
        public void Reinit_NullEntries_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ControlPanelReinitializer.Reinit(null, _attacher, _selection));
        }

        #region Test Doubles
        private class RootEntry : ControlPanelEntry
        {
            public override bool IsTopLevel => true;
            public override bool IsMeantToHaveSubwindow => false;
            public override string SortingName => nameof(RootEntry);

            protected override void PrepareLeftSidebarTab() => Tab = new FakeTab();

            protected override IEnumerable<IControlPanelEntry> CreateSubentries()
            {
                return new IControlPanelEntry[] { new ChildEntry() };
            }
        }

        private class ChildEntry : ControlPanelEntry
        {
            public override string SortingName => nameof(ChildEntry);

            protected override void PrepareLeftSidebarTab() => Tab = new FakeTab();

            protected override void PrepareSubwindow()
            {
                _subwindow ??= new FakeSubwindow();
                _subwindow.Init();
            }

            // No loader, so selecting finishes loading synchronously.
            protected override void PrepareLoader() => Loader = null;
        }

        private class FakeSaver : IControlPanelEntrySaver
        {
            public void Save(IControlPanelEntry toSaveFor, Action onComplete = null) { }
            public bool IsCompatibleWith(IControlPanelEntry toSaveFor) => true;
        }

        private class FakeTab : IControlPanelTab
        {
            public VisualElement Root { get; } = new VisualElement();
            public string PathToUxml => string.Empty;
            public string DisplayName => "Fake";
            public string Text { get; set; }
            public bool IsSelected { get; set; }
            public IReadOnlyList<IControlPanelTab> Subtabs => Array.Empty<IControlPanelTab>();

            public event Action<IControlPanelTab> Clicked;

            // Like the real tab, disposing takes the Root out of the hierarchy.
            public void Dispose() => Root.RemoveFromHierarchy();
            public void Init() { }
            public void InvokeClicked() => Clicked?.Invoke(this);
            public void Register(IControlPanelTab subtab) { }
        }

        private class FakeSubwindow : IControlPanelSubwindow
        {
            public VisualElement Root { get; } = new VisualElement();
            public string PathToUxml => string.Empty;
            public bool IsVisible => IsShown;
            public bool IsShown { get; private set; }

            public void Init() { }
            public void Bind() { }
            public void Unbind() { }
            // Like the real subwindow, disposing takes the Root out of the hierarchy.
            public void Dispose() => Root.RemoveFromHierarchy();
            public void Show() => IsShown = true;
            public void Hide() => IsShown = false;
            public void Refresh() { }
        }
        #endregion
    }
}

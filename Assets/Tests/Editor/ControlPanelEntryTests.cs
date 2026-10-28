using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium.Tests
{
    public class ControlPanelEntryTests
    {
        [TearDown]
        public void TearDown()
        {
            ControlPanelSignals.EntryTabClicked = delegate { };
        }

        #region Init
        [Test]
        public void Init_FirstCall_PreparesTabAndSetsInittedTrue()
        {
            var entry = new TestEntry();

            entry.Init();

            Assert.That(entry.IsInitted, Is.True);
            Assert.That(entry.Tab, Is.Not.Null);
            Assert.That(entry.PrepareLeftSidebarTabCallCount, Is.EqualTo(1));
        }

        [Test]
        public void Init_CalledTwiceWithoutForce_SecondCallIsNoOp()
        {
            var entry = new TestEntry();

            entry.Init();
            entry.Init();

            Assert.That(entry.PrepareLeftSidebarTabCallCount, Is.EqualTo(1));
        }

        [Test]
        public void Init_CalledAgainWithForceReinit_ReInitializes()
        {
            var entry = new TestEntry();
            entry.Init();

            entry.Init(forceReinit: true);

            Assert.That(entry.PrepareLeftSidebarTabCallCount, Is.EqualTo(2));
            Assert.That(entry.IsInitted, Is.True);
        }

        [Test]
        public void Init_CallsCreateSubentriesAndPrepareSubwindow()
        {
            var entry = new TestEntry();

            entry.Init();

            Assert.That(entry.CreateSubentriesCallCount, Is.EqualTo(1));
            Assert.That(entry.PrepareSubwindowCallCount, Is.EqualTo(1));
        }
        #endregion

        #region Tab click -> signal forwarding
        [Test]
        public void TabClicked_AfterInit_ForwardsToControlPanelSignals()
        {
            var entry = new TestEntry();
            entry.Init();
            IControlPanelEntry received = null;
            ControlPanelSignals.EntryTabClicked += e => received = e;

            entry.Tab.InvokeClicked();

            Assert.That(received, Is.SameAs(entry));
        }

        #endregion

        #region Subwindow guard behavior
        [Test]
        public void Subwindow_Getter_MeantToHaveSubwindowButNull_Throws()
        {
            var entry = new TestEntry(meantToHaveSubwindow: true, assignSubwindow: false);
            entry.Init();

            Assert.Throws<InvalidOperationException>(() => _ = entry.Subwindow);
        }

        [Test]
        public void Subwindow_Getter_NotMeantToHaveSubwindowAndNull_DoesNotThrow()
        {
            var entry = new TestEntry(meantToHaveSubwindow: false, assignSubwindow: false);
            entry.Init();

            Assert.DoesNotThrow(() => _ = entry.Subwindow);
            Assert.That(entry.Subwindow, Is.Null);
        }

        [Test]
        public void Subwindow_Getter_MeantToHaveSubwindowAndAssigned_ReturnsIt()
        {
            var entry = new TestEntry(meantToHaveSubwindow: true, assignSubwindow: true);

            entry.Init();

            Assert.That(entry.Subwindow, Is.Not.Null);
        }

        [Test]
        public void Subwindow_Setter_NotMeantToHaveSubwindow_Throws()
        {
            var entry = new TestEntry(meantToHaveSubwindow: false, assignSubwindow: true);

            Assert.Throws<InvalidOperationException>(() => entry.Init());
        }
        #endregion

        #region ResetState (via forceReinit)
        [Test]
        public void Init_ForceReinit_DisposesThePreviousSubwindow()
        {
            var entry = new TestEntry(meantToHaveSubwindow: true, assignSubwindow: true);
            entry.Init();
            var originalSubwindow = (FakeSubwindow)entry.Subwindow;

            entry.Init(forceReinit: true);

            Assert.That(originalSubwindow.DisposeCallCount, Is.EqualTo(1));
        }
        #endregion


        #region Dispose
        [Test]
        public void Dispose_BeforeInit_DoesNotThrow()
        {
            var entry = new TestEntry();

            Assert.DoesNotThrow(() => entry.Dispose());
        }

        [Test]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            var entry = new TestEntry();
            entry.Init();

            Assert.DoesNotThrow(() =>
            {
                entry.Dispose();
                entry.Dispose();
            });
        }

        [Test]
        public void Init_AfterDisposeBeforeAnyInit_StillInitializes()
        {
            var entry = new TestEntry();
            entry.Dispose();

            entry.Init(forceReinit: true);

            Assert.That(entry.IsInitted, Is.True);
        }
        #endregion

        #region HasUnsavedChanges (which comes from the logic)
        private static IUndoableCommand NoOpCommand() => new DelegateCommand(() => { }, () => { });

        [Test]
        public void HasUnsavedChanges_AfterInit_IsFalse()
        {
            var entry = new TestEntry();
            entry.Init();

            Assert.That(entry.HasUnsavedChanges, Is.False);
        }

        [Test]
        public void HasUnsavedChanges_WhenAnEditIsMade_RaisesTheSignalForTheEntry()
        {
            var entry = new TestEntry();
            entry.Init();
            var raised = new List<IControlPanelEntry>();
            Action<IControlPanelEntry> handler = raised.Add;
            ControlPanelSignals.UnsavedChangesChanged += handler;

            try
            {
                entry.Logic.History.Record(NoOpCommand());
            }
            finally
            {
                ControlPanelSignals.UnsavedChangesChanged -= handler;
            }

            Assert.That(raised, Is.EqualTo(new[] { entry }));
            Assert.That(entry.HasUnsavedChanges, Is.True);
        }

        [Test]
        public void HasUnsavedChanges_BackToTheSavedState_RaisesTheSignalAgain()
        {
            var entry = new TestEntry();
            entry.Init();
            entry.Logic.History.Record(NoOpCommand());
            int raisedCount = 0;
            Action<IControlPanelEntry> handler = _ => raisedCount++;
            ControlPanelSignals.UnsavedChangesChanged += handler;

            try
            {
                entry.Logic.History.Undo();
            }
            finally
            {
                ControlPanelSignals.UnsavedChangesChanged -= handler;
            }

            Assert.That(raisedCount, Is.EqualTo(1));
            Assert.That(entry.HasUnsavedChanges, Is.False);
        }

        [Test]
        public void HasUnsavedChanges_BeforeInit_IsFalse()
        {
            var entry = new TestEntry(); // No logic yet

            Assert.That(entry.HasUnsavedChanges, Is.False);
        }

        [Test]
        public void Select_WithUnsavedChanges_DoesNotReload()
        {
            var entry = new TestEntry();
            entry.Init();
            entry.Logic.History.Record(NoOpCommand());

            entry.Select();

            Assert.That(entry.LoadingCallCount, Is.EqualTo(0));
            Assert.That(((FakeSubwindow)entry.Subwindow).IsVisible, Is.True);
        }

        [Test]
        public void Select_WithoutUnsavedChanges_Loads()
        {
            var entry = new TestEntry();
            entry.Init();

            entry.Select();

            Assert.That(entry.LoadingCallCount, Is.EqualTo(1));
        }
        #endregion

        #region Subentry identity
        [Test]
        public void Init_CreatesSubentriesOnceAndRegistersThemUnderTheTab()
        {
            var parent = new ParentEntry();

            parent.Init();

            Assert.That(parent.CreateSubentriesCallCount, Is.EqualTo(1));
            Assert.That(parent.GetSubentries(), Has.Count.EqualTo(1));
            Assert.That(parent.GetSubentries()[0].IsInitted, Is.True);
            Assert.That(((FakeTab)parent.Tab).Registered,
                Is.EqualTo(new[] { parent.GetSubentries()[0].Tab }));
        }

        [Test]
        public void Init_ForceReinit_KeepsTheSameSubentryInstancesAndReinitializesThem()
        {
            var parent = new ParentEntry();
            parent.Init();
            var child = (TestEntry)parent.GetSubentries()[0];
            var oldChildTab = child.Tab;

            parent.Init(forceReinit: true);

            Assert.That(parent.CreateSubentriesCallCount, Is.EqualTo(1));
            Assert.That(parent.GetSubentries(), Is.EqualTo(new[] { child }));
            Assert.That(child.IsInitted, Is.True);
            Assert.That(child.Tab, Is.Not.SameAs(oldChildTab));
            Assert.That(((FakeTab)parent.Tab).Registered,
                Is.EqualTo(new[] { child.Tab }));
        }

        [Test]
        public void DisposeThenForceReinit_KeepsTheSameSubentryInstances()
        {
            var parent = new ParentEntry();
            parent.Init();
            var child = (TestEntry)parent.GetSubentries()[0];

            parent.Dispose();
            parent.Init(forceReinit: true);

            Assert.That(parent.CreateSubentriesCallCount, Is.EqualTo(1));
            Assert.That(parent.GetSubentries(), Is.EqualTo(new[] { child }));
            Assert.That(child.IsInitted, Is.True);
            Assert.That(child.Tab, Is.Not.Null);
        }

        [Test]
        public void Dispose_AlsoDisposesTheSubentries()
        {
            var parent = new ParentEntry();
            parent.Init();
            var child = (TestEntry)parent.GetSubentries()[0];
            var childTab = (FakeTab)child.Tab;

            parent.Dispose();

            Assert.That(childTab.DisposeCallCount, Is.EqualTo(1));
            Assert.That(child.Tab, Is.Null);
        }
        #endregion

        #region Defaults
        [Test]
        public void IsTestOnly_DefaultsToFalse()
        {
            var entry = new TestEntry();

            Assert.That(entry.IsTestOnly, Is.False);
        }

        [Test]
        public void HasSubentries_NoSubentriesAdded_ReturnsFalse()
        {
            var entry = new TestEntry();

            Assert.That(entry.HasSubentries, Is.False);
        }

        [Test]
        public void GetSubentries_NoneAdded_ReturnsEmpty()
        {
            var entry = new TestEntry();

            var result = entry.GetSubentries();

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void OnSelected_OnDeselected_DoNotThrowByDefault()
        {
            var entry = new TestEntry();

            Assert.DoesNotThrow(() =>
            {
                entry.Select();
                entry.Deselect();
            });
        }
        #endregion

        #region Test Doubles
        private class TestEntry : ControlPanelEntry
        {
            private readonly bool _meantToHaveSubwindow;
            private readonly bool _assignSubwindow;

            public TestEntry(bool meantToHaveSubwindow = true, bool assignSubwindow = true)
            {
                _meantToHaveSubwindow = meantToHaveSubwindow;
                _assignSubwindow = assignSubwindow;
            }

            public override string SortingName => nameof(TestEntry);
            public override bool IsMeantToHaveSubwindow => _meantToHaveSubwindow;

            protected override Type SidebarTabType => typeof(FakeTab);

            public int LoadingCallCount { get; private set; }

            // Not calling the base, which would start the loader's (asynchronous) editor
            // coroutine. What the tests care about is whether Select decided to load.
            protected override void HandleLoading()
            {
                LoadingCallCount++;
            }

            public int PrepareLeftSidebarTabCallCount { get; private set; }
            public int CreateSubentriesCallCount { get; private set; }
            public int PrepareSubwindowCallCount { get; private set; }

            protected override void PrepareMainSidebarTab()
            {
                PrepareLeftSidebarTabCallCount++;
                Tab = new FakeTab();
            }

            protected override IEnumerable<IControlPanelEntry> CreateSubentries()
            {
                CreateSubentriesCallCount++;
                return base.CreateSubentries();
            }

            protected override void PrepareSubwindow()
            {
                PrepareSubwindowCallCount++;
                if (_assignSubwindow)
                {
                    Subwindow = new FakeSubwindow();
                }
            }
        }

        private class ParentEntry : ControlPanelEntry
        {
            public override string SortingName => nameof(ParentEntry);
            public override bool IsMeantToHaveSubwindow => false;
            public int CreateSubentriesCallCount { get; private set; }

            protected override Type SidebarTabType => typeof(FakeTab);
            protected override void PrepareMainSidebarTab() => Tab = new FakeTab();

            protected override IEnumerable<IControlPanelEntry> CreateSubentries()
            {
                CreateSubentriesCallCount++;
                return new IControlPanelEntry[] { new TestEntry() };
            }
        }

        private class FakeTab : IControlPanelTab
        {
            public VisualElement Root { get; } = new VisualElement();
            public string PathToUxml => string.Empty;
            public string DisplayName => "Fake";
            public string Text { get; set; }
            public bool IsSelected { get; set; }
            public IReadOnlyList<IControlPanelTab> Subtabs => Array.Empty<IControlPanelTab>();
            public int DisposeCallCount { get; private set; }

            public event Action<IControlPanelTab> Clicked;

            public void Dispose() => DisposeCallCount++;

            public void Init() { }
            public void InvokeClicked() => Clicked?.Invoke(this);
            public List<IControlPanelTab> Registered { get; } = new List<IControlPanelTab>();
            public void Register(IControlPanelTab subtab) => Registered.Add(subtab);
            public void RemoveFromHierarchy() => Root.RemoveFromHierarchy();
        }

        private class FakeSubwindow : IControlPanelSubwindow
        {
            public VisualElement Root { get; } = new VisualElement();
            public string PathToUxml => string.Empty;
            public bool IsVisible => Root.style.display == DisplayStyle.Flex;
            public int DisposeCallCount { get; private set; }

            public void Init() { }
            public void Bind() { }
            public void Unbind() { }
            public void Dispose() => DisposeCallCount++;
            public void Show() => Root.style.display = DisplayStyle.Flex;
            public void Hide() => Root.style.display = DisplayStyle.None;
            public void Refresh() { }

            public T Q<T>(string name, string className) where T : VisualElement
            {
                return null;//
            }
        }
        #endregion
    }
}

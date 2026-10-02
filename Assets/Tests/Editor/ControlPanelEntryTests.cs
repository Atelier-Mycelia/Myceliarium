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
            ControlPanelSignals.OnEntryTabClicked = delegate { };
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
        public void Init_CallsPrepareSubentriesAndPrepareSubwindow()
        {
            var entry = new TestEntry();

            entry.Init();

            Assert.That(entry.PrepareSubentriesCallCount, Is.EqualTo(1));
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
            ControlPanelSignals.OnEntryTabClicked += e => received = e;

            entry.Tab.InvokeClicked();

            Assert.That(received, Is.SameAs(entry));
        }

        [Test]
        public void TabClicked_AfterDispose_NoLongerForwardsToSignal()
        {
            var entry = new TestEntry();
            entry.Init();
            bool wasCalled = false;
            ControlPanelSignals.OnEntryTabClicked += _ => wasCalled = true;

            entry.Dispose();
            entry.Tab.InvokeClicked();

            Assert.That(wasCalled, Is.False);
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
        public void Dispose_DisposesTabAndSubwindowAndRemovesTheirRootsFromHierarchy()
        {
            var root = new VisualElement();
            var entry = new TestEntry(meantToHaveSubwindow: true, assignSubwindow: true);
            entry.Init();
            var tab = (FakeTab)entry.Tab;
            var subwindow = (FakeSubwindow)entry.Subwindow;
            root.Add(tab.Root);
            root.Add(subwindow.Root);

            entry.Dispose();

            Assert.That(tab.DisposeCallCount, Is.EqualTo(1));
            Assert.That(subwindow.DisposeCallCount, Is.EqualTo(1));
            Assert.That(root.Contains(tab.Root), Is.False);
            Assert.That(root.Contains(subwindow.Root), Is.False);
        }

        [Test]
        public void Dispose_EntryWithoutSubwindow_DoesNotThrow()
        {
            var entry = new TestEntry(meantToHaveSubwindow: false, assignSubwindow: false);
            entry.Init();

            Assert.DoesNotThrow(() => entry.Dispose());
        }

        [Test]
        public void Dispose_CalledTwice_SecondCallIsNoOp()
        {
            var entry = new TestEntry(meantToHaveSubwindow: true, assignSubwindow: true);
            entry.Init();
            var tab = (FakeTab)entry.Tab;

            entry.Dispose();
            entry.Dispose();

            Assert.That(tab.DisposeCallCount, Is.EqualTo(1));
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
                entry.OnSelected();
                entry.OnDeselected();
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

            public int PrepareLeftSidebarTabCallCount { get; private set; }
            public int PrepareSubentriesCallCount { get; private set; }
            public int PrepareSubwindowCallCount { get; private set; }

            protected override void PrepareLeftSidebarTab()
            {
                PrepareLeftSidebarTabCallCount++;
                Tab = new FakeTab();
            }

            protected override void PrepareSubentries()
            {
                PrepareSubentriesCallCount++;
                base.PrepareSubentries();
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
            public void Register(IControlPanelTab subtab) { }
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

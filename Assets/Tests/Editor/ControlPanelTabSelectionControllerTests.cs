using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium.Tests
{
    public class ControlPanelTabSelectionControllerTests
    {
        private ControlPanelTabSelectionController _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new ControlPanelTabSelectionController();
        }

        [TearDown]
        public void TearDown()
        {
            _controller.Dispose();
        }

        #region Basic selection / subwindow toggling
        [Test]
        public void TabClicked_KnownEntry_ShowsItsSubwindowAndSelectsItsTab()
        {
            var entry = new FakeEntry();
            _controller.Init(new List<IControlPanelEntry> { entry });

            entry.Tab.InvokeClicked();

            Assert.That(((FakeSubwindow)entry.Subwindow).IsShown, Is.True);
            Assert.That(entry.Tab.IsSelected, Is.True);
        }

        [Test]
        public void TabClicked_SwitchingBetweenTwoEntries_HidesPreviousAndShowsNew()
        {
            var entryA = new FakeEntry();
            var entryB = new FakeEntry();
            _controller.Init(new List<IControlPanelEntry> { entryA, entryB });

            entryA.Tab.InvokeClicked();
            entryB.Tab.InvokeClicked();

            Assert.That(((FakeSubwindow)entryA.Subwindow).IsShown, Is.False);
            Assert.That(((FakeSubwindow)entryB.Subwindow).IsShown, Is.True);
            Assert.That(entryA.Tab.IsSelected, Is.False);
            Assert.That(entryB.Tab.IsSelected, Is.True);
        }

        [Test]
        public void TabClicked_SameEntryTwice_IsIdempotent()
        {
            var entry = new FakeEntry();
            _controller.Init(new List<IControlPanelEntry> { entry });

            entry.Tab.InvokeClicked();
            entry.Tab.InvokeClicked();

            var subwindow = (FakeSubwindow)entry.Subwindow;
            Assert.That(subwindow.ShowCallCount, Is.EqualTo(1));
            Assert.That(subwindow.HideCallCount, Is.EqualTo(0));
            Assert.That(entry.Tab.IsSelected, Is.True);
        }

        [Test]
        public void TabClicked_UnknownEntry_IsIgnored()
        {
            var known = new FakeEntry();
            var unknown = new FakeEntry();
            _controller.Init(new List<IControlPanelEntry> { known });

            unknown.Tab.InvokeClicked();

            Assert.That(((FakeSubwindow)unknown.Subwindow).IsShown, Is.False);
            Assert.That(unknown.Tab.IsSelected, Is.False);
        }

        [Test]
        public void TabClicked_EntryNotMeantToHaveSubwindow_IsIgnored()
        {
            var entry = new FakeEntry(isMeantToHaveSubwindow: false);
            _controller.Init(new List<IControlPanelEntry> { entry });

            entry.Tab.InvokeClicked();

            Assert.That(entry.Tab.IsSelected, Is.False);
        }
        #endregion

        #region Nested subentries
        [Test]
        public void TabClicked_NestedSubentry_IsRecognizedAndSelected()
        {
            var child = new FakeEntry();
            var parent = new FakeEntry(subentries: new List<IControlPanelEntry> { child });
            _controller.Init(new List<IControlPanelEntry> { parent });

            child.Tab.InvokeClicked();

            Assert.That(((FakeSubwindow)child.Subwindow).IsShown, Is.True);
            Assert.That(child.Tab.IsSelected, Is.True);
        }

        [Test]
        public void TabClicked_SelectingSubentry_DeselectsOtherTopLevelAndSiblingTabs()
        {
            var child = new FakeEntry();
            var parent = new FakeEntry(subentries: new List<IControlPanelEntry> { child });
            var sibling = new FakeEntry();
            _controller.Init(new List<IControlPanelEntry> { parent, sibling });

            child.Tab.InvokeClicked();

            Assert.That(parent.Tab.IsSelected, Is.False);
            Assert.That(sibling.Tab.IsSelected, Is.False);
            Assert.That(child.Tab.IsSelected, Is.True);
        }
        #endregion

        #region Init / Dispose lifecycle
        [Test]
        public void Init_NullEntries_DoesNotThrowAndTreatsAsEmpty()
        {
            Assert.DoesNotThrow(() => _controller.Init(null));

            var entry = new FakeEntry();
            entry.Tab.InvokeClicked();

            Assert.That(entry.Tab.IsSelected, Is.False);
        }

        [Test]
        public void Init_CalledAgain_ResetsCurrentlyDisplayedEntry()
        {
            var entry = new FakeEntry();
            _controller.Init(new List<IControlPanelEntry> { entry });
            entry.Tab.InvokeClicked();

            _controller.Init(new List<IControlPanelEntry> { entry });
            entry.Tab.InvokeClicked();

            // Second Init resets _entryBeingDisplayed to null, so clicking the same
            // entry again should be treated as a fresh switch (Show called twice total).
            var subwindow = (FakeSubwindow)entry.Subwindow;
            Assert.That(subwindow.ShowCallCount, Is.EqualTo(2));
        }

        [Test]
        public void Dispose_UnsubscribesFromSignal_SubsequentClicksAreIgnored()
        {
            var entry = new FakeEntry();
            _controller.Init(new List<IControlPanelEntry> { entry });

            _controller.Dispose();
            entry.Tab.InvokeClicked();

            Assert.That(entry.Tab.IsSelected, Is.False);
        }

        [Test]
        public void Dispose_CalledTwice_SecondCallIsNoOp()
        {
            var entry = new FakeEntry();
            _controller.Init(new List<IControlPanelEntry> { entry });

            Assert.DoesNotThrow(() =>
            {
                _controller.Dispose();
                _controller.Dispose();
            });
        }
        #endregion

        #region Test Doubles
        private class FakeEntry : IControlPanelEntry
        {
            private readonly IReadOnlyList<IControlPanelEntry> _subentries;

            public FakeEntry(
                bool isMeantToHaveSubwindow = true,
                IReadOnlyList<IControlPanelEntry> subentries = null)
            {
                IsMeantToHaveSubwindow = isMeantToHaveSubwindow;
                _subentries = subentries ?? Array.Empty<IControlPanelEntry>();
                Tab = new FakeTab();
                Subwindow = new FakeSubwindow();

                // Mirrors ControlPanelEntry.ToggleSubs/OnTabClicked: the real
                // production entry subscribes to its tab's Clicked event and
                // forwards it to the static ControlPanelSignals.OnEntryTabClicked
                // bus, which is what ControlPanelTabSelectionController actually
                // listens to. Without this bridge, invoking the fake tab's
                // Clicked event would never reach the controller under test.
                Tab.Clicked += _ => ControlPanelSignals.OnEntryTabClicked(this);
            }

            public int SortingOrder => 0;
            public string SortingName => string.Empty;
            public IControlPanelTab Tab { get; }
            public IControlPanelSubwindow Subwindow { get; }
            public bool IsTopLevel => true;
            public bool IsMeantToHaveSubwindow { get; }
            public bool IsInitted => true;
            public bool IsTestOnly => true;
            public bool HasSubentries => _subentries.Count > 0;

            public void Init(bool forceReinit = false) { }

            public IReadOnlyList<IControlPanelEntry> GetSubentries(bool recursive = false)
            {
                if (!recursive)
                {
                    return _subentries;
                }

                var result = new List<IControlPanelEntry>(_subentries);
                foreach (var sub in _subentries)
                {
                    result.AddRange(sub.GetSubentries(true));
                }
                return result;
            }

            public void RemoveFromHierarchy() { }
            public void Select() { }
            public void Deselect() { }
        }

        private class FakeTab : IControlPanelTab
        {
            public UnityEngine.UIElements.VisualElement Root => null;
            public string PathToUxml => string.Empty;
            public string DisplayName => "Fake";
            public string Text { get; set; }
            public bool IsSelected { get; set; }
            public IReadOnlyList<IControlPanelTab> Subtabs => Array.Empty<IControlPanelTab>();

            public event Action<IControlPanelTab> Clicked;

            public void Dispose()
            {

            }

            public void Init() { }
            public void InvokeClicked() => Clicked?.Invoke(this);
            public void Register(IControlPanelTab subtab) { }
            public void RemoveFromHierarchy() { }
        }

        private class FakeSubwindow : IControlPanelSubwindow
        {
            public UnityEngine.UIElements.VisualElement Root => null;
            public string PathToUxml => string.Empty;
            public bool IsVisible => IsShown;
            public bool IsShown { get; private set; }
            public int ShowCallCount { get; private set; }
            public int HideCallCount { get; private set; }

            public void Init() { }
            public void Bind() { }
            public void Unbind() { }
            public void Dispose() { }
            public void RemoveFromHierarchy() { }

            public void Show()
            {
                IsShown = true;
                ShowCallCount++;
            }

            public void Hide()
            {
                IsShown = false;
                HideCallCount++;
            }

            public void Refresh() { }
            public T Q<T>(string name, string className) where T : VisualElement
            {
                return null;
            }
        }
        #endregion
    }
}

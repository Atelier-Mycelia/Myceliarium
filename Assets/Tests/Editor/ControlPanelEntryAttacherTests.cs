using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium.Tests
{
    public class ControlPanelEntryAttacherTests
    {
        private ControlPanelEntryAttacher _attacher;

        [SetUp]
        public void SetUp()
        {
            _attacher = new ControlPanelEntryAttacher();
        }

        [TearDown]
        public void TearDown()
        {
            _attacher.Dispose();
        }

        private static VisualElement CreateValidRoot(
            out VisualElement mainTabSet,
            out ScrollView subwindowDisplay)
        {
            var root = new VisualElement();
            mainTabSet = new VisualElement { name = "MainTabSet" };
            subwindowDisplay = new ScrollView { name = "CategorySubwindowDisplay" };
            root.Add(mainTabSet);
            root.Add(subwindowDisplay);
            return root;
        }

        #region Init
        [Test]
        public void Init_NullRootElement_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _attacher.Init(null));
        }

        [Test]
        public void Init_MissingMainTabSet_ThrowsInvalidOperationException()
        {
            var root = new VisualElement();
            root.Add(new ScrollView { name = "CategorySubwindowDisplay" });

            var ex = Assert.Throws<InvalidOperationException>(() => _attacher.Init(root));
            Assert.That(ex.Message, Does.Contain("MainTabSet"));
        }

        [Test]
        public void Init_MissingSubwindowDisplay_ThrowsInvalidOperationException()
        {
            var root = new VisualElement();
            root.Add(new VisualElement { name = "MainTabSet" });

            var ex = Assert.Throws<InvalidOperationException>(() => _attacher.Init(root));
            Assert.That(ex.Message, Does.Contain("CategorySubwindowDisplay"));
        }

        [Test]
        public void Init_MissingBothContainers_ThrowsWithBothMessagesIncluded()
        {
            var root = new VisualElement();

            var ex = Assert.Throws<InvalidOperationException>(() => _attacher.Init(root));
            Assert.That(ex.Message, Does.Contain("MainTabSet"));
            Assert.That(ex.Message, Does.Contain("CategorySubwindowDisplay"));
        }

        [Test]
        public void Init_ValidRootElement_DoesNotThrow()
        {
            var root = CreateValidRoot(out _, out _);

            Assert.DoesNotThrow(() => _attacher.Init(root));
        }
        #endregion

        #region Attach - basic behavior
        [Test]
        public void Attach_TopLevelEntry_AddsTabRootToMainTabSet()
        {
            var root = CreateValidRoot(out var mainTabSet, out _);
            _attacher.Init(root);
            var entry = new FakeEntry();

            _attacher.Attach(new List<IControlPanelEntry> { entry });

            Assert.That(mainTabSet.Contains(entry.Tab.Root), Is.True);
        }

        [Test]
        public void Attach_NonTopLevelEntry_IsTrackedButNotAttachedToMainTabSet()
        {
            var root = CreateValidRoot(out var mainTabSet, out _);
            _attacher.Init(root);
            var entry = new FakeEntry(isTopLevel: false);

            _attacher.Attach(new List<IControlPanelEntry> { entry });

            Assert.That(_attacher.Entries.Contains(entry), Is.True);
            Assert.That(mainTabSet.Contains(entry.Tab.Root), Is.False);
        }

        [Test]
        public void Attach_SameEntryTwice_DoesNotDuplicateInEntries()
        {
            var root = CreateValidRoot(out _, out _);
            _attacher.Init(root);
            var entry = new FakeEntry();

            _attacher.Attach(new List<IControlPanelEntry> { entry });
            _attacher.Attach(new List<IControlPanelEntry> { entry });

            Assert.That(_attacher.Entries.Count, Is.EqualTo(1));
        }

        [Test]
        public void Attach_MultipleEntries_AllTrackedInEntries()
        {
            var root = CreateValidRoot(out _, out _);
            _attacher.Init(root);
            var entryA = new FakeEntry();
            var entryB = new FakeEntry();

            _attacher.Attach(new List<IControlPanelEntry> { entryA, entryB });

            Assert.That(_attacher.Entries.Count, Is.EqualTo(2));
            Assert.That(_attacher.Entries, Does.Contain(entryA));
            Assert.That(_attacher.Entries, Does.Contain(entryB));
        }
        #endregion

        #region Attach - Init() delegation
        [Test]
        public void Attach_EntryNotInitted_CallsInit()
        {
            var root = CreateValidRoot(out _, out _);
            _attacher.Init(root);
            var entry = new FakeEntry(isInitted: false);

            _attacher.Attach(new List<IControlPanelEntry> { entry });

            Assert.That(entry.InitCallCount, Is.EqualTo(1));
        }

        [Test]
        public void Attach_EntryAlreadyInitted_DoesNotCallInitAgain()
        {
            var root = CreateValidRoot(out _, out _);
            _attacher.Init(root);
            var entry = new FakeEntry(isInitted: true);

            _attacher.Attach(new List<IControlPanelEntry> { entry });

            Assert.That(entry.InitCallCount, Is.EqualTo(0));
        }

        [Test]
        public void Attach_EntryInitThrows_LogsErrorAndStillAttachesTab()
        {
            var root = CreateValidRoot(out var mainTabSet, out _);
            _attacher.Init(root);
            var entry = new FakeEntry(isInitted: false, initThrows: true);

            LogAssert.Expect(LogType.Error,
                new System.Text.RegularExpressions.Regex(".*Failed to attach.*"));

            Assert.DoesNotThrow(() =>
                _attacher.Attach(new List<IControlPanelEntry> { entry }));
            Assert.That(mainTabSet.Contains(entry.Tab.Root), Is.True);
        }
        #endregion

        #region Attach - subwindow registration
        [Test]
        public void Attach_EntryWithSubwindow_HidesItAndAddsToSubwindowDisplay()
        {
            var root = CreateValidRoot(out _, out var subwindowDisplay);
            _attacher.Init(root);
            var entry = new FakeEntry();

            _attacher.Attach(new List<IControlPanelEntry> { entry });

            var subwindow = (FakeSubwindow)entry.Subwindow;
            Assert.That(subwindow.HideCallCount, Is.EqualTo(1));
            Assert.That(subwindowDisplay.Contains(subwindow.Root), Is.True);
        }

        [Test]
        public void Attach_EntryWithoutSubwindow_SkipsSubwindowRegistration()
        {
            var root = CreateValidRoot(out _, out var subwindowDisplay);
            _attacher.Init(root);
            var entry = new FakeEntry(hasSubwindow: false);

            Assert.DoesNotThrow(() =>
                _attacher.Attach(new List<IControlPanelEntry> { entry }));
            Assert.That(subwindowDisplay.childCount, Is.EqualTo(0));
        }

        [Test]
        public void Attach_EntryWithSubentries_RegistersSubentrySubwindowsRecursively()
        {
            var root = CreateValidRoot(out _, out var subwindowDisplay);
            _attacher.Init(root);
            var grandchild = new FakeEntry();
            var child = new FakeEntry(subentries: new List<IControlPanelEntry> { grandchild });
            var parent = new FakeEntry(subentries: new List<IControlPanelEntry> { child });

            _attacher.Attach(new List<IControlPanelEntry> { parent });

            var parentSubwindow = (FakeSubwindow)parent.Subwindow;
            var childSubwindow = (FakeSubwindow)child.Subwindow;
            var grandchildSubwindow = (FakeSubwindow)grandchild.Subwindow;

            Assert.That(subwindowDisplay.Contains(parentSubwindow.Root), Is.True);
            Assert.That(subwindowDisplay.Contains(childSubwindow.Root), Is.True);
            Assert.That(subwindowDisplay.Contains(grandchildSubwindow.Root), Is.True);
        }

        [Test]
        public void Attach_SubentryWithoutSubwindow_IsSkippedWithoutError()
        {
            var root = CreateValidRoot(out _, out var subwindowDisplay);
            _attacher.Init(root);
            var child = new FakeEntry(hasSubwindow: false);
            var parent = new FakeEntry(subentries: new List<IControlPanelEntry> { child });

            Assert.DoesNotThrow(() =>
                _attacher.Attach(new List<IControlPanelEntry> { parent }));

            var parentSubwindow = (FakeSubwindow)parent.Subwindow;
            Assert.That(subwindowDisplay.Contains(parentSubwindow.Root), Is.True);
            Assert.That(subwindowDisplay.childCount, Is.EqualTo(1));
        }
        #endregion

        #region Dispose
        [Test]
        public void Dispose_ClearsEntries()
        {
            var root = CreateValidRoot(out _, out _);
            _attacher.Init(root);
            var entry = new FakeEntry();
            _attacher.Attach(new List<IControlPanelEntry> { entry });

            _attacher.Dispose();

            Assert.That(_attacher.Entries.Count, Is.EqualTo(0));
        }

        [Test]
        public void Dispose_CalledTwice_SecondCallIsNoOp()
        {
            var root = CreateValidRoot(out _, out _);
            _attacher.Init(root);

            Assert.DoesNotThrow(() =>
            {
                _attacher.Dispose();
                _attacher.Dispose();
            });
        }

        [Test]
        public void Init_CalledAgainAfterDispose_WorksCorrectly()
        {
            var root = CreateValidRoot(out var mainTabSet, out _);
            _attacher.Init(root);
            _attacher.Dispose();

            var newRoot = CreateValidRoot(out var newMainTabSet, out _);
            _attacher.Init(newRoot);
            var entry = new FakeEntry();

            Assert.DoesNotThrow(() =>
                _attacher.Attach(new List<IControlPanelEntry> { entry }));
            Assert.That(newMainTabSet.Contains(entry.Tab.Root), Is.True);
        }
        #endregion

        #region Test Doubles
        private class FakeEntry : IControlPanelEntry
        {
            private readonly IReadOnlyList<IControlPanelEntry> _subentries;
            private readonly bool _initThrows;

            public FakeEntry(
                bool isTopLevel = true,
                bool isInitted = true,
                bool initThrows = false,
                bool hasSubwindow = true,
                IControlPanelSubwindow subwindow = null,
                IReadOnlyList<IControlPanelEntry> subentries = null)
            {
                IsTopLevel = isTopLevel;
                IsInitted = isInitted;
                _initThrows = initThrows;
                _subentries = subentries ?? Array.Empty<IControlPanelEntry>();
                Tab = new FakeTab();

                if (!hasSubwindow)
                {
                    Subwindow = null;
                }
                else
                {
                    Subwindow = subwindow ?? new FakeSubwindow();
                }
            }

            public bool IsTestOnly => true;
            public int SortingOrder => 0;
            public string SortingName => string.Empty;
            public IControlPanelTab Tab { get; }
            public IControlPanelSubwindow Subwindow { get; }
            public bool IsTopLevel { get; }
            public bool IsMeantToHaveSubwindow => Subwindow != null;
            public bool IsInitted { get; }
            public bool HasSubentries => _subentries.Count > 0;
            public int InitCallCount { get; private set; }

            public void Init(bool forceReinit = false)
            {
                InitCallCount++;
                if (_initThrows)
                {
                    throw new InvalidOperationException("Simulated Init failure");
                }
            }

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
            public VisualElement Root { get; } = new VisualElement();
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

        private class FakeSubwindow : VisualElement, IControlPanelSubwindow
        {
            public VisualElement Root { get; } = new VisualElement();
            public string PathToUxml => string.Empty;
            public bool IsVisible => Root.style.display == DisplayStyle.Flex;
            public int ShowCallCount { get; private set; }
            public int HideCallCount { get; private set; }

            public void Init() { }
            public void Bind() { }
            public void Unbind() { }
            public void Dispose() { }

            public void Show()
            {
                ShowCallCount++;
                Root.style.display = DisplayStyle.Flex;
            }

            public void Hide()
            {
                HideCallCount++;
                Root.style.display = DisplayStyle.None;
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

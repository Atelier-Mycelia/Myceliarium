using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium.Tests
{
    public class ControlPanelSignalsTests
    {
        [TearDown]
        public void TearDown()
        {
            // These are static, shared event buses. Resetting them to their
            // default no-op delegates after each test prevents leftover
            // subscriptions from one test leaking into (and breaking) another.
            ControlPanelSignals.OnControlPanelOpened = delegate { };
            ControlPanelSignals.OnControlPanelClosed = delegate { };
            ControlPanelSignals.OnEntryTabClicked = delegate { };
            ControlPanelSignals.SaveRequested = delegate { };
            ControlPanelSignals.SaveCompleted = delegate { };
            ControlPanelSignals.LoadCompleted = delegate { };
            ControlPanelSignals.SaveFailed = delegate { };
            ControlPanelSignals.LoadFailed = delegate { };
            ControlPanelSignals.CloseRequested = delegate { };
        }

        #region OnControlPanelOpened / OnControlPanelClosed
        [Test]
        public void OnControlPanelOpened_Invoked_NotifiesSubscriberWithCorrectPanel()
        {
            var panel = new FakeControlPanel();
            IControlPanel received = null;
            ControlPanelSignals.OnControlPanelOpened += p => received = p;

            ControlPanelSignals.OnControlPanelOpened(panel);

            Assert.That(received, Is.SameAs(panel));
        }

        [Test]
        public void OnControlPanelClosed_Invoked_NotifiesSubscriberWithCorrectPanel()
        {
            var panel = new FakeControlPanel();
            IControlPanel received = null;
            ControlPanelSignals.OnControlPanelClosed += p => received = p;

            ControlPanelSignals.OnControlPanelClosed(panel);

            Assert.That(received, Is.SameAs(panel));
        }
        #endregion

        #region OnEntryTabClicked
        [Test]
        public void OnEntryTabClicked_Invoked_NotifiesSubscriberWithCorrectEntry()
        {
            var entry = new FakeEntry();
            IControlPanelEntry received = null;
            ControlPanelSignals.OnEntryTabClicked += e => received = e;

            ControlPanelSignals.OnEntryTabClicked(entry);

            Assert.That(received, Is.SameAs(entry));
        }

        [Test]
        public void OnEntryTabClicked_MultipleSubscribers_AllAreNotified()
        {
            var entry = new FakeEntry();
            int callCountA = 0;
            int callCountB = 0;
            ControlPanelSignals.OnEntryTabClicked += _ => callCountA++;
            ControlPanelSignals.OnEntryTabClicked += _ => callCountB++;

            ControlPanelSignals.OnEntryTabClicked(entry);

            Assert.That(callCountA, Is.EqualTo(1));
            Assert.That(callCountB, Is.EqualTo(1));
        }

        [Test]
        public void OnEntryTabClicked_UnsubscribedHandler_IsNotNotified()
        {
            var entry = new FakeEntry();
            int callCount = 0;
            void Handler(IControlPanelEntry e) => callCount++;

            ControlPanelSignals.OnEntryTabClicked += Handler;
            ControlPanelSignals.OnEntryTabClicked -= Handler;
            ControlPanelSignals.OnEntryTabClicked(entry);

            Assert.That(callCount, Is.EqualTo(0));
        }
        #endregion

        #region SaveRequested / CloseRequested
        [Test]
        public void SaveRequested_Invoked_NotifiesSubscriberWithCorrectPanel()
        {
            var panel = new FakeControlPanel();
            IControlPanel received = null;
            ControlPanelSignals.SaveRequested += p => received = p;

            ControlPanelSignals.SaveRequested(panel);

            Assert.That(received, Is.SameAs(panel));
        }

        [Test]
        public void CloseRequested_Invoked_NotifiesSubscriberWithCorrectPanel()
        {
            var panel = new FakeControlPanel();
            IControlPanel received = null;
            ControlPanelSignals.CloseRequested += p => received = p;

            ControlPanelSignals.CloseRequested(panel);

            Assert.That(received, Is.SameAs(panel));
        }
        #endregion

        #region SaveCompleted / LoadCompleted / SaveFailed / LoadFailed
        [Test]
        public void SaveCompleted_Invoked_NotifiesSubscriberWithCorrectEntry()
        {
            var entry = new FakeEntry();
            IControlPanelEntry received = null;
            ControlPanelSignals.SaveCompleted += e => received = e;

            ControlPanelSignals.SaveCompleted(entry);

            Assert.That(received, Is.SameAs(entry));
        }

        [Test]
        public void LoadCompleted_Invoked_NotifiesSubscriberWithCorrectEntry()
        {
            var entry = new FakeEntry();
            IControlPanelEntry received = null;
            ControlPanelSignals.LoadCompleted += e => received = e;

            ControlPanelSignals.LoadCompleted(entry);

            Assert.That(received, Is.SameAs(entry));
        }

        [Test]
        public void SaveFailed_Invoked_NotifiesSubscriberWithCorrectEntry()
        {
            var entry = new FakeEntry();
            IControlPanelEntry received = null;
            ControlPanelSignals.SaveFailed += e => received = e;

            ControlPanelSignals.SaveFailed(entry);

            Assert.That(received, Is.SameAs(entry));
        }

        [Test]
        public void LoadFailed_Invoked_NotifiesSubscriberWithCorrectEntry()
        {
            var entry = new FakeEntry();
            IControlPanelEntry received = null;
            ControlPanelSignals.LoadFailed += e => received = e;

            ControlPanelSignals.LoadFailed(entry);

            Assert.That(received, Is.SameAs(entry));
        }
        #endregion

        #region Defaults / No subscribers
        [Test]
        public void AllSignals_NoSubscribers_InvokingDoesNotThrow()
        {
            var panel = new FakeControlPanel();
            var entry = new FakeEntry();

            Assert.DoesNotThrow(() =>
            {
                ControlPanelSignals.OnControlPanelOpened(panel);
                ControlPanelSignals.OnControlPanelClosed(panel);
                ControlPanelSignals.OnEntryTabClicked(entry);
                ControlPanelSignals.SaveRequested(panel);
                ControlPanelSignals.SaveCompleted(entry);
                ControlPanelSignals.LoadCompleted(entry);
                ControlPanelSignals.SaveFailed(entry);
                ControlPanelSignals.LoadFailed(entry);
                ControlPanelSignals.CloseRequested(panel);
            });
        }
        #endregion

        #region Test Doubles
        private class FakeControlPanel : IControlPanel
        {
            public VisualElement Root { get; } = new VisualElement();
            public IReadOnlyList<IControlPanelEntry> TopLevelEntries { get; } =
                new List<IControlPanelEntry>();
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

            public void Init(bool forceReinit = false) { }
            public IReadOnlyList<IControlPanelEntry> GetSubentries(bool recursive = false) =>
                System.Array.Empty<IControlPanelEntry>();
            public void RemoveFromHierarchy() { }
            public void Select() { }
            public void Deselect() { }
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace AtMycelia.Myceliarium.Tests
{
    public class PlayModeGateTests
    {
        private List<IGuardedPanel> _dirtyPanels;
        private UnsavedChangesChoice _answer;
        private int _asked;
        private List<bool> _playingRequests;
        private Queue<Action> _deferred;
        private PlayModeGate _gate;

        [SetUp]
        public void SetUp()
        {
            _dirtyPanels = new List<IGuardedPanel>();
            _answer = UnsavedChangesChoice.KeepEditing;
            _asked = 0;
            _playingRequests = new List<bool>();
            _deferred = new Queue<Action>();
            _gate = new PlayModeGate(
                () => _dirtyPanels,
                () =>
                {
                    _asked++;
                    return _answer;
                },
                _playingRequests.Add,
                _deferred.Enqueue);
        }

        private void RunDeferred()
        {
            while (_deferred.Count > 0)
            {
                _deferred.Dequeue()();
            }
        }

        [Test]
        public void NothingDirty_LetsPlayModeStart()
        {
            _gate.OnExitingEditMode();
            RunDeferred();

            Assert.That(_playingRequests, Is.Empty);
            Assert.That(_asked, Is.EqualTo(0));
        }

        [Test]
        public void SomethingDirty_StopsPlayModeAndAsksOnlyAfterwards()
        {
            _dirtyPanels.Add(new FakePanel());

            _gate.OnExitingEditMode();

            Assert.That(_playingRequests, Is.EqualTo(new[] { false }));
            Assert.That(_asked, Is.EqualTo(0));

            RunDeferred();

            Assert.That(_asked, Is.EqualTo(1));
        }

        [Test]
        public void KeepEditing_DoesNotRestartPlayMode()
        {
            var panel = new FakePanel();
            _dirtyPanels.Add(panel);
            _answer = UnsavedChangesChoice.KeepEditing;

            _gate.OnExitingEditMode();
            RunDeferred();

            Assert.That(_playingRequests, Is.EqualTo(new[] { false }));
            Assert.That(panel.Saves, Is.EqualTo(0));
            Assert.That(panel.Discards, Is.EqualTo(0));
        }

        [Test]
        public void KeepEditing_ThenTryingAgain_AsksAgain()
        {
            _dirtyPanels.Add(new FakePanel());

            _gate.OnExitingEditMode();
            RunDeferred();
            _gate.OnExitingEditMode();
            RunDeferred();

            Assert.That(_asked, Is.EqualTo(2));
        }

        [Test]
        public void Save_SavesEveryPanelAndRestartsOnlyOnceTheLastIsDone()
        {
            var first = new FakePanel { CompleteSavesAutomatically = false };
            var second = new FakePanel { CompleteSavesAutomatically = false };
            _dirtyPanels.Add(first);
            _dirtyPanels.Add(second);
            _answer = UnsavedChangesChoice.Save;

            _gate.OnExitingEditMode();
            RunDeferred();
            first.CompleteSave();
            RunDeferred();

            Assert.That(_playingRequests, Is.EqualTo(new[] { false }));

            second.CompleteSave();
            RunDeferred();

            Assert.That(_playingRequests, Is.EqualTo(new[] { false, true }));
        }

        [Test]
        public void Save_WhenAnyPanelFailsToSave_DoesNotStartPlayMode()
        {
            var failing = new FakePanel { SavesSucceed = false };
            _dirtyPanels.Add(failing);
            _dirtyPanels.Add(new FakePanel());
            _answer = UnsavedChangesChoice.Save;

            _gate.OnExitingEditMode();
            RunDeferred();

            Assert.That(_playingRequests, Is.EqualTo(new[] { false }));
        }

        [Test]
        public void Save_AfterAFailure_TheNextAttemptIsCheckedAgain()
        {
            _dirtyPanels.Add(new FakePanel { SavesSucceed = false });
            _answer = UnsavedChangesChoice.Save;
            _gate.OnExitingEditMode();
            RunDeferred();

            _gate.OnExitingEditMode();
            RunDeferred();

            Assert.That(_asked, Is.EqualTo(2));
        }

        [Test]
        public void Save_RestartsPlayModeOnALaterTickThanTheSaveCompleting()
        {
            _dirtyPanels.Add(new FakePanel());
            _answer = UnsavedChangesChoice.Save;
            _gate.OnExitingEditMode();
            _deferred.Dequeue()(); // The question; the save completes right inside it

            Assert.That(_playingRequests, Is.EqualTo(new[] { false }));
            Assert.That(_deferred.Count, Is.EqualTo(1));
        }

        [Test]
        public void Discard_DiscardsEveryPanelThenRestarts()
        {
            var first = new FakePanel();
            var second = new FakePanel();
            _dirtyPanels.Add(first);
            _dirtyPanels.Add(second);
            _answer = UnsavedChangesChoice.Discard;

            _gate.OnExitingEditMode();
            RunDeferred();

            Assert.That(first.Discards, Is.EqualTo(1));
            Assert.That(second.Discards, Is.EqualTo(1));
            Assert.That(_playingRequests, Is.EqualTo(new[] { false, true }));
        }

        [Test]
        public void TheRestartedPlayMode_IsNotAskedAboutAgain()
        {
            _dirtyPanels.Add(new FakePanel());
            _answer = UnsavedChangesChoice.Discard;
            _gate.OnExitingEditMode();
            RunDeferred();

            _gate.OnExitingEditMode(); // The one raised by our own restart
            RunDeferred();

            Assert.That(_asked, Is.EqualTo(1));
            Assert.That(_playingRequests, Is.EqualTo(new[] { false, true }));
        }

        [Test]
        public void AfterTheRestartedPlayModeStarted_LaterAttemptsAreCheckedAgain()
        {
            _dirtyPanels.Add(new FakePanel());
            _answer = UnsavedChangesChoice.Discard;
            _gate.OnExitingEditMode();
            RunDeferred();
            _gate.OnExitingEditMode();

            _gate.OnExitingEditMode();
            RunDeferred();

            Assert.That(_asked, Is.EqualTo(2));
        }

        [Test]
        public void ExitingEditModeWhileAwaitingTheAnswer_IsIgnored()
        {
            _dirtyPanels.Add(new FakePanel());

            _gate.OnExitingEditMode();
            _gate.OnExitingEditMode();
            RunDeferred();

            Assert.That(_asked, Is.EqualTo(1));
        }

        private class FakePanel : IGuardedPanel
        {
            public bool CompleteSavesAutomatically = true;
            public bool SavesSucceed = true;
            public int Saves;
            public int Discards;
            private Action<bool> _onFinished;

            public bool IsDirty => true;

            public void SaveThen(Action<bool> onFinished)
            {
                Saves++;
                _onFinished = onFinished;
                if (CompleteSavesAutomatically)
                {
                    CompleteSave();
                }
            }

            public void CompleteSave()
            {
                _onFinished?.Invoke(SavesSucceed);
            }

            public void Discard()
            {
                Discards++;
            }

            public void CloseForAssemblyReload()
            {
            }
        }
    }
}

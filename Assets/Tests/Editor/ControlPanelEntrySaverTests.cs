using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AtMycelia.Myceliarium.Tests
{
    public class ControlPanelEntrySaverTests
    {
        private TestSaver _saver;
        private FakeEntry _entry;
        private List<string> _outcomes;
        private List<IControlPanelEntry> _saveCompletedFor;
        private EntrySaveRequest _request;

        [SetUp]
        public void SetUp()
        {
            _saver = new TestSaver();
            _entry = new FakeEntry();
            _outcomes = new List<string>();
            _saveCompletedFor = new List<IControlPanelEntry>();
            _request = new EntrySaveRequest(_entry,
                onComplete: () => _outcomes.Add("complete"),
                onFail: failedEntry => _outcomes.Add("fail"));
            ControlPanelSignals.SaveCompleted += _saveCompletedFor.Add;
        }

        [TearDown]
        public void TearDown()
        {
            ControlPanelSignals.SaveCompleted -= _saveCompletedFor.Add;
        }

        private void RunToTheEnd()
        {
            var sequence = CoroutineGuard.Run(_saver.SaveProcessSequence(_request),
                error => throw error);
            while (sequence.MoveNext())
            {
            }
        }

        [Test]
        public void SaveProcessSequence_RunsTheSaveProcess_ThenReportsSuccess()
        {
            RunToTheEnd();

            Assert.That(_saver.TimesSaveProcessRan, Is.EqualTo(1));
            Assert.That(_outcomes, Is.EqualTo(new[] { "complete" }));
            Assert.That(_saveCompletedFor, Is.EqualTo(new[] { _entry }));
        }

        [Test]
        public void SaveProcessSequence_WaitsForTheUiBeforeSaving()
        {
            var sequence = _saver.SaveProcessSequence(_request);

            sequence.MoveNext(); // Gets as far as the nested wait for the UI
            Assert.That(_saver.TimesSaveProcessRan, Is.EqualTo(0));

            var drain = CoroutineGuard.Run(sequence, error => throw error);
            while (drain.MoveNext())
            {
            }
            Assert.That(_saver.TimesSaveProcessRan, Is.EqualTo(1));
        }

        [Test]
        public void SaveProcessSequence_EntryNotLoaded_SkipsTheSaveProcess_AndStillReports()
        {
            _entry.IsLoaded = false;

            RunToTheEnd();

            Assert.That(_saver.TimesSaveProcessRan, Is.EqualTo(0));
            Assert.That(_outcomes, Is.EqualTo(new[] { "complete" }));
            Assert.That(_saveCompletedFor, Is.Empty);
        }

        [Test]
        public void SaveProcessSequence_WaitsForProcessesThatYieldOverSeveralFrames()
        {
            _saver.FramesToTake = 3;

            RunToTheEnd();

            Assert.That(_saver.TimesSaveProcessRan, Is.EqualTo(1));
            Assert.That(_outcomes, Is.EqualTo(new[] { "complete" }));
        }

        [Test]
        public void SaveProcessSequence_ProcessThrows_ReportsFailureOnly()
        {
            _saver.ThrowWith = new InvalidOperationException("Nope");
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("Nope"));

            RunToTheEnd();

            Assert.That(_outcomes, Is.EqualTo(new[] { "fail" }));
            Assert.That(_saveCompletedFor, Is.Empty);
        }

        [Test]
        public void SaveProcessSequence_ProcessThrowsAfterYielding_ReportsFailureOnly()
        {
            _saver.FramesToTake = 2;
            _saver.ThrowWith = new InvalidOperationException("Late");
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("Late"));

            RunToTheEnd();

            Assert.That(_outcomes, Is.EqualTo(new[] { "fail" }));
        }

        [Test]
        public void Save_IncompatibleEntry_ReportsFailureRightAway()
        {
            _saver.Compatible = false;

            _saver.Save(_request);

            Assert.That(_outcomes, Is.EqualTo(new[] { "fail" }));
            Assert.That(_saver.TimesSaveProcessRan, Is.EqualTo(0));
        }

        #region Test Doubles
        private class TestSaver : ControlPanelEntrySaver
        {
            public bool Compatible = true;
            public int FramesToTake;
            public Exception ThrowWith;
            public int TimesSaveProcessRan;

            // The parameterless ctor is needed because CpEntrySaverReg's TypeCache scan
            // picks this class up and tries to instantiate it.
            public TestSaver() { }

            public override bool IsCompatibleWith(IControlPanelEntry toSaveFor) => Compatible;

            protected override IEnumerator SaveProcess(EntrySaveRequest request)
            {
                TimesSaveProcessRan++;
                for (int i = 0; i < FramesToTake; i++)
                {
                    yield return null;
                }

                if (ThrowWith != null)
                {
                    throw ThrowWith;
                }
            }
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
            public bool IsInitted => true;
            public bool IsLoaded { get; set; } = true;
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

    public class CoroutineGuardTests
    {
        [Test]
        public void Run_FlattensNestedCoroutines_InOrder()
        {
            var steps = new List<string>();
            IEnumerator Inner()
            {
                steps.Add("inner1");
                yield return null;
                steps.Add("inner2");
            }
            IEnumerator Outer()
            {
                steps.Add("outer1");
                yield return Inner();
                steps.Add("outer2");
            }

            var guarded = CoroutineGuard.Run(Outer(), error => Assert.Fail("Unexpected error"));
            int yields = 0;
            while (guarded.MoveNext())
            {
                yields++;
            }

            Assert.That(steps, Is.EqualTo(new[] { "outer1", "inner1", "inner2", "outer2" }));
            Assert.That(yields, Is.EqualTo(1)); // Only the null; the nested one is stepped here
        }

        [Test]
        public void Run_ExceptionInANestedCoroutine_GoesToTheCallbackOnce_AndStopsEverything()
        {
            var errors = new List<Exception>();
            bool ranAfterTheThrow = false;
            IEnumerator Inner()
            {
                yield return null;
                throw new InvalidOperationException("Boom");
            }
            IEnumerator Outer()
            {
                yield return Inner();
                ranAfterTheThrow = true;
            }

            var guarded = CoroutineGuard.Run(Outer(), errors.Add);
            while (guarded.MoveNext())
            {
            }

            Assert.That(errors.Count, Is.EqualTo(1));
            Assert.That(errors[0].Message, Is.EqualTo("Boom"));
            Assert.That(ranAfterTheThrow, Is.False);
        }

        [Test]
        public void Run_NoException_NeverCallsTheCallback()
        {
            IEnumerator Fine()
            {
                yield return null;
            }
            int errors = 0;

            var guarded = CoroutineGuard.Run(Fine(), error => errors++);
            while (guarded.MoveNext())
            {
            }

            Assert.That(errors, Is.EqualTo(0));
        }
    }

    public class EntrySaveRequestTests
    {
        [Test]
        public void ReportSuccess_CallsOnComplete_Once()
        {
            int completed = 0;
            var request = new EntrySaveRequest(null, onComplete: () => completed++);

            request.ReportSuccess();
            request.ReportSuccess();

            Assert.That(completed, Is.EqualTo(1));
            Assert.That(request.IsReported, Is.True);
        }

        [Test]
        public void ReportFailure_CallsOnFailWithTheEntry_Once()
        {
            var failedWith = new List<IControlPanelEntry>();
            var request = new EntrySaveRequest(null, onFail: failedWith.Add);

            request.ReportFailure();
            request.ReportFailure();

            Assert.That(failedWith.Count, Is.EqualTo(1));
        }

        [Test]
        public void OnlyTheFirstReportCounts()
        {
            int completed = 0;
            int failed = 0;
            var request = new EntrySaveRequest(null, () => completed++, entry => failed++);

            request.ReportFailure();
            request.ReportSuccess();

            Assert.That(completed, Is.EqualTo(0));
            Assert.That(failed, Is.EqualTo(1));
        }

        [Test]
        public void Reports_WithoutCallbacks_DoNotThrow()
        {
            var request = new EntrySaveRequest(null);

            Assert.DoesNotThrow(() => request.ReportSuccess());
            Assert.DoesNotThrow(() => new EntrySaveRequest(null).ReportFailure());
        }
    }
}

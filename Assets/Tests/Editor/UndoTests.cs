using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium.Tests
{
    public class UndoTests
    {
        private sealed class Counter
        {
            public int Value;

            public IUndoableCommand Add(int amount)
            {
                return new DelegateCommand(() => Value += amount, () => Value -= amount);
            }
        }

        private sealed class UndoTestEntry : ControlPanelEntry
        {
            public override string SortingName => "UndoTest";
            protected override void PrepareLeftSidebarTab() { }

            public void Do(IUndoableCommand command) => ExecuteUndoable(command);
        }

        #region UndoStack
        [Test]
        public void Push_ExecutesAndEnablesUndo()
        {
            var stack = new UndoStack();
            var counter = new Counter();

            stack.Push(counter.Add(5));

            Assert.That(counter.Value, Is.EqualTo(5));
            Assert.That(stack.CanUndo, Is.True);
            Assert.That(stack.CanRedo, Is.False);
        }

        [Test]
        public void UndoThenRedo_RestoresState()
        {
            var stack = new UndoStack();
            var counter = new Counter();
            stack.Push(counter.Add(5));

            Assert.That(stack.Undo(), Is.True);
            Assert.That(counter.Value, Is.EqualTo(0));
            Assert.That(stack.CanRedo, Is.True);

            Assert.That(stack.Redo(), Is.True);
            Assert.That(counter.Value, Is.EqualTo(5));
        }

        [Test]
        public void Push_AfterUndo_DiscardsRedoHistory()
        {
            var stack = new UndoStack();
            var counter = new Counter();
            stack.Push(counter.Add(1));
            stack.Undo();

            stack.Push(counter.Add(10));

            Assert.That(stack.CanRedo, Is.False);
        }

        [Test]
        public void UndoAndRedo_WhenEmpty_ReturnFalse()
        {
            var stack = new UndoStack();

            Assert.That(stack.Undo(), Is.False);
            Assert.That(stack.Redo(), Is.False);
        }

        [Test]
        public void Push_BeyondCapacity_DropsOldest()
        {
            var stack = new UndoStack(2);
            var counter = new Counter();
            stack.Push(counter.Add(1));
            stack.Push(counter.Add(10));
            stack.Push(counter.Add(100));

            stack.Undo();
            stack.Undo();

            Assert.That(stack.CanUndo, Is.False);
            Assert.That(counter.Value, Is.EqualTo(1)); // The oldest one was forgotten
        }

        [Test]
        public void Push_CommandThatThrows_IsNotRecorded()
        {
            var stack = new UndoStack();
            var bad = new DelegateCommand(() => throw new System.InvalidOperationException(), () => { });

            Assert.Throws<System.InvalidOperationException>(() => stack.Push(bad));

            Assert.That(stack.CanUndo, Is.False);
        }

        [Test]
        public void Clear_RemovesBothHistories_AndRaisesChanged()
        {
            var stack = new UndoStack();
            var counter = new Counter();
            stack.Push(counter.Add(1));
            stack.Push(counter.Add(1));
            stack.Undo();
            int changes = 0;
            stack.Changed += () => changes++;

            stack.Clear();

            Assert.That(stack.CanUndo, Is.False);
            Assert.That(stack.CanRedo, Is.False);
            Assert.That(changes, Is.EqualTo(1));
        }
        #endregion

        #region ControlPanelEntry defaults
        [Test]
        public void Entry_WithNoUndoableState_CannotUndoOrRedo()
        {
            var entry = new UndoTestEntry();

            Assert.That(entry.CanUndo, Is.False);
            Assert.That(entry.CanRedo, Is.False);
        }

        [Test]
        public void Entry_UndoWithNothingToUndo_LogsWarning()
        {
            var entry = new UndoTestEntry();

            LogAssert.Expect(UnityEngine.LogType.Warning, new System.Text.RegularExpressions.Regex("undo"));
            LogAssert.Expect(UnityEngine.LogType.Warning, new System.Text.RegularExpressions.Regex("redo"));

            entry.Undo();
            entry.Redo();
        }

        [Test]
        public void Entry_ExecuteUndoable_MakesItUndoable()
        {
            var entry = new UndoTestEntry();
            var counter = new Counter();

            entry.Do(counter.Add(3));
            entry.Undo();

            Assert.That(counter.Value, Is.EqualTo(0));
            Assert.That(entry.CanRedo, Is.True);
        }
        #endregion

        #region UndoStack.Record
        [Test]
        public void Record_DoesNotExecute_ButCanBeUndone()
        {
            var stack = new UndoStack();
            var counter = new Counter { Value = 5 }; // As if the edit already happened

            stack.Record(counter.Add(5));

            Assert.That(counter.Value, Is.EqualTo(5));
            stack.Undo();
            Assert.That(counter.Value, Is.EqualTo(0));
        }
        #endregion

        #region TextFieldEditTracker
        private static TextFieldEditTracker AttachTracker(List<FieldEdit> reported,
            object context = null)
        {
            var tracker = new TextFieldEditTracker();
            tracker.Attach(new VisualElement(), field => context, reported.Add);
            return tracker;
        }

        [Test]
        public void TextTracker_ChangedValue_ReportsOneEditWithOldAndNew()
        {
            var reported = new List<FieldEdit>();
            var tracker = AttachTracker(reported);
            var field = new TextField { value = "a" };

            tracker.BeginEdit(field);
            field.value = "ab";
            field.value = "abc"; // Several keystrokes, still one edit
            tracker.CommitPending();

            Assert.That(reported.Count, Is.EqualTo(1));
            Assert.That(reported[0].OldValue, Is.EqualTo("a"));
            Assert.That(reported[0].NewValue, Is.EqualTo("abc"));
            Assert.That(reported[0].Field, Is.SameAs(field));
        }

        [Test]
        public void TextTracker_UnchangedValue_ReportsNothing()
        {
            var reported = new List<FieldEdit>();
            var tracker = AttachTracker(reported);
            var field = new TextField { value = "a" };

            tracker.BeginEdit(field);
            field.value = "b";
            field.value = "a";
            tracker.CommitPending();

            Assert.That(reported, Is.Empty);
        }

        [Test]
        public void TextTracker_CommitPendingTwice_ReportsOnce()
        {
            var reported = new List<FieldEdit>();
            var tracker = AttachTracker(reported);
            var field = new TextField { value = "a" };
            tracker.BeginEdit(field);
            field.value = "b";

            tracker.CommitPending();
            tracker.CommitPending();

            Assert.That(reported.Count, Is.EqualTo(1));
        }

        [Test]
        public void TextTracker_BeginningAnotherField_CommitsThePreviousOne()
        {
            var reported = new List<FieldEdit>();
            var tracker = AttachTracker(reported);
            var first = new TextField { value = "a" };
            var second = new TextField { value = "x" };
            tracker.BeginEdit(first);
            first.value = "b";

            tracker.BeginEdit(second);

            Assert.That(reported.Count, Is.EqualTo(1));
            Assert.That(reported[0].Field, Is.SameAs(first));
        }

        [Test]
        public void TextTracker_ContextIsCapturedWhenEditStarts()
        {
            var reported = new List<FieldEdit>();
            object current = "first target";
            var tracker = new TextFieldEditTracker();
            tracker.Attach(new VisualElement(), field => current, reported.Add);
            var textField = new TextField { value = "a" };

            tracker.BeginEdit(textField);
            current = "second target"; // Selection changed before focus-out
            textField.value = "b";
            tracker.CommitPending();

            Assert.That(reported[0].Context, Is.EqualTo("first target"));
        }

        [Test]
        public void FieldEdit_ToCommand_AppliesNewOnExecuteAndOldOnUndo()
        {
            var edit = new FieldEdit(new TextField(), null, "old", "new");
            string state = "new"; // Already applied by the user
            var stack = new UndoStack();

            stack.Record(edit.ToCommand((e, value) => state = (string)value));
            stack.Undo();
            Assert.That(state, Is.EqualTo("old"));

            stack.Redo();
            Assert.That(state, Is.EqualTo("new"));
        }
        #endregion

        #region ControlPanelUndoRouter
        [Test]
        public void Router_NoEntry_DoesNothing()
        {
            Assert.That(ControlPanelUndoRouter.Route(null, redo: false), Is.False);
        }

        [Test]
        public void Router_Undo_AppliesToDisplayedEntryOnly()
        {
            var shown = new UndoTestEntry();
            var other = new UndoTestEntry();
            var shownCounter = new Counter();
            var otherCounter = new Counter();
            shown.Do(shownCounter.Add(1));
            other.Do(otherCounter.Add(1));

            bool handled = ControlPanelUndoRouter.Route(shown, redo: false);

            Assert.That(handled, Is.True);
            Assert.That(shownCounter.Value, Is.EqualTo(0));
            Assert.That(otherCounter.Value, Is.EqualTo(1));
        }

        [Test]
        public void Router_Redo_AppliesRedo()
        {
            var entry = new UndoTestEntry();
            var counter = new Counter();
            entry.Do(counter.Add(1));
            entry.Undo();

            bool handled = ControlPanelUndoRouter.Route(entry, redo: true);

            Assert.That(handled, Is.True);
            Assert.That(counter.Value, Is.EqualTo(1));
        }

        [Test]
        public void Router_NothingToUndo_DoesNotCallUndo()
        {
            var entry = new UndoTestEntry();

            bool handled = ControlPanelUndoRouter.Route(entry, redo: false);

            Assert.That(handled, Is.False);
            // No warning is expected here: the router checks CanUndo first.
        }
        #endregion
    }
}

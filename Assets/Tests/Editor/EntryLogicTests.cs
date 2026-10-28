using NUnit.Framework;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium.Tests
{
    public class EntryLogicTests
    {
        private sealed class Counter
        {
            public int Value;

            public IUndoableCommand Add(int amount)
            {
                return new DelegateCommand(() => Value += amount, () => Value -= amount);
            }
        }

        private sealed class CounterLogic : ControlPanelEntryLogic
        {
            public int Value;

            protected override void AdoptState(object loadResult)
            {
                Value = (int)loadResult;
            }

            public override IUndoableCommand CreateEditCommand(FieldEdit edit)
            {
                if (edit.Field.name != "Counter")
                {
                    return null;
                }
                return edit.ToCommand((e, value) => Value = (int)value);
            }
        }

        private sealed class LogicEntry : ControlPanelEntry
        {
            public override string SortingName => "Logic";
            protected override void PrepareLeftSidebarTab() { }
            protected override void PrepareLogic() => Logic = new CounterLogic();

            public CounterLogic CounterLogic => (CounterLogic)Logic;
            public void Do(IUndoableCommand command) => ExecuteUndoable(command);
            public void RaiseEdit(FieldEdit edit) => OnSubwindowEdit(edit);
        }

        #region UndoStack clean point
        [Test]
        public void CleanPoint_FreshStack_IsClean()
        {
            Assert.That(new UndoStack().IsClean, Is.True);
        }

        [Test]
        public void CleanPoint_PushThenUndo_BecomesCleanAgain()
        {
            var stack = new UndoStack();
            var counter = new Counter();

            stack.Push(counter.Add(1));
            Assert.That(stack.IsClean, Is.False);

            stack.Undo();
            Assert.That(stack.IsClean, Is.True);

            stack.Redo();
            Assert.That(stack.IsClean, Is.False);
        }

        [Test]
        public void CleanPoint_MarkClean_MovesTheCleanPointToWhereWeAre()
        {
            var stack = new UndoStack();
            var counter = new Counter();
            stack.Push(counter.Add(1));
            stack.MarkClean();
            Assert.That(stack.IsClean, Is.True);

            stack.Push(counter.Add(1));
            Assert.That(stack.IsClean, Is.False);

            stack.Undo();
            Assert.That(stack.IsClean, Is.True);
        }

        [Test]
        public void CleanPoint_DiscardedByNewCommandAfterUndo_StaysDirty()
        {
            var stack = new UndoStack();
            var counter = new Counter();
            stack.Push(counter.Add(1));
            stack.Push(counter.Add(1));
            stack.MarkClean();   // After two commands
            stack.Undo();        // Back to one, with the clean state redoable
            stack.Push(counter.Add(5)); // ... which this throws away

            Assert.That(stack.IsClean, Is.False);
            stack.Undo();
            Assert.That(stack.IsClean, Is.False);
            stack.Undo();
            Assert.That(stack.IsClean, Is.False); // The original state isn't the saved one
        }

        [Test]
        public void CleanPoint_TrimmedByCapacity_StaysDirty()
        {
            var stack = new UndoStack(2);
            var counter = new Counter();
            stack.Push(counter.Add(1));
            stack.Push(counter.Add(1));
            stack.Push(counter.Add(1)); // Drops the first command, and the way back to the start

            stack.Undo();
            stack.Undo();

            Assert.That(stack.CanUndo, Is.False);
            Assert.That(stack.IsClean, Is.False);
        }

        [Test]
        public void CleanPoint_Clear_KeepsWhetherItWasClean()
        {
            var cleanStack = new UndoStack();
            var counter = new Counter();
            cleanStack.Push(counter.Add(1));
            cleanStack.MarkClean();
            cleanStack.Clear();
            Assert.That(cleanStack.IsClean, Is.True);

            var dirtyStack = new UndoStack();
            dirtyStack.Push(counter.Add(1));
            dirtyStack.Clear();
            Assert.That(dirtyStack.IsClean, Is.False);
        }
        #endregion

        #region ControlPanelEntryLogic
        [Test]
        public void Logic_Load_AdoptsStateClearsHistoryAndIsClean()
        {
            var logic = new CounterLogic();
            logic.History.Push(new Counter().Add(1));
            Assert.That(logic.HasUnsavedChanges, Is.True);

            logic.Load(7);

            Assert.That(logic.Value, Is.EqualTo(7));
            Assert.That(logic.History.CanUndo, Is.False);
            Assert.That(logic.HasUnsavedChanges, Is.False);
        }

        [Test]
        public void Logic_HasUnsavedChanges_FollowsTheHistory_AndRaisesTheEvent()
        {
            var logic = new CounterLogic();
            int raised = 0;
            logic.UnsavedChangesMayHaveChanged += () => raised++;

            logic.History.Record(new Counter().Add(1));
            Assert.That(logic.HasUnsavedChanges, Is.True);
            Assert.That(raised, Is.GreaterThan(0));

            logic.History.Undo();
            Assert.That(logic.HasUnsavedChanges, Is.False);
        }

        [Test]
        public void Logic_MarkSaved_MakesTheCurrentStateClean()
        {
            var logic = new CounterLogic();
            logic.History.Record(new Counter().Add(1));

            logic.MarkSaved();

            Assert.That(logic.HasUnsavedChanges, Is.False);
        }
        #endregion

        #region ControlPanelEntry with a logic module
        [Test]
        public void Entry_WithLogic_UsesTheLogicsHistoryAndUnsavedFlag()
        {
            var entry = new LogicEntry();
            entry.Init();
            var counter = new Counter();

            entry.Do(counter.Add(2));

            Assert.That(entry.CanUndo, Is.True);
            Assert.That(entry.HasUnsavedChanges, Is.True);
            Assert.That(entry.CounterLogic.History.CanUndo, Is.True);

            entry.Undo();

            Assert.That(counter.Value, Is.EqualTo(0));
            Assert.That(entry.HasUnsavedChanges, Is.False); // Back at the saved state
        }

        [Test]
        public void Entry_WithLogic_RaisesUnsavedChangesSignal()
        {
            var entry = new LogicEntry();
            entry.Init();
            int signals = 0;
            System.Action<IControlPanelEntry> handler = e => { if (e == entry) signals++; };
            ControlPanelSignals.UnsavedChangesChanged += handler;
            try
            {
                entry.Do(new Counter().Add(1));
            }
            finally
            {
                ControlPanelSignals.UnsavedChangesChanged -= handler;
                entry.Dispose();
            }

            Assert.That(signals, Is.GreaterThan(0));
        }

        [Test]
        public void Entry_WithLogic_DefaultSubwindowEditHandlingRecordsTheLogicsCommand()
        {
            var entry = new LogicEntry();
            entry.Init();
            entry.CounterLogic.Value = 5; // As if the user already changed it to 5
            var field = new IntegerField { name = "Counter" };

            entry.RaiseEdit(new FieldEdit(field, null, 1, 5));
            entry.Undo();

            Assert.That(entry.CounterLogic.Value, Is.EqualTo(1));
            entry.Redo();
            Assert.That(entry.CounterLogic.Value, Is.EqualTo(5));
        }

        [Test]
        public void Entry_WithLogic_EditTheLogicDoesntHandleIsIgnored()
        {
            var entry = new LogicEntry();
            entry.Init();

            entry.RaiseEdit(new FieldEdit(new IntegerField { name = "Other" }, null, 1, 5));

            Assert.That(entry.CanUndo, Is.False);
        }

        [Test]
        public void Entry_Reinit_StartsWithFreshLogic()
        {
            var entry = new LogicEntry();
            entry.Init();
            entry.Do(new Counter().Add(1));
            var logicBefore = entry.CounterLogic;

            entry.Init(forceReinit: true);

            Assert.That(entry.CounterLogic, Is.Not.SameAs(logicBefore));
            Assert.That(entry.CanUndo, Is.False);
            Assert.That(entry.HasUnsavedChanges, Is.False);
        }
        #endregion
    }
}

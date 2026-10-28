using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
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

            public void RaiseEdit(FieldEdit edit) => OnSubwindowEdit(edit);
        }

        private sealed class LogicEntry : ControlPanelEntry
        {
            public override string SortingName => "Logic";
            public override bool IsMeantToHaveSubwindow => false;
            protected override Type SidebarTabType => null;
            protected override void PrepareMainSidebarTab() { }
            protected override Type LogicType => typeof(CounterLogic);

            public CounterLogic CounterLogic => (CounterLogic)Logic;
            public void Do(IUndoableCommand command) => Logic.History.Push(command);
        }

        private sealed class NotALogic { }

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

            Assert.That(entry.Logic.CanUndo, Is.True);
            Assert.That(entry.HasUnsavedChanges, Is.True);
            Assert.That(entry.CounterLogic.History.CanUndo, Is.True);

            entry.Logic.Undo();

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

            entry.CounterLogic.RaiseEdit(new FieldEdit(field, null, 1, 5));
            entry.Logic.Undo();

            Assert.That(entry.CounterLogic.Value, Is.EqualTo(1));
            entry.Logic.Redo();
            Assert.That(entry.CounterLogic.Value, Is.EqualTo(5));
        }

        [Test]
        public void Entry_WithLogic_EditTheLogicDoesntHandleIsIgnored()
        {
            var entry = new LogicEntry();
            entry.Init();

            entry.CounterLogic.RaiseEdit(
                new FieldEdit(new IntegerField { name = "Other" }, null, 1, 5));

            Assert.That(entry.Logic.CanUndo, Is.False);
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
            Assert.That(entry.Logic.CanUndo, Is.False);
            Assert.That(entry.HasUnsavedChanges, Is.False);
        }

        [Test]
        public void Entry_Dispose_DisposesItsLogic_SoItStopsReactingToSignals()
        {
            var entry = new LogicEntry();
            entry.Init();
            var logic = entry.CounterLogic;
            entry.Do(new Counter().Add(1));
            Assert.That(logic.HasUnsavedChanges, Is.True);

            entry.Dispose();
            logic.History.Record(new Counter().Add(1)); // Dirty again, as far as it knows
            ControlPanelSignals.SaveCompleted(entry);

            Assert.That(logic.HasUnsavedChanges, Is.True); // Didn't hear about the save
            Assert.That(entry.HasUnsavedChanges, Is.False); // And the entry has no logic now
        }

        [Test]
        public void Entry_SaveCompleted_MarksItsLogicSaved()
        {
            var entry = new LogicEntry();
            entry.Init();
            entry.Do(new Counter().Add(1));

            ControlPanelSignals.SaveCompleted(entry);

            Assert.That(entry.HasUnsavedChanges, Is.False);
            entry.Dispose();
        }

        [Test]
        public void Entry_WithAnInvalidLogicType_FallsBackToTheDefaultLogic()
        {
            var entry = new BadLogicEntry();
            LogAssert.Expect(LogType.Error, new Regex("NotALogic"));

            entry.Init();

            Assert.That(entry.Logic, Is.InstanceOf<DefaultControlPanelEntryLogic>());
        }

        private sealed class BadLogicEntry : ControlPanelEntry
        {
            public override string SortingName => "BadLogic";
            public override bool IsMeantToHaveSubwindow => false;
            protected override Type SidebarTabType => null;
            protected override void PrepareMainSidebarTab() { }
            protected override Type LogicType => typeof(NotALogic);
        }
        #endregion

        #region ControlPanelModuleFactory
        [Test]
        public void Factory_ValidType_CreatesAnInstance()
        {
            var created = ControlPanelModuleFactory.Create<IControlPanelEntryLogic>(
                typeof(DefaultControlPanelEntryLogic), "logic");

            Assert.That(created, Is.InstanceOf<DefaultControlPanelEntryLogic>());
        }

        [Test]
        public void Factory_NullType_LogsAndReturnsNull()
        {
            LogAssert.Expect(LogType.Error, new Regex("logic"));

            var created = ControlPanelModuleFactory.Create<IControlPanelEntryLogic>(null, "logic");

            Assert.That(created, Is.Null);
        }

        [Test]
        public void Factory_TypeOfTheWrongKind_LogsAndReturnsNull()
        {
            LogAssert.Expect(LogType.Error, new Regex("NotALogic"));

            var created = ControlPanelModuleFactory.Create<IControlPanelEntryLogic>(
                typeof(NotALogic), "logic");

            Assert.That(created, Is.Null);
        }

        [Test]
        public void Factory_TypeThatCantBeInstantiated_LogsAndReturnsNull()
        {
            LogAssert.Expect(LogType.Error, new Regex("logic"));

            var created = ControlPanelModuleFactory.Create<IControlPanelEntryLogic>(
                typeof(ControlPanelEntryLogic), "logic"); // Abstract

            Assert.That(created, Is.Null);
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// A single undoable change. Redoing a command is just executing it again, so
    /// Execute should be safe to call repeatedly after each Undo.
    /// </summary>
    public interface IUndoableCommand
    {
        void Execute();
        void Undo();
    }

    /// <summary>
    /// Convenience command built from two delegates.
    /// </summary>
    public sealed class DelegateCommand : IUndoableCommand
    {
        public DelegateCommand(Action execute, Action undo)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _undo = undo ?? throw new ArgumentNullException(nameof(undo));
        }

        private readonly Action _execute;
        private readonly Action _undo;

        public void Execute() => _execute();
        public void Undo() => _undo();
    }

    /// <summary>
    /// Undo/redo history for one ControlPanelEntry. Deliberately separate from Unity's
    /// global Undo system, so Ctrl+Z in a ControlPanel only touches the displayed entry.
    /// Plain C# so it can be tested without an EditorWindow.
    /// </summary>
    public sealed class UndoStack
    {
        public const int DefaultCapacity = 100;

        public UndoStack(int capacity = DefaultCapacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Must be at least 1.");
            }
            _capacity = capacity;
        }

        private readonly int _capacity;
        private readonly List<IUndoableCommand> _undoable = new List<IUndoableCommand>();
        private readonly Stack<IUndoableCommand> _redoable = new Stack<IUndoableCommand>();

        /// <summary>
        /// Raised after any change to what can be undone or redone.
        /// </summary>
        public event Action Changed = delegate { };

        public bool CanUndo => _undoable.Count > 0;
        public bool CanRedo => _redoable.Count > 0;

        /// <summary>
        /// Executes the command and records it. Anything that was redoable is discarded.
        /// A command that throws while executing is not recorded.
        /// </summary>
        public void Push(IUndoableCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            command.Execute();
            AddUndoable(command);
        }

        /// <summary>
        /// Records a command whose effect has already been applied (say, by the user
        /// typing into a field), so it isn't executed. Otherwise the same as Push.
        /// </summary>
        public void Record(IUndoableCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            AddUndoable(command);
        }

        private void AddUndoable(IUndoableCommand command)
        {
            // Discarding the redoable commands also discards the positions after them
            if (_cleanPosition > _undoable.Count)
            {
                _cleanPosition = Unreachable;
            }

            _undoable.Add(command);
            if (_undoable.Count > _capacity)
            {
                _undoable.RemoveAt(0);
                // Every position moves down by one, and the one before the oldest command
                // can no longer be gotten back to.
                _cleanPosition = _cleanPosition > 0 ? 
                _cleanPosition - 1 : 
                Unreachable;
            }
            _redoable.Clear();
            Changed();
        }

        #region Clean point
        // The position in the history (how many commands are applied) at which the state
        // was last known to be saved. Undoing or redoing back to it makes the stack clean
        // again; anything that makes that position impossible to get back to leaves it
        // dirty until the next MarkClean.
        private const int Unreachable = -1;
        private int _cleanPosition;

        /// <summary>
        /// Whether the state is the one that was last marked clean (initially, the state
        /// before any commands).
        /// </summary>
        public bool IsClean => _cleanPosition == _undoable.Count;

        /// <summary>
        /// Says that the state as it is now is the saved one.
        /// </summary>
        public void MarkClean()
        {
            _cleanPosition = _undoable.Count;
            Changed();
        }
        #endregion

        /// <returns>Whether anything was undone.</returns>
        public bool Undo()
        {
            if (!CanUndo)
            {
                return false;
            }

            int last = _undoable.Count - 1;
            var command = _undoable[last];
            command.Undo();
            // Only moved once Undo succeeds, so a throwing command stays where it was.
            _undoable.RemoveAt(last);
            _redoable.Push(command);
            Changed();
            return true;
        }

        /// <returns>Whether anything was redone.</returns>
        public bool Redo()
        {
            if (!CanRedo)
            {
                return false;
            }

            var command = _redoable.Peek();
            command.Execute();
            _redoable.Pop();
            _undoable.Add(command);
            Changed();
            return true;
        }

        /// <summary>
        /// Forgets all commands. The state itself isn't touched, so a clean stack stays
        /// clean, while a dirty one stays dirty (until MarkClean) since there's no longer
        /// a way back to the clean state.
        /// </summary>
        public void Clear()
        {
            if (!CanUndo && !CanRedo)
            {
                return;
            }

            _cleanPosition = IsClean ? 0 : Unreachable;
            _undoable.Clear();
            _redoable.Clear();
            Changed();
        }
    }
}

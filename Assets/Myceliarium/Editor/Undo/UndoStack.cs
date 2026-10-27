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
            _undoable.Add(command);
            if (_undoable.Count > _capacity)
            {
                _undoable.RemoveAt(0);
            }
            _redoable.Clear();
            Changed();
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

            _undoable.Add(command);
            if (_undoable.Count > _capacity)
            {
                _undoable.RemoveAt(0);
            }
            _redoable.Clear();
            Changed();
        }

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

        public void Clear()
        {
            if (!CanUndo && !CanRedo)
            {
                return;
            }

            _undoable.Clear();
            _redoable.Clear();
            Changed();
        }
    }
}

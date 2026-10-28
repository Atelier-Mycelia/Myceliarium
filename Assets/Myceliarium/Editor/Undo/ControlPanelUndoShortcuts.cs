using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Shortcuts whose context is the ControlPanel window type are only active while a
    /// ControlPanel has focus, and take priority over the global ones. That's how Ctrl+Z
    /// gets kept away from Unity's own Undo while a panel is focused.
    /// </summary>
    internal static class ControlPanelUndoShortcuts
    {
        [Shortcut("Myceliarium/Control Panel Undo", typeof(ControlPanel),
            KeyCode.Z, ShortcutModifiers.Action)]
        private static void Undo()
        {
            Handle(redo: false);
        }

        [Shortcut("Myceliarium/Control Panel Redo", typeof(ControlPanel),
            KeyCode.Z, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        private static void Redo()
        {
            Handle(redo: true);
        }

#if UNITY_EDITOR_WIN
        // Unity's own redo is also Ctrl+Y on Windows.
        [Shortcut("Myceliarium/Control Panel Redo (Alt)", typeof(ControlPanel),
            KeyCode.Y, ShortcutModifiers.Action)]
        private static void RedoAlt()
        {
            Handle(redo: true);
        }
#endif

        private static void Handle(bool redo)
        {
            var panel = EditorWindow.focusedWindow as ControlPanel;
            // Having been invoked at all consumed the key, so global undo doesn't run
            // even when the displayed entry has nothing to undo.
            if (panel != null)
            {
                panel.HandleUndoRequest(redo);
            }
        }
    }
}

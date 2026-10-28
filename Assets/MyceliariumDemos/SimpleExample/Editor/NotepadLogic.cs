using System;
using AtMycelia.Myceliarium;

namespace SimpleExample
{
    /// <summary>
    /// Owns the text being edited (a copy of what's on disk, until the user saves) 
    /// and the undo history that goes with it.
    /// </summary>
    public class NotepadLogic : ControlPanelEntryLogic
    {
        public string Text { get; private set; } = string.Empty;

        /// <summary>
        /// Raised when undoing or redoing changed the text, so that the UI 
        /// can show it.
        /// </summary>
        public event Action<string> TextChangedByHistory = delegate { };

        protected override void AdoptState(object loadResult)
        {
            var contents = loadResult as NoteContents;
            
            Text = contents != null ?
            contents.Text :
            string.Empty;
        }

        public override IUndoableCommand CreateEditCommand(FieldEdit edit)
        {
            if (edit.Field.name != "NoteTextField")
            {
                return null;
            }

            // The user has already typed it, so the working text follows right away.
            // The command's job is just to switch between the old text and the new.
            Text = (string)edit.NewValue;
            return edit.ToCommand(ApplyText);
        }

        private void ApplyText(FieldEdit edit, object value)
        {
            Text = (string)value;
            TextChangedByHistory(Text);//
        }
    }
}

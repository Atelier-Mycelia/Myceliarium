using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AtMycelia.Myceliarium;
using UitkLabel = UnityEngine.UIElements.Label;
using UnityEngine.UIElements;
using System;

namespace SimpleExample
{
    public class NotepadSubwindow : ControlPanelSubwindow
    {
        public override string PathToUxml => "Editor/Uxml/NotepadSubwindow";

        protected override void RegisterVisualElements()
        {
            _noteTextField = Root.Q<TextField>("NoteTextField");
        }

        private TextField _noteTextField;

        protected override void ConfigureVisualElements()
        {
            _noteTextField.SetValueWithoutNotify(string.Empty);
            _noteTextField.multiline = true;
            _noteTextField.isDelayed = true; 

            _noteTextField.style.whiteSpace = WhiteSpace.Normal;
        }

        protected override void SetSubs(bool wantsSubsActive)
        {
            base.SetSubs(wantsSubsActive);
            if (wantsSubsActive)
            {
                _noteTextField.RegisterValueChangedCallback(OnNoteTextFieldValueChanged);
            }
            else
            {
                _noteTextField.UnregisterValueChangedCallback(OnNoteTextFieldValueChanged);
            }
        }

        private void OnNoteTextFieldValueChanged(ChangeEvent<string> evt)
        {
            NoteTextUiChanged.Invoke(_noteTextField.value);
        }
        private void OnNoteTextFieldFocusOut(FocusOutEvent evt)
        {
            // Handle the focus out event for the note text field
            NoteTextUiChanged.Invoke(_noteTextField.value);
        }

        public event Action<string> NoteTextUiChanged = delegate { };
        public string NoteText
        {
            get => _noteTextField.value;
        }

        public void SetNoteText(string newText, bool triggerEvents = true)
        {
            if (triggerEvents)
            {
                _noteTextField.value = newText;
            }
            else
            {
                _noteTextField.SetValueWithoutNotify(newText);
            }
        }

    }
}
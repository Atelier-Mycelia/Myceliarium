using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AtMycelia.Myceliarium;
using UnityObj = UnityEngine.Object;

namespace SimpleExample
{
    public class NotepadEntry : ControlPanelEntry, ISimpleExampleEntry
    {
        public override bool IsTopLevel => true;
        public override string SortingName => "Notepad";

        protected override void PrepareLoader()
        {
            Loader = new NotepadLoader();
        }

        protected override void OnLoadingDone()
        {
            var contentsOnDisk = Loader.GetLoadResult<NoteContents>();
            _workingState = UnityObj.Instantiate(contentsOnDisk);
            NpSubwindow.SetNoteText(_workingState.Text, false);
            base.OnLoadingDone();
        }

        public NoteContents WorkingState => _workingState;
        private NoteContents _workingState;

        protected override void PrepareLeftSidebarTab()
        {
            Tab = new NotepadTab();
            Tab.Init();
        }

        protected override void PrepareSubwindow()
        {
            _subwindow = new NotepadSubwindow();
            _subwindow.Init();
        }

        protected override void SetSubs(bool wantsSubsActive)
        {
            // At this point, the subwindow should be set up and ready to go.
            // No need for null-checks.
            base.SetSubs(wantsSubsActive);
            if (wantsSubsActive)
            {
                NpSubwindow.NoteTextUiChanged += OnNoteTextUiChanged;
            }
            else
            {
                NpSubwindow.NoteTextUiChanged -= OnNoteTextUiChanged;
            }
        }

        private NotepadSubwindow NpSubwindow => _subwindow as NotepadSubwindow;

        private void OnNoteTextUiChanged(string newText)
        {
            HasUnsavedChanges = newText != _baseText;
            _workingState.Text = newText;
        }

        private string _baseText = string.Empty;

        protected override void SetCurrentStateAsBase()
        {
            _baseText = NpSubwindow.NoteText;
            if (_workingState != null)
            {
                _workingState.Text = _baseText;
            }
            base.SetCurrentStateAsBase();
        }

        public override void Dispose()
        {
            _workingState = null;
            _baseText = string.Empty;
            base.Dispose();
        }

    }

    public interface ISimpleExampleEntry : IControlPanelEntry
    {
    }
}
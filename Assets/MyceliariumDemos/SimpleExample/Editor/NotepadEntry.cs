using System;
using AtMycelia.Myceliarium;

namespace SimpleExample
{
    public class NotepadEntry : ControlPanelEntry, ISimpleExampleEntry
    {
        public override bool IsTopLevel => true;
        public override string SortingName => "Notepad";

        protected override Type SidebarTabType => typeof(NotepadTab);
        protected override Type SubwindowType => typeof(NotepadSubwindow);
        protected override Type LoaderType => typeof(NotepadLoader);
        protected override Type LogicType => typeof(NotepadLogic);

        private NotepadSubwindow NpSubwindow => _subwindow as NotepadSubwindow;

        protected override void SetSubs(bool wantsSubsActive)
        {
            base.SetSubs(wantsSubsActive);
            if (wantsSubsActive)
            {
                NpLogic.TextChangedByHistory += OnTextChangedByHistory;
            }
            else
            {
                NpLogic.TextChangedByHistory -= OnTextChangedByHistory;
            }
        }

        private NotepadLogic NpLogic => Logic as NotepadLogic;

        private void OnTextChangedByHistory(string text)
        {
            NpSubwindow.SetNoteText(text, false);
        }

        protected override void OnLoadingDone()
        {
            base.OnLoadingDone(); // Loads the logic, then refreshes and shows the subwindow
            NpSubwindow.SetNoteText(NpLogic.Text, false);
        }

        /// <summary>
        /// The text as the user has it right now, which isn't necessarily what's on disk.
        /// </summary>
        public string WorkingText => NpLogic.Text;
    }

    public interface ISimpleExampleEntry : IControlPanelEntry
    {
    }
}

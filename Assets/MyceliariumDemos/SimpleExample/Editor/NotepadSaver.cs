using AtMycelia;
using AtMycelia.Myceliarium;
using System;
using System.Collections;

namespace SimpleExample
{
    public class NotepadSaver : ControlPanelEntrySaver, ISimpleExampleSaver
    {
        public override bool IsCompatibleWith(IControlPanelEntry toSaveFor)
        {
            return toSaveFor is NotepadEntry;
        }

        protected override IEnumerator SaveProcess(IControlPanelEntry toSaveFor, Action onComplete)
        {
            yield return base.SaveProcess(toSaveFor, onComplete);
            var contentsOnDisk = SOUtils.EnsureSOExists<NoteContents>("SimpleExample", 
                "NotepadContents");
            var entry = toSaveFor as NotepadEntry;
            contentsOnDisk.Text = entry.WorkingState.Text;

            contentsOnDisk.MarkDirtyAndSave();
            onComplete?.Invoke();
            ControlPanelSignals.SaveCompleted?.Invoke(toSaveFor);
            yield return null;
        }
    }

    public interface ISimpleExampleSaver : IControlPanelEntrySaver
    {
    }
}
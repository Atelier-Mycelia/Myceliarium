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

        protected override IEnumerator SaveProcess(EntrySaveRequest request)
        {
            var contentsOnDisk = SOUtils.EnsureSOExists<NoteContents>("SimpleExample",
                "NotepadContents");
            var entry = request.Entry as NotepadEntry;
            contentsOnDisk.Text = entry.WorkingText;
            contentsOnDisk.MarkDirtyAndSave();
            yield break;
        }
    }

    public interface ISimpleExampleSaver : IControlPanelEntrySaver
    {
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AtMycelia.Myceliarium;
using AtMycelia;

namespace SimpleExample
{
    public class NotepadLoader : ControlPanelEntryLoader
    {
        protected override IEnumerator LoadProcess(IControlPanelEntry toLoadFor, Action onComplete)
        {
            _loadResult = SOUtils.EnsureSOExists<NoteContents>("SimpleExample", "NotepadContents");
            onComplete?.Invoke();
            yield break;
        }
    }

}
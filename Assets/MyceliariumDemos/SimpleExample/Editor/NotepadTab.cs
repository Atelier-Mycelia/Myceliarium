using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AtMycelia.Myceliarium;
using UnityEngine.UIElements;

namespace SimpleExample
{
    public sealed class NotepadTab : ControlPanelTab
    {
        public override string DisplayName => "Notepad";
        public override string PathToUxml => "Editor/Uxml/NotepadTab";
        protected override StyleLength OverrideSubtabHeight { get; set; } = 50f;
    }
}
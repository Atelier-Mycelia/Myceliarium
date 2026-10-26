using System;
using AtMycelia.Myceliarium;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SimpleExample
{
    public class SimpleControlPanel : ControlPanel
    {
        protected override string DisplayName => "Simple Example Control Panel";//
        protected override string PathToUxml => "Editor/Uxml/SimpleControlPanel";

        protected override Type EntrySuperType => typeof(ISimpleExampleEntry);

        protected override Type SaverSuperType => typeof(ISimpleExampleSaver);

        [MenuItem("Window/Simple Example/Control Panel")]
        public static void BringUp()
        {
            GetWindow<SimpleControlPanel>();
        }
    }
}
using AtMycelia.Myceliarium;
using System;
using UnityEditor;

namespace ComplexExample
{
	public class ComplexExampleControlPanel : ControlPanel
	{
		protected override string DisplayName => "Complex Example Control Panel";
		protected override string PathToUxml => "Editor/Uxml/CustomControlPanel";

		protected override Type EntrySuperType => typeof(ICustomExampleEntry);

		protected override Type SaverSuperType => typeof(CharaBioSaver);

		[MenuItem("Window/Complex Example/Control Panel")]
		public static void BringUp()
		{
			if (S != null)
			{
				S.Focus();
				return;
			}

			GetWindow<ComplexExampleControlPanel>();
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			if (S != null && S != this)
			{
				S.Focus();
				Close();
				return;
			}
			S = this;
		}

		public static ComplexExampleControlPanel S { get; private set; }

	}

}
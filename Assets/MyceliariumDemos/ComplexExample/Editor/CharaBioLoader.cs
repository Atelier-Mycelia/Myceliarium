using AtMycelia;
using AtMycelia.Myceliarium;
using System;
using System.Collections;

namespace ComplexExample
{
	public class CharaBioLoader : ControlPanelEntryLoader
	{
		protected override IEnumerator LoadProcess(IControlPanelEntry toLoadFor, 
			Action onComplete = null)
		{
			_loadResult = SOUtils.EnsureSOExists<CharaBioRegistry>("ComplexExample",
				"CharaBioRegistry");
			onComplete?.Invoke();
			yield break;
		}

	}
}
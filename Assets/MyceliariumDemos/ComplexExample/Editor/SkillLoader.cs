using AtMycelia;
using AtMycelia.Myceliarium;
using System;
using System.Collections;

namespace ComplexExample
{
	public class SkillLoader : ControlPanelEntryLoader
	{
		protected override IEnumerator LoadProcess(IControlPanelEntry toLoadFor,
			Action onComplete = null)
		{
			_loadResult = SOUtils.EnsureSOExists<SkillRegistry>("ComplexExample",
				"SkillRegistry");
			onComplete?.Invoke();
			yield break;
		}
	}
}

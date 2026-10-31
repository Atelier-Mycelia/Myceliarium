using System;
using System.Collections;
using System.Collections.Generic;
using AtMycelia;
using AtMycelia.Myceliarium;

namespace ComplexExample
{
	public class SkillSaver : ControlPanelEntrySaver, ICustomExampleSaver
	{
		public override bool IsCompatibleWith(IControlPanelEntry toSaveFor)
		{
			return toSaveFor is SkillEntry;
		}

		protected override IEnumerator SaveProcess(IControlPanelEntry toSaveFor, Action onComplete)
		{
			var entry = toSaveFor as SkillEntry;
			SkillRegistry registry = entry.SkillRegistry;
			registry.Clear();

			// Copies, so that editing a skill in the UI after saving doesn't immediately
			// change the on-disk registry, which would be confusing.
			var skillCopies = new List<Skill>();
			foreach (var skill in entry.SkillsInUi)
			{
				skillCopies.Add(new Skill(skill));
			}
			registry.AddRange(skillCopies);
			registry.MarkDirtyAndSave();
			onComplete?.Invoke();
			ControlPanelSignals.SaveCompleted(entry);
			yield return null;
		}
	}
}

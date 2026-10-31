using System;
using System.Collections.Generic;
using AtMycelia.Myceliarium;

namespace ComplexExample
{
	/// <summary>
	/// A stripped-down take on RPG Maker's Skills tab, meant to be a second entry for
	/// the ComplexExample panel (so there's more than one tab to switch between).
	/// </summary>
	public class SkillEntry : ControlPanelEntry, ICustomExampleEntry
	{
		public override string SortingName => "Skills";
		public override bool IsTopLevel => true;

		// The entry makes an instance of each of these (and calls Init on it).
		protected override Type SidebarTabType => typeof(SkillTab);
		protected override Type SubwindowType => typeof(SkillSubwindow);
		protected override Type LoaderType => typeof(SkillLoader);
		protected override Type LogicType => typeof(SkillLogic);

		private SkillLogic SkillsLogic => Logic as SkillLogic;
		private SkillSubwindow SkillWindow => _subwindow as SkillSubwindow;

		protected override void SetSubs(bool wantsSubsActive)
		{
			base.SetSubs(wantsSubsActive);

			// Neither exists if this was never initialized (or is already disposed)
			if (SkillsLogic == null || SkillWindow == null)
			{
				return;
			}

			// The subwindow is the UI asking for something; the logic does it and says
			// what changed, which gets shown through the handlers below.
			if (wantsSubsActive)
			{
				SkillWindow.AddRequested += SkillsLogic.AddSkill;
				SkillWindow.DeleteRequested += SkillsLogic.DeleteSkill;
				SkillsLogic.SkillsChanged += OnSkillsChanged;
				SkillsLogic.SkillChanged += OnSkillChanged;
			}
			else
			{
				SkillWindow.AddRequested -= SkillsLogic.AddSkill;
				SkillWindow.DeleteRequested -= SkillsLogic.DeleteSkill;
				SkillsLogic.SkillsChanged -= OnSkillsChanged;
				SkillsLogic.SkillChanged -= OnSkillChanged;
			}
		}

		private void OnSkillsChanged(Skill skillToShow)
		{
			SkillWindow.ShowSkills(SkillsLogic.Skills, skillToShow);
		}

		private void OnSkillChanged(Skill skill)
		{
			SkillWindow.ShowSkill(skill);
		}

		protected override void OnControlPanelOpened(IControlPanel panel)
		{
			base.OnControlPanelOpened(panel);
			if (!SkillsLogic.HasUnsavedChanges) // Otherwise loading would discard them
			{
				HandleLoading();
			}
		}

		protected override void OnLoadingDone()
		{
			var loadResult = Loader.GetLoadResult<SkillRegistry>();
			SkillsLogic.Load(loadResult);
			SkillWindow.ShowSkills(SkillsLogic.Skills);

			// Loading also happens when the panel opens, when this may not be the
			// selected entry, and its subwindow shouldn't cover that of the one that is.
			if (_tab.IsSelected)
			{
				SkillWindow.Show();
			}
		}

		/// <summary>
		/// Reference to the registry on disk.
		/// </summary>
		public SkillRegistry SkillRegistry => SkillsLogic?.Registry;

		public IReadOnlyList<Skill> SkillsInUi => SkillsLogic.Skills;
	}
}

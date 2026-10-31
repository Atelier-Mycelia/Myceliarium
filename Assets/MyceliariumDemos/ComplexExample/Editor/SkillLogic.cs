using System;
using System.Collections.Generic;
using AtMycelia.Myceliarium;
using UnityEngine;

namespace ComplexExample
{
	/// <summary>
	/// Owns the skills the user is working on (copies of what's in the registry, so
	/// editing doesn't touch the saved data until they save), along with the undo history
	/// and unsaved changes tracking that go with them. Knows nothing about the UI: the
	/// entry listens to the events and tells the subwindow what to show.
	/// </summary>
	public class SkillLogic : ControlPanelEntryLogic
	{
		public const int MinSkillCount = 10;

		protected override void AdoptState(object loadResult)
		{
			Registry = loadResult as SkillRegistry;
			_skills.Clear();
			if (Registry != null)
			{
				foreach (var elem in Registry.Skills)
				{
					_skills.Add(new Skill(elem));
				}
			}

			if (_skills.Count == 0)
			{
				var starters = SkillRegistry.StarterSkills;
				_skills.AddRange(starters);
			}

			PadToMinimum();
		}

		/// <summary>
		/// The registry on disk that the skills were loaded from (and get saved to).
		/// </summary>
		public SkillRegistry Registry { get; private set; }

		public IReadOnlyList<Skill> Skills => _skills;
		private readonly List<Skill> _skills = new List<Skill>();

		/// <summary>
		/// Tops the list up with blank skills until it has the minimum count.
		/// </summary>
		/// <returns>The blanks that had to be added.</returns>
		private List<Skill> PadToMinimum()
		{
			var padding = new List<Skill>();
			while (_skills.Count < MinSkillCount)
			{
				var blank = new Skill();
				_skills.Add(blank);
				padding.Add(blank);
			}
			return padding;
		}

		public void AddSkill()
		{
			var newSkill = new Skill(); // Kept so redoing brings back this same one
			var command = new DelegateCommand(AddSkillThenSignal, RemoveSkillThenSignal);

			void AddSkillThenSignal()
			{
				_skills.Add(newSkill);
				SkillsChanged(newSkill);
			}

			void RemoveSkillThenSignal()
			{
				_skills.Remove(newSkill);
				SkillsChanged(null);
			}

			History.Push(command);
		}

		/// <summary>
		/// Raised when skills were added or removed. The argument is the skill that's
		/// worth showing afterwards, or null if there isn't one in particular.
		/// </summary>
		public event Action<Skill> SkillsChanged = delegate { };

		public void DeleteSkill(Skill skill)
		{
			int index = _skills.IndexOf(skill);
			if (index < 0)
			{
				return;
			}

			List<Skill> padding = null; // Blanks added to keep the minimum count

			void SkillDeletion()
			{
				_skills.Remove(skill);
				padding = PadToMinimum();
				SkillsChanged(null);
			}

			History.Push(new DelegateCommand(
				execute: () =>
				{
					_skills.Remove(skill);
					padding = PadToMinimum();
					SkillsChanged(null);
				},
				undo: () =>
				{
					foreach (var blank in padding)
					{
						_skills.Remove(blank);
					}
					int whereToInsert = Mathf.Clamp(index, 0, _skills.Count);
					_skills.Insert(whereToInsert, skill);
					SkillsChanged(skill);
				}));
		}

		/// <summary>
		/// Handles edits to the detail view's fields. The view has already put the value
		/// into the skill, so the command is only recorded, not executed.
		/// </summary>
		public override IUndoableCommand CreateEditCommand(FieldEdit edit)
		{
			if (!(edit.Context is Skill))
			{
				return null;
			}

			return edit.ToCommand(ApplyEdit);
		}

		private void ApplyEdit(FieldEdit edit, object value)
		{
			var skill = (Skill)edit.Context;
			if (SkillFields.TrySet(skill, edit.Field.name, value))
			{
				SkillChanged(skill);
			}
		}

		/// <summary>
		/// Raised when an edit command changed the values of the skill (by applying it,
		/// undoing it or redoing it).
		/// </summary>
		public event Action<Skill> SkillChanged = delegate { };
	}
}

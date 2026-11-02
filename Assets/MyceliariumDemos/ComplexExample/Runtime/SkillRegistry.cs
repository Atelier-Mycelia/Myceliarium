using System.Collections.Generic;
using UnityEngine;

namespace ComplexExample
{
	public sealed class SkillRegistry : ScriptableObject
	{
		[SerializeField] private List<Skill> _skills = new List<Skill>();
		public IReadOnlyList<Skill> Skills => _skills;

		public void Clear()
		{
			_skills.Clear();
		}

		public void AddRange(IEnumerable<Skill> skills)
		{
			_skills.AddRange(skills);
		}

		/// <summary>
		/// Returns a new list of fresh copies of the starter skills.
		/// </summary>
		public static IList<Skill> StarterSkills
		{
			get
			{
				return new List<Skill>
				{
					new Skill
					{
						skillName = "Attack",
						description = "Apply damage to enemy unit.",
						message1 = "attacked!",
						note = "ATTACK ANIMATION: target, (mirror)",
					},
					new Skill
					{
						skillName = "Defend",
						description = "Take a defensive stance until the next turn.",
						scope = SkillScope.User,
						damageType = SkillDamageType.None,
						formula = string.Empty,
						speed = 2000,
						message1 = "defends!",
					},
					new Skill
					{
						skillName = "Fire Breath",
						description = "Burns all enemies with a gout of flame.",
						skillType = "Special",
						tpCost = 20,
						scope = SkillScope.AllEnemies,
						hitType = SkillHitType.MagicalAttack,
						animation = "Fire",
						element = "Fire",
						formula = "a.mat * 3 - b.mdf * 2",
						message1 = "breathes fire!",
					},
					new Skill
					{
						skillName = "Heal",
						description = "Restores some of an ally's HP.",
						skillType = "Magic",
						mpCost = 5,
						scope = SkillScope.OneAlly,
						occasion = SkillOccasion.Always,
						hitType = SkillHitType.CertainHit,
						animation = "Heal",
						damageType = SkillDamageType.HpRecover,
						element = "Holy",
						formula = "a.mat * 2 + 50",
						criticalHits = false,
						message1 = "casts Heal!",
					},
				};
			}
		}
	}
}

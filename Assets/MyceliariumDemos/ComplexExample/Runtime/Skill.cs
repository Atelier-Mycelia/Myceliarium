using System.Collections.Generic;
using UnityEngine;

namespace ComplexExample
{
	public enum SkillScope
	{
		None,
		[InspectorName("1 Enemy")] OneEnemy,
		[InspectorName("All Enemies")] AllEnemies,
		[InspectorName("1 Ally")] OneAlly,
		[InspectorName("All Allies")] AllAllies,
		User,
	}

	public enum SkillOccasion
	{
		Always,
		[InspectorName("Battle Screen")] BattleScreen,
		[InspectorName("Menu Screen")] MenuScreen,
		Never,
	}

	public enum SkillHitType
	{
		[InspectorName("Certain Hit")] CertainHit,
		[InspectorName("Physical Attack")] PhysicalAttack,
		[InspectorName("Magical Attack")] MagicalAttack,
	}

	public enum SkillDamageType
	{
		None,
		[InspectorName("HP Damage")] HpDamage,
		[InspectorName("MP Damage")] MpDamage,
		[InspectorName("HP Recover")] HpRecover,
		[InspectorName("MP Recover")] MpRecover,
		[InspectorName("HP Drain")] HpDrain,
		[InspectorName("MP Drain")] MpDrain,
	}

	/// <summary>
	/// A stripped-down take on the skills of an RPG Maker-style database. It's all
	/// metadata; nothing here does anything in battle. The fields are public because
	/// this is a plain data holder, and Unity serializes them as they are.
	/// </summary>
	[System.Serializable]
	public class Skill
	{
		#region General Settings
		public string skillName = string.Empty;
		public Sprite icon;
		[TextArea] public string description = string.Empty;
		public string skillType = DefaultSkillType;
		public int mpCost = 0;
		public int tpCost = 0;
		public SkillScope scope = SkillScope.OneEnemy;
		public SkillOccasion occasion = SkillOccasion.BattleScreen;
		#endregion

		#region Invocation
		public int speed = 0;
		public int successRate = 100; // As a percentage
		public int repeats = 1;
		public int tpGain = 0;
		public SkillHitType hitType = SkillHitType.PhysicalAttack;
		public string animation = DefaultAnimation;
		#endregion

		#region Message
		public string message1 = string.Empty;
		public string message2 = string.Empty;
		#endregion

		#region Damage
		public SkillDamageType damageType = SkillDamageType.HpDamage;
		public string element = DefaultElement;
		public string formula = "a.atk * 4 - b.def * 2";
		public int variance = 20; // As a percentage
		public bool criticalHits = true;
		#endregion

		#region Required Weapon
		public string weaponType1 = DefaultWeaponType;
		public string weaponType2 = DefaultWeaponType;
		#endregion

		[TextArea] public string note = string.Empty;

		/// <summary>
		/// What the dropdowns offer. The first of each list is the default.
		/// </summary>
		public static readonly IReadOnlyList<string> SkillTypes = new[]
		{
			"None", "Magic", "Special",
		};

		public static readonly IReadOnlyList<string> Animations = new[]
		{
			"None", "Normal Attack", "Fire", "Ice", "Lightning", "Heal",
		};

		public static readonly IReadOnlyList<string> Elements = new[]
		{
			"Normal Attack", "Fire", "Ice", "Thunder", "Water", "Earth", "Wind", "Holy",
			"Darkness",
		};

		public static readonly IReadOnlyList<string> WeaponTypes = new[]
		{
			"None", "Dagger", "Sword", "Axe", "Spear", "Bow", "Staff",
		};

		public static string DefaultSkillType => SkillTypes[0];
		public static string DefaultAnimation => Animations[1];
		public static string DefaultElement => Elements[0];
		public static string DefaultWeaponType => WeaponTypes[0];

		public Skill()
		{
		}

		public Skill(Skill other)
		{
			SetFrom(other);
		}

		/// <summary>
		/// Sets every field to what it is in the other skill. This is a shallow copy,
		/// which is enough since the fields are values, strings or asset references.
		/// </summary>
		public void SetFrom(Skill other)
		{
			skillName = other.skillName;
			icon = other.icon;
			description = other.description;
			skillType = other.skillType;
			mpCost = other.mpCost;
			tpCost = other.tpCost;
			scope = other.scope;
			occasion = other.occasion;

			speed = other.speed;
			successRate = other.successRate;
			repeats = other.repeats;
			tpGain = other.tpGain;
			hitType = other.hitType;
			animation = other.animation;

			message1 = other.message1;
			message2 = other.message2;

			damageType = other.damageType;
			element = other.element;
			formula = other.formula;
			variance = other.variance;
			criticalHits = other.criticalHits;

			weaponType1 = other.weaponType1;
			weaponType2 = other.weaponType2;

			note = other.note;
		}
	}
}

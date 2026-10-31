using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComplexExample
{
	/// <summary>
	/// Ties the names of the fields in SkillView.uxml to the values of a Skill, so the
	/// view (showing and reading values) and the logic (applying edits, undos and redos)
	/// agree on what each field means without each keeping a switch of its own.
	/// Values go through as plain objects, of the type the field itself uses.
	/// </summary>
	public static class SkillFields
	{
		public static IEnumerable<string> Names => _accessors.Keys;

		public static bool Has(string fieldName)
		{
			return fieldName != null && _accessors.ContainsKey(fieldName);
		}

		public static object Get(Skill skill, string fieldName)
		{
			return _accessors[fieldName].Get(skill);
		}

		/// <summary>
		/// Puts the value into the skill's field that goes with the name. Returns false
		/// (logging a warning) if no skill field goes with it.
		/// </summary>
		public static bool TrySet(Skill skill, string fieldName, object value)
		{
			if (!Has(fieldName))
			{
				Debug.LogWarning($"SkillFields: No skill field is mapped to '{fieldName}'.");
				return false;
			}

			_accessors[fieldName].Set(skill, value);
			return true;
		}

		private readonly struct Accessor
		{
			public Accessor(Func<Skill, object> get, Action<Skill, object> set)
			{
				Get = get;
				Set = set;
			}

			public Func<Skill, object> Get { get; }
			public Action<Skill, object> Set { get; }
		}

		private static readonly Dictionary<string, Accessor> _accessors =
			new Dictionary<string, Accessor>
		{
			["SkillName"] = new Accessor(s => s.skillName, (s, v) => s.skillName = (string)v),
			["IconPicker"] = new Accessor(s => s.icon, (s, v) => s.icon = v as Sprite),
			["Description"] = new Accessor(s => s.description, (s, v) => s.description = (string)v),
			["SkillType"] = new Accessor(s => s.skillType, (s, v) => s.skillType = (string)v),
			["MpCost"] = new Accessor(s => s.mpCost, (s, v) => s.mpCost = (int)v),
			["TpCost"] = new Accessor(s => s.tpCost, (s, v) => s.tpCost = (int)v),
			["Scope"] = new Accessor(s => s.scope, (s, v) => s.scope = (SkillScope)v),
			["Occasion"] = new Accessor(s => s.occasion, (s, v) => s.occasion = (SkillOccasion)v),

			["Speed"] = new Accessor(s => s.speed, (s, v) => s.speed = (int)v),
			["SuccessRate"] = new Accessor(s => s.successRate, (s, v) => s.successRate = (int)v),
			["Repeats"] = new Accessor(s => s.repeats, (s, v) => s.repeats = (int)v),
			["TpGain"] = new Accessor(s => s.tpGain, (s, v) => s.tpGain = (int)v),
			["HitType"] = new Accessor(s => s.hitType, (s, v) => s.hitType = (SkillHitType)v),
			["Animation"] = new Accessor(s => s.animation, (s, v) => s.animation = (string)v),

			["Message1"] = new Accessor(s => s.message1, (s, v) => s.message1 = (string)v),
			["Message2"] = new Accessor(s => s.message2, (s, v) => s.message2 = (string)v),

			["DamageType"] = new Accessor(s => s.damageType, (s, v) => s.damageType = (SkillDamageType)v),
			["Element"] = new Accessor(s => s.element, (s, v) => s.element = (string)v),
			["Formula"] = new Accessor(s => s.formula, (s, v) => s.formula = (string)v),
			["Variance"] = new Accessor(s => s.variance, (s, v) => s.variance = (int)v),
			["CriticalHits"] = new Accessor(s => s.criticalHits, (s, v) => s.criticalHits = (bool)v),

			["WeaponType1"] = new Accessor(s => s.weaponType1, (s, v) => s.weaponType1 = (string)v),
			["WeaponType2"] = new Accessor(s => s.weaponType2, (s, v) => s.weaponType2 = (string)v),

			["Note"] = new Accessor(s => s.note, (s, v) => s.note = (string)v),
		};
	}
}

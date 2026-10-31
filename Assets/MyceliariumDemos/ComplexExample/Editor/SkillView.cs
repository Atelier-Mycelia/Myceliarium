using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ComplexExample
{
	/// <summary>
	/// The detail view for one skill: its fields, laid out in sections. Edits go into
	/// the skill as soon as the user makes them (so the skill is always current, even
	/// when another one gets picked mid-typing); making them undoable is the job of the
	/// subwindow's edit trackers and the logic, not this.
	/// </summary>
	public class SkillView : VisualElement
	{
		/// <summary>
		/// Note that this does not bind itself to any skill until one is set. Only
		/// the user's changes to the fields write to the skill; saving to disk is a
		/// separate step they ask for.
		/// </summary>
		public SkillView()
		{
			if (_vta == null)
			{
				_vta = Resources.Load<VisualTreeAsset>(_vtaPath);
			}

			_vta.CloneTree(this);
			ConfigureVisualElements();
			ApplySkillToFields();
			SetSubs(true);
		}

		private static VisualTreeAsset _vta;
		private readonly string _vtaPath = "Editor/Uxml/SkillView";

		private void ConfigureVisualElements()
		{
			var iconPicker = this.Q<ObjectField>("IconPicker");
			iconPicker.objectType = typeof(Sprite);
			_iconPreview = new VisualElement { pickingMode = PickingMode.Ignore };
			_iconPreview.AddToClassList("icon-preview");
			// At the bottom, so the picker button stays on top of it
			iconPicker.Q(className: "unity-object-field__input").Insert(0, _iconPreview);

			// Sets each enum field's type and choices
			this.Q<EnumField>("Scope").Init(SkillScope.None);
			this.Q<EnumField>("Occasion").Init(SkillOccasion.Always);
			this.Q<EnumField>("HitType").Init(SkillHitType.CertainHit);
			this.Q<EnumField>("DamageType").Init(SkillDamageType.None);

			SetChoices("SkillType", Skill.SkillTypes);
			SetChoices("Animation", Skill.Animations);
			SetChoices("Element", Skill.Elements);
			SetChoices("WeaponType1", Skill.WeaponTypes);
			SetChoices("WeaponType2", Skill.WeaponTypes);

			// So the typed values are current the moment they're typed
			foreach (var textField in this.Query<TextField>().ToList())
			{
				textField.isDelayed = false;
			}
		}

		private void SetChoices(string dropdownName, IReadOnlyList<string> choices)
		{
			this.Q<DropdownField>(dropdownName).choices = new List<string>(choices);
		}

		/// <summary>
		/// Raised when the user changes a field. Shown values the skill itself provides
		/// (when setting Skill or calling RefreshFromSkill) don't count.
		/// </summary>
		public event Action Edited = delegate { };

		public Skill Skill
		{
			get => _skill;
			set
			{
				_skill = value;
				ApplySkillToFields();
			}
		}

		private Skill _skill;

		/// <summary>
		/// Re-shows the current skill's values in the fields, for when they were changed
		/// from outside (applying an undo or redo).
		/// </summary>
		public void RefreshFromSkill()
		{
			ApplySkillToFields();
		}

		private void ApplySkillToFields()
		{
			// With no skill (the list is empty), there's nothing for the fields to edit
			SetEnabled(_skill != null);
			var toShow = _skill ?? _blankSkill;
			foreach (string fieldName in SkillFields.Names)
			{
				var field = this.Q(fieldName);
				UitkFieldValues.SetWithoutNotify(field, SkillFields.Get(toShow, fieldName));
			}
			ShowIcon(toShow.icon);
		}

		private readonly Skill _blankSkill = new Skill();

		/// <summary>
		/// The picker is an ObjectField, which only shows the sprite's name; the box shows
		/// what it looks like.
		/// </summary>
		private void ShowIcon(Sprite icon)
		{
			_iconPreview.style.backgroundImage = icon != null ?
				new StyleBackground(icon) :
				new StyleBackground(StyleKeyword.None);
		}

		private VisualElement _iconPreview;

		private void SetSubs(bool wantsSubsActive)
		{
			// ChangeEvents bubble, but are typed, so it takes one registration per type
			if (wantsSubsActive)
			{
				RegisterCallback<ChangeEvent<string>>(OnFieldChanged);
				RegisterCallback<ChangeEvent<int>>(OnFieldChanged);
				RegisterCallback<ChangeEvent<bool>>(OnFieldChanged);
				RegisterCallback<ChangeEvent<Enum>>(OnFieldChanged);
				RegisterCallback<ChangeEvent<UnityEngine.Object>>(OnFieldChanged);
			}
			else
			{
				UnregisterCallback<ChangeEvent<string>>(OnFieldChanged);
				UnregisterCallback<ChangeEvent<int>>(OnFieldChanged);
				UnregisterCallback<ChangeEvent<bool>>(OnFieldChanged);
				UnregisterCallback<ChangeEvent<Enum>>(OnFieldChanged);
				UnregisterCallback<ChangeEvent<UnityEngine.Object>>(OnFieldChanged);
			}
		}

		private void OnFieldChanged<T>(ChangeEvent<T> evt)
		{
			var field = evt.target as VisualElement;
			bool notOneOfOurFields = _skill == null ||
				field == null ||
				!SkillFields.Has(field.name);
			if (notOneOfOurFields)
			{
				return;
			}

			SkillFields.TrySet(_skill, field.name, evt.newValue);
			ShowIcon(_skill.icon);
			Edited();
		}
	}
}

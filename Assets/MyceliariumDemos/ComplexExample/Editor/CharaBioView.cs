using AtMycelia.Myceliarium;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ComplexExample
{
	public class CharaBioView : VisualElement
	{
		/// <summary>
		/// Note that this does not bind itself to the passed bio. It merely
		/// initializes its fields to match the bio's state at the time of
		/// construction. We only want the bio to be saved to disk in response
		/// to the user clicking the Save button.
		/// </summary>
		public CharaBioView(CharaBio initBio = null)
		{
			if (_entryVta == null)
			{
				_entryVta = Resources.Load<VisualTreeAsset>(_entryVtaPath);
			}

			initBio ??= new CharaBio();
			_baseBioSnapshot.SetFrom(initBio);
			_bio.SetFrom(initBio);

			_entryVta.CloneTree(this);
			RegisterVisualElements();
			ConfigureVisualElements();
			ApplyBioFieldsToSelf();
			SetSubs(true);
		}

		private static VisualTreeAsset _entryVta;
		private readonly string _entryVtaPath = "Editor/Uxml/CharaBioView";
		private CharaBio _baseBioSnapshot = new CharaBio(), _bio = new CharaBio();
		// ^The former is to help keep track of when the info in the view changes,
		// helping inform the hasUnsavedChanges functionality. 

		private void RegisterVisualElements()
		{
			_mugshotField = this.Q<VisualElement>("Mugshot");
			_mugshotPicker = this.Q<ObjectField>("MugshotPicker");
			_mugshotPicker.objectType = typeof(Sprite);
			_firstNameField = this.Q<TextField>("FirstName");
			_lastNameField = this.Q<TextField>("LastName");
			_nicknameField = this.Q<TextField>("Nickname");
			_ageField = this.Q<IntegerField>("Age");
			_summaryField = this.Q<TextField>("Summary");
			_sexualHistoryField = this.Q<TextField>("SexualHistory");
			_isAliveField = this.Q<Toggle>("IsAlive");
			_alignmentField = this.Q<EnumField>("Alignment");
			_occupationField = this.Q<DropdownField>("Occupation");
		}

		private VisualElement _mugshotField;
		private TextField _firstNameField, _lastNameField, _nicknameField;
		private IntegerField _ageField;
		private Toggle _isAliveField;
		private EnumField _alignmentField;
		private DropdownField _occupationField;
		private TextField _summaryField, _sexualHistoryField;
		private ObjectField _mugshotPicker;

		private void ConfigureVisualElements()
		{
			_firstNameField.isDelayed = _lastNameField.isDelayed = 
				_nicknameField.isDelayed = _summaryField.isDelayed = 
				_sexualHistoryField.isDelayed = false;
			// ^We do this to ensure that the values are current by the time FocusOut fires

			_alignmentField.Init(CharaAlignment.Neutral); // Sets the enum type and choices
			_occupationField.choices = new List<string>(CharaBio.Occupations);
		}

		private void ApplyBioFieldsToSelf()
		{
			if (_bio != null)
			{
				_firstNameField.SetValueWithoutNotify(_bio.FirstName);
				_lastNameField.SetValueWithoutNotify(_bio.LastName);
				_nicknameField.SetValueWithoutNotify(_bio.NickName);
				_ageField.SetValueWithoutNotify(_bio.Age);
				_summaryField.SetValueWithoutNotify(_bio.Summary);
				_sexualHistoryField.SetValueWithoutNotify(_bio.SexualHistory);
				_isAliveField.SetValueWithoutNotify(_bio.IsAlive);
				_alignmentField.SetValueWithoutNotify(_bio.Alignment);
				_occupationField.SetValueWithoutNotify(_bio.Occupation);
				ApplyMugshotToField(_bio.Mugshot);
				this.name = $"{_bio.FirstName}_{_bio.LastName}_CharaBioView";
			}
			else
			{
				ClearInfo();
				this.name = "CharaBioView_Empty";
			}
		}

		private void ApplyMugshotToField(Sprite sprite)
		{
			_mugshotPicker.SetValueWithoutNotify(sprite);
			_mugshotField.style.backgroundImage = sprite != null
				? new StyleBackground(sprite)
				: new StyleBackground(StyleKeyword.None);
		}

		private void ClearInfo()
		{
			_firstNameField.SetValueWithoutNotify(string.Empty);
			_lastNameField.SetValueWithoutNotify(string.Empty);
			_nicknameField.SetValueWithoutNotify(string.Empty);
			_ageField.SetValueWithoutNotify(0);
			_summaryField.SetValueWithoutNotify(string.Empty);
			_sexualHistoryField.SetValueWithoutNotify(string.Empty);
			_isAliveField.SetValueWithoutNotify(true);
			_alignmentField.SetValueWithoutNotify(CharaAlignment.Neutral);
			_occupationField.SetValueWithoutNotify(CharaBio.DefaultOccupation);
			ApplyMugshotToField(null);
		}

		private void SetSubs(bool wantsSubsActive)
		{
			if (wantsSubsActive)
			{
				ControlPanelSignals.SaveCompleted += OnSaveCompleted;
			}
			else
			{
				ControlPanelSignals.SaveCompleted -= OnSaveCompleted;
			}

			SetSubsForFocusOuts(wantsSubsActive);
			SetSubsForMugshotPicker(wantsSubsActive);
			SetSubsForChoiceFields(wantsSubsActive);
		}

		private void OnSaveCompleted(IControlPanelEntry entry)
		{
			_baseBioSnapshot.SetFrom(_bio);
		}


		#region Focus-Out Subs
		private void SetSubsForFocusOuts(bool wantsSubsActive)
		{
			// FocusOutEvent bubbles, so one callback on the root hears every field
			if (wantsSubsActive)
			{
				RegisterCallback<FocusOutEvent>(OnAnyFocusOut);
			}
			else
			{
				UnregisterCallback<FocusOutEvent>(OnAnyFocusOut);
			}
		}

		private void OnAnyFocusOut(FocusOutEvent evt)
		{
			FlushFieldsToBio();
			SignalStateChangeAsAppropriate();
		}

		private void SignalStateChangeAsAppropriate()
		{
			if (!_baseBioSnapshot.Equals(_bio))
			{
				CharaBioViewSignals.BioStateChanged(_baseBioSnapshot, _bio);
			}
		}

		#endregion

		#region Mugshot Sprite Picker
		private void SetSubsForMugshotPicker(bool wantsSubsActive)
		{
			if (wantsSubsActive)
			{
				_mugshotPicker.RegisterValueChangedCallback(OnMugshotPicked);
			}
			else
			{
				_mugshotPicker.UnregisterValueChangedCallback(OnMugshotPicked);
			}
		}

		private void OnMugshotPicked(ChangeEvent<Object> evt)
		{
			var pickedSprite = evt.newValue as Sprite;
			if (_bio != null)
			{
				_bio.Mugshot = pickedSprite;
			}
			ApplyMugshotToField(pickedSprite);
			SignalStateChangeAsAppropriate();
		}

		#endregion

		#region Toggle, Enum and Dropdown Fields
		// These have no typing session to wait out, so unlike the text fields they
		// write to the bio as soon as the user changes them. ChangeEvents bubble too, but
		// are typed, so it takes one registration per type. The string one also comes
		// from the text fields, hence the target check.
		private void SetSubsForChoiceFields(bool wantsSubsActive)
		{
			if (wantsSubsActive)
			{
				RegisterCallback<ChangeEvent<bool>>(OnChoiceChanged);
				RegisterCallback<ChangeEvent<System.Enum>>(OnChoiceChanged);
				RegisterCallback<ChangeEvent<string>>(OnOccupationChanged);
			}
			else
			{
				UnregisterCallback<ChangeEvent<bool>>(OnChoiceChanged);
				UnregisterCallback<ChangeEvent<System.Enum>>(OnChoiceChanged);
				UnregisterCallback<ChangeEvent<string>>(OnOccupationChanged);
			}
		}

		private void OnChoiceChanged<T>(ChangeEvent<T> evt)
		{
			FlushFieldsToBio();
			SignalStateChangeAsAppropriate();
		}

		private void OnOccupationChanged(ChangeEvent<string> evt)
		{
			if (evt.target == _occupationField)
			{
				OnChoiceChanged(evt);
			}
		}
		#endregion

		/// <summary>
		/// Writes what the fields hold into the current bio. Needed when the bio is
		/// about to be swapped out while a field still has focus, since its focus-out
		/// would otherwise come after the swap (leading to the typed value being lost).
		/// </summary>
		public void FlushFieldsToBio()
		{
			_bio.FirstName = _firstNameField.value;
			_bio.LastName = _lastNameField.value;
			_bio.NickName = _nicknameField.value;
			_bio.Age = _ageField.value;
			_bio.IsAlive = _isAliveField.value;
			_bio.Alignment = (CharaAlignment)_alignmentField.value;
			_bio.Occupation = _occupationField.value;
			_bio.Summary = _summaryField.value;
			_bio.SexualHistory = _sexualHistoryField.value;
		}

		/// <summary>
		/// Re-shows the current bio's values in the fields. Unlike setting Bio, this
		/// leaves the base snapshot alone.
		/// </summary>
		public void RefreshFromBio()
		{
			ApplyBioFieldsToSelf();
		}

		public CharaBio Bio
		{
			get => _bio;
			set
			{
				ClearInfo();
				_bio = value ?? new CharaBio();
				ApplyBioFieldsToSelf();
				_baseBioSnapshot.SetFrom(_bio);
			}
		}

	}
}

using AtMycelia.Myceliarium;
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
		}

		private VisualElement _mugshotField;
		private TextField _firstNameField, _lastNameField, _nicknameField;
		private IntegerField _ageField;
		private TextField _summaryField, _sexualHistoryField;
		private ObjectField _mugshotPicker;

		private void ConfigureVisualElements()
		{
			_firstNameField.isDelayed = _lastNameField.isDelayed = 
				_nicknameField.isDelayed = _summaryField.isDelayed = 
				_sexualHistoryField.isDelayed = false;
			// ^We do this to ensure that the values are current by the time FocusOut fires
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
		}

		private void OnSaveCompleted(IControlPanelEntry entry)
		{
			_baseBioSnapshot.SetFrom(_bio);
		}


		#region Focus-Out Subs
		private void SetSubsForFocusOuts(bool wantsSubsActive)
		{
			if (wantsSubsActive)
			{
				_firstNameField.RegisterCallback<FocusOutEvent>(OnFirstNameFocusOut);
				_lastNameField.RegisterCallback<FocusOutEvent>(OnLastNameFocusOut);
				_nicknameField.RegisterCallback<FocusOutEvent>(OnNicknameFocusOut);
				_ageField.RegisterCallback<FocusOutEvent>(OnAgeFocusOut);
				_summaryField.RegisterCallback<FocusOutEvent>(OnSummaryFocusOut);
				_sexualHistoryField.RegisterCallback<FocusOutEvent>(OnSexualHistoryFocusOut);
			}
			else
			{
				_firstNameField.UnregisterCallback<FocusOutEvent>(OnFirstNameFocusOut);
				_lastNameField.UnregisterCallback<FocusOutEvent>(OnLastNameFocusOut);
				_nicknameField.UnregisterCallback<FocusOutEvent>(OnNicknameFocusOut);
				_ageField.UnregisterCallback<FocusOutEvent>(OnAgeFocusOut);
				_summaryField.UnregisterCallback<FocusOutEvent>(OnSummaryFocusOut);
				_sexualHistoryField.UnregisterCallback<FocusOutEvent>(OnSexualHistoryFocusOut);
			}
		}

		private void OnFirstNameFocusOut(FocusOutEvent evt)
		{
			_bio.FirstName = _firstNameField.value;
			SignalStateChangeAsAppropriate();
		}

		private void SignalStateChangeAsAppropriate()
		{
			if (!_baseBioSnapshot.Equals(_bio))
			{
				CharaBioViewSignals.BioStateChanged(_baseBioSnapshot, _bio);
			}
		}

		private void OnLastNameFocusOut(FocusOutEvent evt)
		{
			_bio.LastName = _lastNameField.value;
			SignalStateChangeAsAppropriate();
		}

		private void OnNicknameFocusOut(FocusOutEvent evt)
		{
			_bio.NickName = _nicknameField.value;
			SignalStateChangeAsAppropriate();
		}

		private void OnAgeFocusOut(FocusOutEvent evt)
		{
			_bio.Age = _ageField.value;
			SignalStateChangeAsAppropriate();
		}

		private void OnSummaryFocusOut(FocusOutEvent evt)
		{
			_bio.Summary = _summaryField.value;
			SignalStateChangeAsAppropriate();
		}

		private void OnSexualHistoryFocusOut(FocusOutEvent evt)
		{
			_bio.SexualHistory = _sexualHistoryField.value;
			SignalStateChangeAsAppropriate();
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

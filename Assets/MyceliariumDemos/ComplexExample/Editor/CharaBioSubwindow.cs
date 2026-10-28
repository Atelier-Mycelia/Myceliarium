using AtMycelia.Myceliarium;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ComplexExample
{
	/// <summary>
	/// Handles the UI for displaying and editing character bios in the control panel.
	/// A sidebar on the left lets the user pick which bio to view/edit, and the
	/// selected bio's details are shown in a single CharaBioView on the right.
	/// </summary>
	public class CharaBioSubwindow : ControlPanelSubwindow, ISimpleExampleSubwindow
	{
		#region Configurables
		public override string PathToUxml => "Editor/Uxml/CharaBioSubwindow";
		private static readonly int _minBioCount = 10;
		private static readonly float _entryHeight = 44f; // Twice the ListView default//
		#endregion

		public override void Init()
		{
			base.Init();
			this.name = "CharaBioSubwindow";
		}

		protected override void RegisterVisualElements()
		{
			_entryListView = Root.Q<ListView>("EntryListView");
			_detailHolder = Root.Q<VisualElement>("DetailHolder");
			_addBioButton = Root.Q<Button>("NewBioButton");
			_deleteBioButton = Root.Q<Button>("DeleteBioButton");
            _detailView = new CharaBioView(null);
        }

        protected override void ConfigureVisualElements()
        {
            _entryListView.selectionType = SelectionType.Single;
            _entryListView.fixedItemHeight = _entryHeight;
            _entryListView.reorderable = true;
            _entryListView.itemsSource = (IList)_bios;

            _detailView.style.flexGrow = 1;
            _detailHolder.Add(_detailView);
        }

		private void OnEntryListSelectedIndexChanged(IEnumerable<int> enumerable)
		{
			int selectedIndex = -1;
			foreach (var index in enumerable)
			{
				selectedIndex = index;
				break;
			}

			bool validSelection = selectedIndex >= 0 && selectedIndex < _bios.Count;
			if (!validSelection) // Must've been cleared after deletion
			{
				return;	
			}

			_lastSelectedIndex = selectedIndex;
			_detailView.Bio = _bios[selectedIndex];

			if (validSelection)
			{
				CharaBioViewSignals.BioSelected(_detailView);
			}
		}

		private ListView _entryListView;
		private VisualElement _detailHolder;
		private CharaBioView _detailView;
		private Button _addBioButton, _deleteBioButton;
		public CharaBioView LastSelectedView => _detailView;
		private int _lastSelectedIndex = 0;

		protected override void SetSubs(bool wantsSubsActive)
		{
			base.SetSubs(wantsSubsActive);
			if (wantsSubsActive)
			{
				_entryListView.makeItem = EntryListMakeItem;
				_entryListView.bindItem = EntryListBindItem;
				_entryListView.selectedIndicesChanged += OnEntryListSelectedIndexChanged;
				CharaBioViewSignals.BioStateChanged += OnBioStateChanged;

				_addBioButton.clicked += OnAddBioButtonClicked;
				_deleteBioButton.clicked += OnDeleteBioButtonClicked;
			}
			else
			{
				_entryListView.makeItem = null;
				_entryListView.bindItem = null;
				_entryListView.selectedIndicesChanged -= OnEntryListSelectedIndexChanged;
				
				CharaBioViewSignals.BioStateChanged -= OnBioStateChanged;

				_addBioButton.clicked -= OnAddBioButtonClicked;
				_deleteBioButton.clicked -= OnDeleteBioButtonClicked;
			}
		}

		private void OnBioStateChanged(CharaBio prevState, CharaBio currentState)
		{
			_entryListView.RefreshItems();
		}

		private VisualElement EntryListMakeItem()
		{
			return new CharaBioListLabel();
		}

		private void EntryListBindItem(VisualElement element, int index)
		{
			if (index < 0 || index >= _bios.Count)
			{
				Debug.LogWarning($"CharaBioSubwindow: Index {index} is out " +
					$"of range for bios.");
				return;
			}

			var label = (CharaBioListLabel)element;
			var bio = _bios[index];
			label.Text = GetDisplayNameFor(bio, index);
		}

		private string GetDisplayNameFor(CharaBio bio, int index)
		{
			string fullName = $"{bio.FirstName} {bio.LastName}".Trim();
			string name = string.IsNullOrEmpty(fullName) ? 
			"(Unnamed)" : 
			fullName;
			string indexStr = $"{index + 1:D3}";
			return $"{indexStr}: {name}";
		}

		public void Remove(CharaBio toRemove)
		{
			_bios.Remove(toRemove);
		}

		public void SetBioViewList(IList<CharaBio> bios)
		{
			_entryListView.itemsSource = (IList)bios;
		}

		public void AddBio(CharaBio bio)
		{
			_bios.Add(bio);
			_entryListView.RefreshItems();
		}

		public int IndexOf(CharaBio bio)
		{
			return _bios.IndexOf(bio);
		}

		/// <summary>
		/// Removes the bio and refreshes. Since refreshing tops the list back up to the
		/// minimum count with blank bios, returns the ones it added, so that undoing the
		/// removal (RestoreBio) can take them back out.
		/// </summary>
		public List<CharaBio> RemoveBio(CharaBio bio)
		{
			_bios.Remove(bio);
			int countWithoutPadding = _bios.Count;
			Refresh();

			var padding = new List<CharaBio>();
			for (int i = countWithoutPadding; i < _bios.Count; i++)
			{
				padding.Add(_bios[i]);
			}
			return padding;
		}

		/// <summary>
		/// Undoes RemoveBio: puts the bio back at its old index (and selects it) after
		/// dropping the padding that the removal caused.
		/// </summary>
		public void RestoreBio(int index, CharaBio bio, IEnumerable<CharaBio> padding)
		{
			foreach (var blank in padding)
			{
				_bios.Remove(blank);
			}

			_bios.Insert(Mathf.Clamp(index, 0, _bios.Count), bio);
			_lastSelectedIndex = _bios.IndexOf(bio);
			Refresh();
		}

		/// <summary>
		/// Puts the given value (a string, or an int for the age) into the matching
		/// property of the bio and shows the result, selecting the bio if it isn't the
		/// one on display. Used when applying undos and redos of edits to the fields.
		/// </summary>
		public void ApplyEdit(CharaBio bio, string fieldName, object value)
		{
			switch (fieldName)
			{
				case "FirstName": bio.FirstName = (string)value; break;
				case "LastName": bio.LastName = (string)value; break;
				case "Nickname": bio.NickName = (string)value; break;
				case "Age": bio.Age = (int)value; break;
				case "Summary": bio.Summary = (string)value; break;
				case "SexualHistory": bio.SexualHistory = (string)value; break;
				default:
					Debug.LogWarning($"CharaBioSubwindow: No bio property is mapped " +
						$"to the field '{fieldName}'.");
					return;
			}

			int index = _bios.IndexOf(bio);
			if (index >= 0 && index != _lastSelectedIndex)
			{
				_lastSelectedIndex = index;
				_entryListView.SetSelection(index); // Shows the bio in the detail view
			}

			if (_detailView.Bio == bio)
			{
				_detailView.RefreshFromBio();
			}
			_entryListView.RefreshItems();
		}

		/// <summary>
		/// The bio on display is what the text fields in the detail view are editing.
		/// </summary>
		protected override object GetEditContext(VisualElement field)
		{
			return _detailView?.Bio;
		}

		private void OnDeleteBioButtonClicked()
		{
			var lastSelectedBio = _bios[_lastSelectedIndex];
			CharaBioViewSignals.BioDeletionRequested(lastSelectedBio);
		}

		private void OnAddBioButtonClicked()
		{
			CharaBioViewSignals.BioAddRequested();
		}

		public void RefreshFrom(CharaBioRegistry target)
		{
			_bios.Clear();
			_registry = target;
			if (_registry != null)
			{
				SetBiosToCopyOf(_registry.CharacterBios);//
			}
			
			Refresh();
		}

        private void AutoSelectBasedOnLastSelected()
        {
            _lastSelectedIndex = Mathf.Min(_lastSelectedIndex, _bios.Count - 1);
			_entryListView.ClearSelection();
            _entryListView.SetSelection(_lastSelectedIndex);
        }

		private void EnsureMinBioCount()
		{
			while (_bios.Count < _minBioCount)
			{
				_bios.Add(new CharaBio());
			}
		}

		private CharaBioRegistry _registry;
		private readonly IList<CharaBio> _bios = new List<CharaBio>();
		public IReadOnlyList<CharaBio> Bios => (IReadOnlyList<CharaBio>)_bios;

		public override void Refresh()
		{
			base.Refresh();
			EnsureMinBioCount();
			_entryListView.itemsSource = (IList)_bios;
			_entryListView.RefreshItems();

            AutoSelectBasedOnLastSelected();
		}

		private void SetBiosToCopyOf(IReadOnlyList<CharaBio> source)
		{
			_bios.Clear();
			foreach (var elem in source)
			{
				var bioCopy = new CharaBio(elem);
				_bios.Add(bioCopy);
			}
		}

	}

	public interface ISimpleExampleSubwindow : IControlPanelSubwindow
	{
	}

}

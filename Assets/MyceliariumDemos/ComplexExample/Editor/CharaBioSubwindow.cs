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
	/// The bios themselves belong to CharaBioLogic; this just shows what it's given.
	/// </summary>
	public class CharaBioSubwindow : ControlPanelSubwindow, ISimpleExampleSubwindow
	{
		#region Configurables
		public override string PathToUxml => "Editor/Uxml/CharaBioSubwindow";
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

		/// <summary>
		/// Shows the given bios in the list. This is the logic's own list, which the
		/// ListView needs to be a non-generic IList too (List does that), so that bios
		/// added or removed by the logic are already there when it refreshes.
		/// </summary>
		/// <param name="bioToShow">Selected afterwards, if it's in the list. Otherwise,
		/// whichever was selected before stays (as long as it's still a valid index).
		/// </param>
		public void ShowMultiBios(IReadOnlyList<CharaBio> bios, CharaBio bioToShow = null)
		{
			_bios = bios;
			int indexToShow = bioToShow != null ? IndexOf(bioToShow) : -1;
			if (indexToShow >= 0)
			{
				_lastSelectedIndex = indexToShow;
			}

			Refresh();
		}

		/// <summary>
		/// Shows the result of changes to one of the bios' values, selecting the bio if it
		/// isn't the one on display. Used when applying undos and redos of edits to the
		/// fields.
		/// </summary>
		public void ShowBio(CharaBio bio)
		{
			int index = IndexOf(bio);
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

		private int IndexOf(CharaBio bio)
		{
			for (int i = 0; i < _bios.Count; i++)
			{
				if (_bios[i] == bio)
				{
					return i;
				}
			}
			return -1;
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
			if (_lastSelectedIndex < 0 || _lastSelectedIndex >= _bios.Count)
			{
				return;
			}

			var lastSelectedBio = _bios[_lastSelectedIndex];
			CharaBioViewSignals.BioDeletionRequested(lastSelectedBio);
		}

		private void OnAddBioButtonClicked()
		{
			CharaBioViewSignals.BioAddRequested();
		}

		private void AutoSelectBasedOnLastSelected()
		{
			_lastSelectedIndex = Mathf.Min(_lastSelectedIndex, _bios.Count - 1);
			_entryListView.ClearSelection();
			if (_lastSelectedIndex >= 0)
			{
				_entryListView.SetSelection(_lastSelectedIndex);
			}
		}

		private IReadOnlyList<CharaBio> _bios = new List<CharaBio>();

		public override void Refresh()
		{
			base.Refresh();
			_entryListView.itemsSource = (IList)_bios;
			_entryListView.RefreshItems();

			AutoSelectBasedOnLastSelected();
		}

	}

	public interface ISimpleExampleSubwindow : IControlPanelSubwindow
	{
	}

}

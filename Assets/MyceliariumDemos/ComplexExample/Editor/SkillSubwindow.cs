using AtMycelia.Myceliarium;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ComplexExample
{
	/// <summary>
	/// Handles the UI for displaying and editing skills in the control panel. A list
	/// on the left lets the user pick which skill to view/edit, and the selected one's
	/// details are shown in a single SkillView on the right. The skills themselves
	/// belong to SkillLogic; this just shows what it's given, and says when the user
	/// asks for one to be added or deleted.
	/// </summary>
	public class SkillSubwindow : ControlPanelSubwindow
	{
		#region Configurables
		public override string PathToUxml => "Editor/Uxml/SkillSubwindow";
		private static readonly float _listItemHeight = 28f;
		#endregion

		public event Action AddRequested = delegate { };
		public event Action<Skill> DeleteRequested = delegate { };

		public override void Init()
		{
			base.Init();
			this.name = "SkillSubwindow";
		}

		protected override void RegisterVisualElements()
		{
			_listView = Root.Q<ListView>("SkillListView");
			_detailHolder = Root.Q<VisualElement>("DetailHolder");
			_addButton = Root.Q<Button>("NewSkillButton");
			_deleteButton = Root.Q<Button>("DeleteSkillButton");
			_detailView = new SkillView();
		}

		protected override void ConfigureVisualElements()
		{
			_listView.selectionType = SelectionType.Single;
			_listView.fixedItemHeight = _listItemHeight;
			_listView.reorderable = false; // Order is by id, as in RPG Maker
			_listView.itemsSource = (IList)_skills;

			_detailView.style.flexGrow = 1;
			_detailHolder.Add(_detailView);
		}

		private ListView _listView;
		private VisualElement _detailHolder;
		private SkillView _detailView;
		private Button _addButton, _deleteButton;

		protected override void SetSubs(bool wantsSubsActive)
		{
			base.SetSubs(wantsSubsActive);
			if (wantsSubsActive)
			{
				_listView.makeItem = ListMakeItem;
				_listView.bindItem = ListBindItem;
				_listView.selectedIndicesChanged += OnListSelectionChanged;
				_detailView.Edited += OnDetailEdited;
				_addButton.clicked += OnAddButtonClicked;
				_deleteButton.clicked += OnDeleteButtonClicked;
			}
			else
			{
				_listView.makeItem = null;
				_listView.bindItem = null;
				_listView.selectedIndicesChanged -= OnListSelectionChanged;
				_detailView.Edited -= OnDetailEdited;
				_addButton.clicked -= OnAddButtonClicked;
				_deleteButton.clicked -= OnDeleteButtonClicked;
			}
		}

		private VisualElement ListMakeItem()
		{
			return new CharaBioListLabel();
		}

		private void ListBindItem(VisualElement element, int index)
		{
			if (index < 0 || index >= _skills.Count)
			{
				return;
			}

			((CharaBioListLabel)element).Text = GetDisplayNameFor(_skills[index], index);
		}

		/// <summary>
		/// Numbered the way RPG Maker's database lists them: "0001: Attack".
		/// </summary>
		private static string GetDisplayNameFor(Skill skill, int index)
		{
			string name = string.IsNullOrWhiteSpace(skill.skillName) ?
				"(Unnamed)" :
				skill.skillName;
			return $"{index + 1:D4}: {name}";
		}

		private void OnListSelectionChanged(IEnumerable<int> selectedIndices)
		{
			int selectedIndex = -1;
			foreach (var index in selectedIndices)
			{
				selectedIndex = index;
				break;
			}

			bool validSelection = selectedIndex >= 0 && selectedIndex < _skills.Count;
			if (!validSelection) // Must've been cleared by a refresh
			{
				return;
			}

			_lastSelectedIndex = selectedIndex;

			// A field may still have focus, so its edit isn't finished yet. Settling it
			// puts it on the history (with the skill it was made to) before the view
			// switches to another one.
			CommitPendingEdits();
			_detailView.Skill = _skills[selectedIndex];
		}

		private int _lastSelectedIndex = 0;

		private void OnDetailEdited()
		{
			_listView.RefreshItems(); // The name may have changed
		}

		private void OnAddButtonClicked()
		{
			AddRequested();
		}

		private void OnDeleteButtonClicked()
		{
			bool nothingSelected = _lastSelectedIndex < 0 || _lastSelectedIndex >= _skills.Count;
			if (nothingSelected)
			{
				return;
			}

			DeleteRequested(_skills[_lastSelectedIndex]);
		}

		/// <summary>
		/// Shows the given skills in the list. This is the logic's own list, which the
		/// ListView needs to be a non-generic IList too (List does that), so that skills
		/// added or removed by the logic are already there when it refreshes.
		/// </summary>
		/// <param name="skillToShow">Selected afterwards, if it's in the list. Otherwise,
		/// whichever was selected before stays (as long as it's still a valid index).
		/// </param>
		public void ShowSkills(IReadOnlyList<Skill> skills, Skill skillToShow = null)
		{
			_skills = skills;
			int indexToShow = skillToShow != null ?
				IndexOf(skillToShow) :
				-1;
			if (indexToShow >= 0)
			{
				_lastSelectedIndex = indexToShow;
			}

			Refresh();
		}

		private IReadOnlyList<Skill> _skills = new List<Skill>();

		/// <summary>
		/// Shows the result of changes to one of the skills' values, selecting the skill
		/// if it isn't the one on display. Used when applying undos and redos of edits
		/// to the fields.
		/// </summary>
		public void ShowSkill(Skill skill)
		{
			int index = IndexOf(skill);
			if (index >= 0 && index != _lastSelectedIndex)
			{
				_lastSelectedIndex = index;
				_listView.SetSelection(index); // Shows the skill in the detail view
			}

			if (_detailView.Skill == skill)
			{
				_detailView.RefreshFromSkill();
			}
			_listView.RefreshItems();
		}

		private int IndexOf(Skill skill)
		{
			for (int i = 0; i < _skills.Count; i++)
			{
				if (_skills[i] == skill)
				{
					return i;
				}
			}
			return -1;
		}

		/// <summary>
		/// The skill on display is what the fields in the detail view are editing.
		/// </summary>
		protected override object GetEditContext(VisualElement field)
		{
			return _detailView?.Skill;
		}

		public override void Refresh()
		{
			base.Refresh();
			_listView.itemsSource = (IList)_skills;
			_listView.RefreshItems();

			_lastSelectedIndex = Mathf.Min(_lastSelectedIndex, _skills.Count - 1);
			_listView.ClearSelection();
			if (_lastSelectedIndex >= 0)
			{
				_listView.SetSelection(_lastSelectedIndex);
			}
			else
			{
				_detailView.Skill = null; // Nothing left to show
			}
		}
	}
}

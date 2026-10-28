using System.Collections.Generic;
using AtMycelia.Myceliarium;
using UnityObj = UnityEngine.Object;

namespace ComplexExample
{
	public class CharaBioEntry : ControlPanelEntry, ICustomExampleEntry
	{
		public override string SortingName => "Chara Bios";
		public override bool IsTopLevel => true;

		protected override void PrepareLoader()
		{
			Loader = new CharaBioLoader();
		}

		protected override void PrepareLeftSidebarTab()
		{
			Tab = new CharaBioTab();
			Tab.Init();
		}

		protected override void PrepareSubwindow()
		{
			_subwindow = new CharaBioSubwindow();
			_subwindow.Init();
		}

		protected override void SetSubs(bool wantsSubsActive)
		{
			base.SetSubs(wantsSubsActive);
			if (wantsSubsActive)
			{
				CharaBioViewSignals.BioAddRequested += OnBioAddRequested;
				CharaBioViewSignals.BioDeletionRequested += OnDeleteBioRequested;
				CharaBioViewSignals.BioStateChanged += OnBioStateChanged;
			}
			else
			{
				CharaBioViewSignals.BioAddRequested -= OnBioAddRequested;
				CharaBioViewSignals.BioDeletionRequested -= OnDeleteBioRequested;
				CharaBioViewSignals.BioStateChanged -= OnBioStateChanged;
			}
		}

		private void OnBioAddRequested()
		{
			var newBio = new CharaBio(); // Kept, so redoing brings back this same one
			var command = new DelegateCommand(RegisterBio, UndoBioRegistration);
			ExecuteUndoable(command);

			void RegisterBio()
			{
				BioSubwindow.AddBio(newBio);
				HasUnsavedChanges = true;
			}

			void UndoBioRegistration()
			{
				BioSubwindow.Remove(newBio);
				BioSubwindow.Refresh();
				HasUnsavedChanges = true;
			}
		}

		private CharaBioSubwindow BioSubwindow => _subwindow as CharaBioSubwindow;

		private void OnBioStateChanged(CharaBio baseState, CharaBio currentState)
		{
			if (!baseState.Equals(currentState))
			{
				HasUnsavedChanges = true;
			}

			// We won't set it to false here; it's possible that another bio in the
			// temp registry is still different from the last saved one.
		}

		protected override void OnControlPanelOpened(IControlPanel panel)
		{
			base.OnControlPanelOpened(panel);
			HandleLoading();
		}

		private void OnDeleteBioRequested(CharaBio bio)
		{
			int index = BioSubwindow.IndexOf(bio);
			if (index < 0)
			{
				return;
			}

			List<CharaBio> padding = null; // Blanks added to keep the minimum count
			void DeleteBio()
			{
				padding = BioSubwindow.RemoveBio(bio);
				HasUnsavedChanges = true;
			}

			void UndoBioDeletion()
			{
				BioSubwindow.RestoreBio(index, bio, padding);
				HasUnsavedChanges = true;
			}
			var command = new DelegateCommand(DeleteBio, UndoBioDeletion);
			ExecuteUndoable(command);
		}

		/// <summary>
		/// The user finished typing in one of the detail view's text fields. The
		/// view has already put the text into the bio, so this just records how to
		/// switch between the old and new text.
		/// </summary>
		protected override void OnSubwindowEdit(FieldEdit edit)
		{
			if (!(edit.Context is CharaBio))
			{
				return;
			}

			var command = edit.ToCommand(ApplyTextEdit);
			RecordUndoable(command);
		}

		private void ApplyTextEdit(FieldEdit edit, object value)
		{
			var bio = (CharaBio)edit.Context;
			BioSubwindow.ApplyTextEdit(bio, edit.Field.name, (string)value);
			HasUnsavedChanges = true;
		}

		protected override void OnLoadingDone()
		{
			var bioLoader = Loader as CharaBioLoader;
			BioRegistry = bioLoader.GetLoadResult<CharaBioRegistry>();
			SetCurrentStateAsBase();
			Subwindow.RefreshFrom(BioRegistry);
			Subwindow.Show();
		}

		/// <summary>
		/// Reference to the registry on disk.
		/// </summary>
		public CharaBioRegistry BioRegistry { get; private set; }

		protected override void SetCurrentStateAsBase()
		{
			if (BioRegistry == null) // Suggests that loading hasn't happened yet,
									 // and thus we'll want to skip this until then.
			{
				return;
			}
			if (_initState != null)
			{
				UnityObj.DestroyImmediate(_initState);
			}

			_initState = UnityObj.Instantiate(BioRegistry);
			base.SetCurrentStateAsBase();
		}

		private CharaBioRegistry _initState;

		private new CharaBioSubwindow Subwindow => _subwindow as CharaBioSubwindow;

		public IReadOnlyList<CharaBio> CharaBiosInUi => Subwindow.Bios;

	}

	public interface ICustomExampleEntry : IControlPanelEntry
	{

	}
}
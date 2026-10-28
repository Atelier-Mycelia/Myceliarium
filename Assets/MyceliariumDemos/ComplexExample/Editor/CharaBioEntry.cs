using System.Collections.Generic;
using AtMycelia.Myceliarium;

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

		protected override void PrepareLogic()
		{
			Logic = new CharaBioLogic();
		}

		private CharaBioLogic BioLogic => Logic as CharaBioLogic;
		
		protected override void SetSubs(bool wantsSubsActive)
		{
			base.SetSubs(wantsSubsActive);
			if (wantsSubsActive)
			{
				CharaBioViewSignals.BioAddRequested += OnBioAddRequested;
				CharaBioViewSignals.BioDeletionRequested += OnDeleteBioRequested;
				if (BioLogic != null)
				{
					BioLogic.MultiBiosChanged += OnMultiBiosChanged;
					BioLogic.BioChanged += OnBioChanged;
				}
			}
			else
			{
				CharaBioViewSignals.BioAddRequested -= OnBioAddRequested;
				CharaBioViewSignals.BioDeletionRequested -= OnDeleteBioRequested;
				if (BioLogic != null)
				{
					BioLogic.MultiBiosChanged -= OnMultiBiosChanged;
					BioLogic.BioChanged -= OnBioChanged;
				}
			}
		}

		// The signals are the UI asking for something; the logic does it and says what
		// changed, which gets shown through the events below.
		private void OnBioAddRequested()
		{
			BioLogic.AddBio();
		}

		private void OnDeleteBioRequested(CharaBio bio)
		{
			BioLogic.DeleteBio(bio);
		}

		private void OnMultiBiosChanged(CharaBio bioToShow)
		{
			BioSubwindow.ShowMultiBios(BioLogic.Bios, bioToShow);
		}

		private CharaBioSubwindow BioSubwindow => _subwindow as CharaBioSubwindow;

		private void OnBioChanged(CharaBio bio)
		{
			BioSubwindow.ShowBio(bio);
		}

		protected override void OnControlPanelOpened(IControlPanel panel)
		{
			base.OnControlPanelOpened(panel);
			if (!BioLogic.HasUnsavedChanges) // Otherwise loading would discard them
			{
				HandleLoading();
			}
		}

		protected override void OnLoadingDone()
		{
			var loadResult = Loader.GetLoadResult<CharaBioRegistry>();
			BioLogic.Load(loadResult);
			BioSubwindow.ShowMultiBios(BioLogic.Bios);
			BioSubwindow.Show();
		}

		/// <summary>
		/// Reference to the registry on disk.
		/// </summary>
		public CharaBioRegistry BioRegistry => BioLogic?.Registry;

		public IReadOnlyList<CharaBio> CharaBiosInUi => BioLogic.Bios;
	}

	public interface ICustomExampleEntry : IControlPanelEntry
	{

	}
}

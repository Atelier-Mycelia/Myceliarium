using System.Collections.Generic;
using AtMycelia.Myceliarium;
using UnityObj = UnityEngine.Object;

namespace ComplexExample
{
	public class CharaBioEntry : ControlPanelEntry, ICustomExampleEntry
	{
		public override string SortingName => "Chara Bios";
		public override bool IsTopLevel => true;

		private CharaBioSubwindow BioSubwindow => _subwindow as CharaBioSubwindow;

		/// <summary>
		/// Reference to the registry on disk.
		/// </summary>
		public CharaBioRegistry BioRegistry { get; private set; }

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
			// This is a solution to a challenge. Best omit this code in the tutorial doc.
			base.OnControlPanelOpened(panel);
			HandleLoading();
		}

		private void OnDeleteBioRequested(CharaBio bio)
		{
			BioSubwindow.Remove(bio);
			BioSubwindow.Refresh();
			HasUnsavedChanges = true;
		}

		private void OnBioAddRequested()
		{
			BioSubwindow.AddNewBioView();
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
using System;

namespace ComplexExample
{
	public static class CharaBioViewSignals
	{
		public static Action<CharaBio> BioDeletionRequested = delegate { };
		public static Action BioAddRequested = delegate { };
		public static Action<CharaBioView> BioSelected = delegate { };

		/// <summary>
		/// The first arg is the base state (as loaded from the registry or last saved to it),
		/// while the second is the current state.
		/// </summary>
		public static Action<CharaBio, CharaBio> BioStateChanged = delegate { };
	}
}
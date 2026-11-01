using System.Collections;
using System.Collections.Generic;
using AtMycelia.Myceliarium;
using System;
using AtMycelia;

namespace ComplexExample
{
	public class CharaBioSaver : ControlPanelEntrySaver, ICustomExampleSaver
	{
		public override bool IsCompatibleWith(IControlPanelEntry toSaveFor)
		{
			return toSaveFor is CharaBioEntry;
		}

		protected override IEnumerator SaveProcess(EntrySaveRequest request)
		{
			var entry = request.Entry as CharaBioEntry;
			OverwriteRegistryOnDisk(entry);
			yield break;
		}

		private void OverwriteRegistryOnDisk(CharaBioEntry entry)
		{
			CharaBioRegistry registry = entry.BioRegistry;
			registry.Clear();

			var biosInUi = entry.CharaBiosInUi;
			var bioCopies = new List<CharaBio>();//
			
			for (int i = 0; i < biosInUi.Count; i++)
			{
				var sourceBio = biosInUi[i];
				var bioCopy = new CharaBio(sourceBio);
				bioCopies.Add(bioCopy);
			}
			// ^Why copies? So that editing a bio in the UI after saving doesn't immediately change the
			// on-disk registry, which would be confusing.
			registry.AddRange(bioCopies);
			registry.MarkDirtyAndSave();
		}

	}
}
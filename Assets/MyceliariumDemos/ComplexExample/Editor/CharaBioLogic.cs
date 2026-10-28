using System;
using System.Collections.Generic;
using AtMycelia.Myceliarium;
using UnityEngine;

namespace ComplexExample
{
	/// <summary>
	/// Owns the bios the user is working on (copies of what's in the registry, so
	/// editing doesn't touch the saved data until they save), along with the undo history
	/// and unsaved changes tracking that go with them. Knows nothing about the UI: the
	/// entry listens to the events and tells the subwindow what to show.
	/// </summary>
	public class CharaBioLogic : ControlPanelEntryLogic
	{
		public const int MinBioCount = 10;

		/// <summary>
		/// The registry on disk that the bios were loaded from (and get saved to).
		/// </summary>
		public CharaBioRegistry Registry { get; private set; }

		public IReadOnlyList<CharaBio> Bios => _bios;
		private readonly List<CharaBio> _bios = new List<CharaBio>();

		/// <summary>
		/// Raised when bios were added or removed. The argument is the bio that's worth
		/// showing afterwards, or null if there isn't one in particular.
		/// </summary>
		public event Action<CharaBio> MultiBiosChanged = delegate { };

		/// <summary>
		/// Raised when an edit command changed the values of the bio (by applying it,
		/// undoing it or redoing it).
		/// </summary>
		public event Action<CharaBio> BioChanged = delegate { };

		protected override void AdoptState(object loadResult)
		{
			Registry = loadResult as CharaBioRegistry;
			_bios.Clear();
			if (Registry != null)
			{
				foreach (var bio in Registry.CharacterBios)
				{
					_bios.Add(new CharaBio(bio));
				}
			}
			PadToMinimum();
		}

		/// <summary>
		/// Tops the list up with blank bios until it has the minimum count.
		/// </summary>
		/// <returns>The blanks that had to be added.</returns>
		private List<CharaBio> PadToMinimum()
		{
			var padding = new List<CharaBio>();
			while (_bios.Count < MinBioCount)
			{
				var blank = new CharaBio();
				_bios.Add(blank);
				padding.Add(blank);
			}
			return padding;
		}

		public void AddBio()
		{
			var newBio = new CharaBio(); // Kept, so redoing brings back this same one
			History.Push(new DelegateCommand(
				execute: () =>
				{
					_bios.Add(newBio);
					MultiBiosChanged(null);
				},
				undo: () =>
				{
					_bios.Remove(newBio);
					PadToMinimum();
					MultiBiosChanged(null);
				}));
		}

		public void DeleteBio(CharaBio bio)
		{
			int index = _bios.IndexOf(bio);
			if (index < 0)
			{
				return;
			}

			List<CharaBio> padding = null; // Blanks added to keep the minimum count
			History.Push(new DelegateCommand(
				execute: () =>
				{
					_bios.Remove(bio);
					padding = PadToMinimum();
					MultiBiosChanged(null);
				},
				undo: () =>
				{
					foreach (var blank in padding)
					{
						_bios.Remove(blank);
					}
					_bios.Insert(Mathf.Clamp(index, 0, _bios.Count), bio);
					MultiBiosChanged(bio);
				}));
		}

		/// <summary>
		/// Handles edits to the detail view's fields. The view has already put the value
		/// into the bio, so the command is only recorded, not executed.
		/// </summary>
		public override IUndoableCommand CreateEditCommand(FieldEdit edit)
		{
			if (!(edit.Context is CharaBio))
			{
				return null;
			}

			return edit.ToCommand(ApplyEdit);
		}

		private void ApplyEdit(FieldEdit edit, object value)
		{
			var bio = (CharaBio)edit.Context;
			if (TrySetField(bio, edit.Field.name, value))
			{
				BioChanged(bio);
			}
		}

		/// <summary>
		/// Puts the value into the property of the bio that goes with the field. The
		/// names are those of the fields in CharaBioView.uxml.
		/// </summary>
		private static bool TrySetField(CharaBio bio, string fieldName, object value)
		{
			switch (fieldName)
			{
				case "FirstName": bio.FirstName = (string)value; break;
				case "LastName": bio.LastName = (string)value; break;
				case "Nickname": bio.NickName = (string)value; break;
				case "Age": bio.Age = (int)value; break;
				case "MugshotPicker": bio.Mugshot = value as Sprite; break;
				case "IsAlive": bio.IsAlive = (bool)value; break;
				case "Alignment": bio.Alignment = (CharaAlignment)value; break;
				case "Occupation": bio.Occupation = (string)value; break;
				case "Summary": bio.Summary = (string)value; break;
				case "SexualHistory": bio.SexualHistory = (string)value; break;
				default:
					Debug.LogWarning($"CharaBioLogic: No bio property is mapped " +
						$"to the field '{fieldName}'.");
					return false;
			}
			return true;
		}
	}
}

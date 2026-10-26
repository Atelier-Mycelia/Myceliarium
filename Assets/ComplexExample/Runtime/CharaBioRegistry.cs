using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComplexExample
{
	public sealed class CharaBioRegistry : ScriptableObject, IEquatable<CharaBioRegistry>
	{
		[SerializeField] private List<CharaBio> _characterBios;
		public IReadOnlyList<CharaBio> CharacterBios => _characterBios;

		public void Add(CharaBio bio)
		{
			if (!_characterBios.Contains(bio))
			{
				_characterBios.Add(bio);
			}
		}

		public void Remove(CharaBio bio)
		{
			if (_characterBios.Contains(bio))
			{
				_characterBios.Remove(bio);
			}
		}

		public void Clear()
		{
			_characterBios.Clear();
		}

		public void AddRange(IList<CharaBio> bios)
		{
			foreach (var bio in bios)
			{
				Add(bio);
			}
		}

		/// <summary>
		/// Decided based on whether this and the passed registry have the same items
		/// (as dictated by CharaBio.Equals) in the same order.
		/// </summary>
		public bool Equals(CharaBioRegistry other)
		{
			bool diffBioCount = _characterBios.Count != other._characterBios.Count;
			if (other == null || diffBioCount)
			{
				return false;
			}

			for (int i = 0; i < _characterBios.Count; i++)
			{
				var ourBio = _characterBios[i];
				var theirBio = other._characterBios[i];
				if (!ourBio.Equals(theirBio))
				{
					return false;
				}
			}

			return true;
		}
	}
}
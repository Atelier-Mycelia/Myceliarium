using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComplexExample
{
	public enum CharaAlignment
	{
		Lawful,
		Neutral,
		Chaotic,
	}

	/// <summary>
	/// Holds information for a character's biography, including name, age,
	/// and other relevant details.
	/// </summary>
	[System.Serializable]
	public class CharaBio : IEquatable<CharaBio>
	{
		[SerializeField] protected Sprite _mugshot;
		[SerializeField] protected string _firstName = string.Empty;
		[SerializeField] protected string _lastName = string.Empty;
		[SerializeField] protected string _nickName = string.Empty;
		[SerializeField] protected int _age = 0;
		[Tooltip("A brief summary of the character's background or personality.")]
		[SerializeField] protected string _summary = string.Empty;
		[SerializeField] protected string _sexualHistory = string.Empty; // Because why not?
		[SerializeField] protected bool _isAlive = true;
		[SerializeField] protected CharaAlignment _alignment = CharaAlignment.Neutral;
		[SerializeField] protected string _occupation = DefaultOccupation;

		/// <summary>
		/// What the occupation dropdown offers. The first one is the default.
		/// </summary>
		public static readonly IReadOnlyList<string> Occupations = new[]
		{
			"Unemployed", "Adventurer", "Merchant", "Scholar", "Soldier", "Thief", "Healer",
		};
		public static string DefaultOccupation => Occupations[0];

		public virtual bool IsAlive
		{
			get { return _isAlive; }
			set { _isAlive = value; }
		}

		public virtual CharaAlignment Alignment
		{
			get { return _alignment; }
			set { _alignment = value; }
		}

		public virtual string Occupation
		{
			get { return _occupation; }
			set { _occupation = value; }
		}

		public virtual Sprite Mugshot         
		{
			get { return _mugshot; }
			set { _mugshot = value; }
		}

		public virtual string FirstName
		{
			get { return _firstName; }
			set { _firstName = value; }
		}

		public virtual string LastName
		{
			get { return _lastName; }
			set { _lastName = value; }
		}

		public virtual string NickName
		{
			get { return _nickName; }
			set { _nickName = value; }
		}

		public virtual int Age
		{
			get { return _age; }
			set { _age = value; }
		}

		public virtual string Summary
		{
			get { return _summary; }
			set { _summary = value; }
		}

		public virtual string SexualHistory
		{
			get { return _sexualHistory; }
			set { _sexualHistory = value; }
		}

		public CharaBio()
		{
			_mugshot = null;
			_firstName = string.Empty;
			_lastName = string.Empty;
			_nickName = string.Empty;
			_age = 0;
			_summary = string.Empty;
			_sexualHistory = string.Empty;
			_isAlive = true;
			_alignment = CharaAlignment.Neutral;
			_occupation = DefaultOccupation;
		}

		public CharaBio(CharaBio other)
		{
			SetFrom(other);
		}

		/// <summary>
		/// Set's this bio's fields to be the same as those of the 
		/// one passed in. This is a shallow copy, so be careful
		/// with reference types.
		/// </summary>
		public void SetFrom(CharaBio other)
		{
			_mugshot = other._mugshot;
			_firstName = other._firstName;
			_lastName = other._lastName;
			_nickName = other._nickName;
			_age = other._age;
			_summary = other._summary;
			_sexualHistory = other._sexualHistory;
			_isAlive = other._isAlive;
			_alignment = other._alignment;
			_occupation = other._occupation;
		}

		public bool Equals(CharaBio other)
		{
			return other != null &&
				   _mugshot == other._mugshot &&
				   _firstName == other._firstName &&
				   _lastName == other._lastName &&
				   _nickName == other._nickName &&
				   _age == other._age &&
				   _summary == other._summary &&
				   _sexualHistory == other._sexualHistory &&
				   _isAlive == other._isAlive &&
				   _alignment == other._alignment &&
				   _occupation == other._occupation;
		}
	}
}
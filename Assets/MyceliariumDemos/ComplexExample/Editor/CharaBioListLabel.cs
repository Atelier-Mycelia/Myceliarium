using UnityEngine;
using UnityEngine.UIElements;

namespace ComplexExample
{
	/// <summary>
	/// An entry in the CharaBioSubwindow's left sidebar. Styling lives in the
	/// uxml/uss, not here.
	/// </summary>
	public class CharaBioListLabel : VisualElement
	{
		public CharaBioListLabel()
		{
			if (_vta == null)
			{
				_vta = Resources.Load<VisualTreeAsset>(_vtaPath);
			}

			_vta.CloneTree(this);
			_nameLabel = this.Q<Label>("NameLabel");
		}

		private static VisualTreeAsset _vta;
		private readonly string _vtaPath = "Editor/Uxml/CharaBioListLabel";
		private readonly Label _nameLabel;

		public string Text
		{
			get => _nameLabel.text;
			set => _nameLabel.text = value;
		}
	}
}

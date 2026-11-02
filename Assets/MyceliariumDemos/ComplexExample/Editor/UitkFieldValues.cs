using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ComplexExample
{
	/// <summary>
	/// Shows a value in a field of any of the types the Myceliarium edit trackers cover,
	/// without the field raising a change event.
	/// </summary>
	public static class UitkFieldValues
	{
		public static void SetWithoutNotify(VisualElement field, object value)
		{
			switch (field)
			{
				case TextField textField:
					textField.SetValueWithoutNotify((string)value);
					break;
				case IntegerField integerField:
					integerField.SetValueWithoutNotify((int)value);
					break;
				case Toggle toggle:
					toggle.SetValueWithoutNotify((bool)value);
					break;
				case EnumField enumField:
					enumField.SetValueWithoutNotify((Enum)value);
					break;
				case DropdownField dropdownField:
					dropdownField.SetValueWithoutNotify((string)value);
					break;
				case ObjectField objectField:
					objectField.SetValueWithoutNotify(value as UnityEngine.Object);
					break;
				default:
					UnityEngine.Debug.LogWarning($"UitkFieldValues: Don't know how to show " +
						$"a value in {field?.GetType().Name} '{field?.name}'.");
					break;
			}
		}
	}
}

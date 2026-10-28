using System;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Reports edits to Toggles. FieldEdit's old and new values are boxed bools.
    /// </summary>
    public sealed class ToggleEditTracker : ImmediateChangeEditTracker<Toggle, bool>
    {
    }//

    /// <summary>
    /// Reports edits to EnumFields. FieldEdit's old and new values are the enum values.
    /// </summary>
    public sealed class EnumFieldEditTracker : ImmediateChangeEditTracker<EnumField, Enum>
    {
    }

    /// <summary>
    /// Reports edits to DropdownFields. FieldEdit's old and new values are the chosen
    /// strings.
    /// </summary>
    public sealed class DropdownFieldEditTracker : ImmediateChangeEditTracker<DropdownField, string>
    {
    }
}

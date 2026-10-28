using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Reports edits to TextFields. FieldEdit's old and new values are strings.
    /// </summary>
    public sealed class TextFieldEditTracker : FocusSessionEditTracker<TextField, string>
    {
    }
}

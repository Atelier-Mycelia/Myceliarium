using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Reports edits to IntegerFields. FieldEdit's old and new values are boxed ints.
    /// </summary>
    public sealed class IntegerFieldEditTracker : FocusSessionEditTracker<IntegerField, int>
    {
    }
}

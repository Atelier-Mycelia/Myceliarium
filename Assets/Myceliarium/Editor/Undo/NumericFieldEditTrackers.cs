using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium
{
    // The other numeric fields work like IntegerFieldEditTracker: a typing session is
    // one edit.

    /// <summary>
    /// Reports edits to LongFields. FieldEdit's old and new values are boxed longs.
    /// </summary>
    public sealed class LongFieldEditTracker : FocusSessionEditTracker<LongField, long>
    {
    }

    /// <summary>
    /// Reports edits to FloatFields. FieldEdit's old and new values are boxed floats.
    /// </summary>
    public sealed class FloatFieldEditTracker : FocusSessionEditTracker<FloatField, float>
    {
    }

    /// <summary>
    /// Reports edits to DoubleFields. FieldEdit's old and new values are boxed doubles.
    /// </summary>
    public sealed class DoubleFieldEditTracker : FocusSessionEditTracker<DoubleField, double>
    {
    }
}

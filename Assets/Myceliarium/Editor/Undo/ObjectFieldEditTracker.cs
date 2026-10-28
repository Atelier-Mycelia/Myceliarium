using UnityEditor.UIElements;
using UnityObject = UnityEngine.Object;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Reports edits to ObjectFields. FieldEdit's old and new values are the objects
    /// (null when cleared).
    /// </summary>
    public sealed class ObjectFieldEditTracker : ImmediateChangeEditTracker<ObjectField, UnityObject>
    {
    }
}

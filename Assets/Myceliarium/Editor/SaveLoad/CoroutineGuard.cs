using System;
using System.Collections;
using System.Collections.Generic;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Runs a coroutine such that an exception thrown anywhere in it (including in
    /// coroutines it yields) is handed to a callback rather than silently ending the
    /// whole thing. A try/catch can't wrap a yield, so this steps the coroutines itself.
    /// </summary>
    internal static class CoroutineGuard
    {
        /// <summary>
        /// The returned coroutine flattens nested coroutines, passing everything else
        /// that was yielded (null, for one) on to whatever runs it. After an exception,
        /// onError is called once and the coroutine ends.
        /// </summary>
        public static IEnumerator Run(IEnumerator routine, Action<Exception> onError)
        {
            var routines = new Stack<IEnumerator>();
            routines.Push(routine);

            while (routines.Count > 0)
            {
                var top = routines.Peek();
                bool hasMore = false;
                bool threw = false;
                try
                {
                    hasMore = top.MoveNext();
                }
                catch (Exception exception)
                {
                    threw = true;
                    onError(exception);
                }

                if (threw)
                {
                    yield break;
                }

                if (!hasMore)
                {
                    routines.Pop();
                }
                else if (top.Current is IEnumerator nested)
                {
                    routines.Push(nested);
                }
                else
                {
                    yield return top.Current;
                }
            }
        }
    }
}

using System;
using UnityDebug = UnityEngine.Debug;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// How a ControlPanelEntry creates its modules (tab, subwindow, loader, logic): one
    /// instance of the type it was told to use. Whether the instance is any good (and
    /// the Init call that follows if so) is up to the caller.
    /// </summary>
    internal static class ControlPanelModuleFactory
    {
        /// <param name="type">What to instantiate. Needs a public parameterless
        /// constructor and has to be a T.</param>
        /// <param name="role">What the module is, for the log message if it fails.</param>
        /// <returns>The new instance, or null (after logging why) if it couldn't be made.
        /// </returns>
        public static T Create<T>(Type type, string role) where T : class
        {
            if (type == null)
            {
                UnityDebug.LogError($"No type was given for the {role}.");
                return null;
            }

            object instance;
            try
            {
                instance = Activator.CreateInstance(type);
            }
            catch (Exception e)
            {
                UnityDebug.LogError($"Could not create the {role} of type {type.Name}: " +
                    $"{e.Message}");
                return null;
            }

            if (instance is T typed)
            {
                return typed;
            }

            UnityDebug.LogError($"{type.Name} is not a valid {typeof(T).Name}, " +
                $"which the {role} needs to be.");
            (instance as IDisposable)?.Dispose();
            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;

[assembly: InternalsVisibleTo("AtMycelia.Myceliarium.Editor.Tests")]

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Registry of all the IControlPanelEntrySaver instances in the project. This is what
    /// you should usually use to access such instances (rather than instantiating them yourself).
    /// </summary>
    [InitializeOnLoad]
    public static class CpEntrySaverReg
    {
        static CpEntrySaverReg()
        {
            ToggleSubs(false);
            ToggleSubs(true);
        }

        private static void ToggleSubs(bool on)
        {
            if (on)
            {
                AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            }
            else
            {
                AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            }
        }

        private static void OnAfterAssemblyReload()
        {
            RefreshRegistry();
            CreateAllSavers();
        }

        /// <summary>
        /// Rebuilds the cached list of saver types. By default this uses Unity's
        /// TypeCache (fast, no need to scan every loaded assembly). A custom
        /// <paramref name="typeProvider"/> can be supplied to make this testable
        /// outside of a live Unity domain reload (e.g. from edit mode tests), or
        /// to otherwise override how candidate types are discovered.
        /// </summary>
        public static void RefreshRegistry(Func<IEnumerable<Type>> typeProvider = null)
        {
            typeProvider ??= GetTypesFromTypeCache;
            var discovered = FilterSaverTypes(typeProvider());

            lock (_registryLock)
            {
                _allSaverTypes = discovered;
            }
        }

        private static IEnumerable<Type> GetTypesFromTypeCache()
        {
            return TypeCache.GetTypesDerivedFrom<IControlPanelEntrySaver>();
        }

        /// <summary>
        /// Pure filtering logic, isolated so it can be unit tested without
        /// touching Unity's TypeCache or AppDomain.
        /// </summary>
        public static Type[] FilterSaverTypes(IEnumerable<Type> candidates)
        {
            if (candidates == null)
            {
                return Array.Empty<Type>();
            }

            return candidates
                .Where(type =>
                    type != null &&
                    _saverType.IsAssignableFrom(type) &&
                    !type.IsAbstract &&
                    !type.IsInterface)
                .ToArray();
        }

        private static readonly Type _saverType = typeof(IControlPanelEntrySaver);
        private static readonly object _registryLock = new object();
        private static Type[] _allSaverTypes = Array.Empty<Type>();

        public static IEnumerable<Type> AllSaverTypes
        {
            get
            {
                lock (_registryLock)
                {
                    return _allSaverTypes.ToArray();
                }
            }
        }

        internal static IEnumerable<IControlPanelEntrySaver> CreateAllSavers(
            Func<Type, IControlPanelEntrySaver> instanceFactory = null)
        {
            instanceFactory ??= DefaultInstanceFactory;
            var saverTypes = AllSaverTypes;
            _savers.Clear();

            foreach (var elem in saverTypes)
            {
                try
                {
                    var instance = instanceFactory(elem);
                    if (instance != null)
                    {
                        _savers.Add(instance);
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Failed to create instance of " +
                        $"{elem.Name}: {ex.Message}");
                }
            }

            return _savers;
        }

        private static IControlPanelEntrySaver DefaultInstanceFactory(Type type)
        {
            return Activator.CreateInstance(type) as IControlPanelEntrySaver;
        }

        /// <summary>
        /// Clears all cached state. Intended for test isolation between test
        /// cases; not meant to be called during normal editor operation.
        /// </summary>
        internal static void ResetForTests()
        {
            lock (_registryLock)
            {
                _allSaverTypes = Array.Empty<Type>();
                _savers.Clear();
            }
        }

        private static readonly List<IControlPanelEntrySaver> _savers = 
            new List<IControlPanelEntrySaver>();

        public static IReadOnlyList<IControlPanelEntrySaver> Savers => _savers;

        public static IList<IControlPanelEntrySaver> GetSaversOfType<T>() 
            where T : IControlPanelEntrySaver
        {
            return GetSaversOfType(typeof(T));
        }

        public static IList<IControlPanelEntrySaver> GetSaversOfType(Type saverType)
        {
            var result = new List<IControlPanelEntrySaver>();
            for (int i = 0; i < _savers.Count; i++)
            {
                var saver = _savers[i];
                if (saverType.IsAssignableFrom(saver.GetType()))
                {
                    result.Add(saver);
                }
            }
            return result;
        }

    }
}
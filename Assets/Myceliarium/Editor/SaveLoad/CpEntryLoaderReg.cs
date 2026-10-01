using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;

[assembly: InternalsVisibleTo("AtMycelia.Myceliarium.Editor.Tests")]

namespace AtMycelia.Myceliarium
{
    [InitializeOnLoad]
    public static class CpEntryLoaderReg
    {
        static CpEntryLoaderReg()
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
            CreateAllLoaders();
        }

        /// <summary>
        /// Rebuilds the cached list of loader types. By default this uses Unity's
        /// TypeCache (fast, no need to scan every loaded assembly). A custom
        /// <paramref name="typeProvider"/> can be supplied to make this testable
        /// outside of a live Unity domain reload (e.g. from edit mode tests), or
        /// to otherwise override how candidate types are discovered.
        /// </summary>
        public static void RefreshRegistry(Func<IEnumerable<Type>> typeProvider = null)
        {
            typeProvider ??= GetTypesFromTypeCache;
            var discovered = FilterLoaderTypes(typeProvider());

            lock (_registryLock)
            {
                _allLoaderTypes = discovered;
            }
        }

        private static IEnumerable<Type> GetTypesFromTypeCache()
        {
            return TypeCache.GetTypesDerivedFrom<IControlPanelEntryLoader>();
        }

        /// <summary>
        /// Pure filtering logic, isolated so it can be unit tested without
        /// touching Unity's TypeCache or AppDomain.
        /// </summary>
        public static Type[] FilterLoaderTypes(IEnumerable<Type> candidates)
        {
            if (candidates == null)
            {
                return Array.Empty<Type>();
            }

            return candidates
                .Where(type =>
                    type != null &&
                    _loaderType.IsAssignableFrom(type) &&
                    !type.IsAbstract &&
                    !type.IsInterface)
                .ToArray();
        }

        private static readonly Type _loaderType = typeof(IControlPanelEntryLoader);
        private static readonly object _registryLock = new object();
        private static Type[] _allLoaderTypes = Array.Empty<Type>();

        public static IEnumerable<Type> AllLoaderTypes
        {
            get
            {
                lock (_registryLock)
                {
                    return _allLoaderTypes.ToArray();
                }
            }
        }

        internal static IEnumerable<IControlPanelEntryLoader> CreateAllLoaders(
            Func<Type, IControlPanelEntryLoader> instanceFactory = null)
        {
            instanceFactory ??= DefaultInstanceFactory;
            var loaderTypes = AllLoaderTypes;
            _loaders.Clear();

            foreach (var elem in loaderTypes)
            {
                try
                {
                    var instance = instanceFactory(elem);
                    if (instance != null)
                    {
                        _loaders.Add(instance);
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Failed to create instance of " +
                        $"{elem.Name}: {ex.Message}");
                }
            }

            return _loaders;
        }

        private static IControlPanelEntryLoader DefaultInstanceFactory(Type type)
        {
            return Activator.CreateInstance(type) as IControlPanelEntryLoader;
        }

        /// <summary>
        /// Clears all cached state. Intended for test isolation between test
        /// cases; not meant to be called during normal editor operation.
        /// </summary>
        internal static void ResetForTests()
        {
            lock (_registryLock)
            {
                _allLoaderTypes = Array.Empty<Type>();
                _loaders.Clear();
            }
        }

        private static readonly List<IControlPanelEntryLoader> _loaders =
            new List<IControlPanelEntryLoader>();

        public static IReadOnlyList<IControlPanelEntryLoader> Loaders => _loaders;

        public static IList<IControlPanelEntryLoader> GetLoadersOfType<T>()
            where T : IControlPanelEntryLoader
        {
            return GetLoadersOfType(typeof(T));
        }

        public static IList<IControlPanelEntryLoader> GetLoadersOfType(Type saverType)
        {
            var result = new List<IControlPanelEntryLoader>();
            for (int i = 0; i < _loaders.Count; i++)
            {
                var saver = _loaders[i];
                if (saverType.IsAssignableFrom(saver.GetType()))
                {
                    result.Add(saver);
                }
            }
            return result;
        }

    }
}
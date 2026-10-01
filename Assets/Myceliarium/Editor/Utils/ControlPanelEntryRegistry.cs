using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;

[assembly: InternalsVisibleTo("AtMycelia.Myceliarium.Editor.Tests")]

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Discovers and maintains a registry of all IControlPanelEntry implementations
    /// in the project using reflection.
    /// </summary>
    public static class ControlPanelEntryRegistry
    {
        [InitializeOnLoadMethod]
        private static void InitializeRegistry()
        {
            InitializeDelayed();

            AssemblyReloadEvents.afterAssemblyReload -= InitializeDelayed;
            AssemblyReloadEvents.afterAssemblyReload += InitializeDelayed;
        }

        private static void InitializeDelayed()
        {
            EditorApplication.delayCall += () =>
            {
                RefreshRegistry();
                CreateAllEntries();
            };
        }

        /// <summary>
        /// Rebuilds the cached list of entry types. By default this uses Unity's
        /// TypeCache (fast, no need to scan every loaded assembly). A custom
        /// <paramref name="typeProvider"/> can be supplied to make this testable
        /// outside of a live Unity domain reload (e.g. from edit mode tests), or
        /// to otherwise override how candidate types are discovered.
        /// </summary>
        public static void RefreshRegistry(Func<IEnumerable<Type>> typeProvider = null)
        {
            typeProvider ??= GetTypesFromTypeCache;
            var discovered = FilterEntryTypes(typeProvider());

            lock (_registryLock)
            {
                _allEntryTypes = discovered;
            }
        }

        private static IEnumerable<Type> GetTypesFromTypeCache()
        {
            return TypeCache.GetTypesDerivedFrom<IControlPanelEntry>();
        }

        /// <summary>
        /// Pure filtering logic, isolated so it can be unit tested without
        /// touching Unity's TypeCache or AppDomain.
        /// </summary>
        public static Type[] FilterEntryTypes(IEnumerable<Type> candidates)
        {
            if (candidates == null)
            {
                return Array.Empty<Type>();
            }

            return candidates
                .Where(type =>
                    type != null &&
                    _entryType.IsAssignableFrom(type) &&
                    !type.IsAbstract &&
                    !type.IsInterface)
                .ToArray();
        }

        private static readonly Type _entryType = typeof(IControlPanelEntry);
        private static readonly object _registryLock = new object();
        private static Type[] _allEntryTypes = Array.Empty<Type>();

        public static IEnumerable<Type> AllEntryTypes
        {
            get
            {
                lock (_registryLock)
                {
                    return _allEntryTypes.ToArray();
                }
            }
        }

        internal static IEnumerable<IControlPanelEntry> CreateAllEntries(
            Func<Type, IControlPanelEntry> instanceFactory = null)
        {
            instanceFactory ??= DefaultInstanceFactory;
            var entryTypes = AllEntryTypes;
            _cachedEntries.Clear();

            foreach (var elem in entryTypes)
            {
                try
                {
                    var instance = instanceFactory(elem);
                    if (instance != null)
                    {
                        _cachedEntries.Add(instance);
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Failed to create instance of " +
                        $"{elem.Name}: {ex.Message}");
                }
            }

            return _cachedEntries;
        }

        private static IControlPanelEntry DefaultInstanceFactory(Type type)
        {
            return Activator.CreateInstance(type) as IControlPanelEntry;
        }

        /// <summary>
        /// Clears all cached state. Intended for test isolation between test
        /// cases; not meant to be called during normal editor operation.
        /// </summary>
        internal static void ResetForTests()
        {
            lock (_registryLock)
            {
                _allEntryTypes = Array.Empty<Type>();
                _cachedEntries.Clear();
            }
        }

        private static readonly IList<IControlPanelEntry> _cachedEntries = 
            new List<IControlPanelEntry>();

        /// <summary>
        /// For every single entry in the registry. If you want entries that derive
        /// from a particular implementor of IControlPanelEntry, better to use 
        /// GetEntriesOfType instead.
        /// </summary>
        public static IReadOnlyList<IControlPanelEntry> Entries
        {
            get
            {
                lock (_registryLock)
                {
                    return (IReadOnlyList<IControlPanelEntry>)_cachedEntries;
                }
            }
        }

        public static IList<T> GetEntriesOfType<T>() where T : IControlPanelEntry
        {
            List<T> result = new List<T>();
            lock (_registryLock)
            {
                Type tType = typeof(T);
                var found = GetEntriesOfType(tType);

                #region Add found elements to result, casted as appropriate
                for (int i = 0; i < found.Count; i++)
                {
                    var elem = found[i];
                    result.Add((T)elem);
                }
                #endregion

                return result;
            }
        }

        public static IList<IControlPanelEntry> GetEntriesOfType(Type entryType)
        {
            List<IControlPanelEntry> result = new List<IControlPanelEntry>();
            lock (_registryLock)
            {
                // Going with a regular for-loop for the sake of performance in 
                // Unity 2022.3.
                for (int i = 0; i < _cachedEntries.Count; i++)
                {
                    var entry = _cachedEntries[i];
                    bool correctType = entryType.IsAssignableFrom(entry.GetType());
                    if (correctType)
                    {
                        result.Add(entry);
                    }
                }
                return result;
            }
        }
    }
}
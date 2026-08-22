using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

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

        public static void RefreshRegistry()
        {
            var discovered = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(SafeGetTypes)
                .Where(type =>
                    _entryType.IsAssignableFrom(type) &&
                    !type.IsAbstract &&
                    !type.IsInterface)
                .ToArray();

            lock (_registryLock)
            {
                _allEntryTypes = discovered;
            }
        }

        private static readonly Type _entryType = typeof(IControlPanelEntry);
        private static readonly object _registryLock = new object();
        private static Type[] _allEntryTypes = Array.Empty<Type>();

        private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
        {
            try 
            { 
                return assembly.GetTypes(); 
            }
            catch (ReflectionTypeLoadException ex) 
            { 
                return ex.Types.Where(type => type != null); 
            }
        }

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

        private static IEnumerable<IControlPanelEntry> CreateAllEntries()
        {
            var entryTypes = AllEntryTypes;
            _cachedEntries.Clear();

            foreach (var elem in entryTypes)
            {
                try
                {
                    var instance = Activator.CreateInstance(elem) as IControlPanelEntry;
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
                    result.Add((T)found);
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
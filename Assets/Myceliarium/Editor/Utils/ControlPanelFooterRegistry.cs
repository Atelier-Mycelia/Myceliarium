using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Discovers all the concrete IControlPanelFooter types in the project. Unlike savers,
    /// footers are bound to one panel's UI, so this hands out a new instance per request
    /// rather than shared ones.
    /// </summary>
    public static class ControlPanelFooterRegistry
    {
        [InitializeOnLoadMethod]
        private static void OnEditorLoad()
        {
            RefreshRegistry();
        }

        /// <summary>
        /// Rebuilds the cached list of footer types. A custom <paramref name="typeProvider"/>
        /// can be supplied to make this testable outside of a live Unity domain.
        /// </summary>
        public static void RefreshRegistry(Func<IEnumerable<Type>> typeProvider = null)
        {
            typeProvider ??= GetTypesFromTypeCache;
            var discovered = FilterFooterTypes(typeProvider());

            lock (_registryLock)
            {
                _allFooterTypes = discovered;
                _refreshed = true;
            }
        }

        private static IEnumerable<Type> GetTypesFromTypeCache()
        {
            return TypeCache.GetTypesDerivedFrom<IControlPanelFooter>();
        }

        /// <summary>
        /// Pure filtering logic, isolated so it can be unit tested without
        /// touching Unity's TypeCache.
        /// </summary>
        public static Type[] FilterFooterTypes(IEnumerable<Type> candidates)
        {
            if (candidates == null)
            {
                return Array.Empty<Type>();
            }

            return candidates
                .Where(type =>
                    type != null &&
                    _footerType.IsAssignableFrom(type) &&
                    !type.IsAbstract &&
                    !type.IsInterface)
                .ToArray();
        }

        private static readonly Type _footerType = typeof(IControlPanelFooter);
        private static readonly object _registryLock = new object();
        private static Type[] _allFooterTypes = Array.Empty<Type>();
        private static bool _refreshed;

        public static IEnumerable<Type> AllFooterTypes
        {
            get
            {
                lock (_registryLock)
                {
                    if (!_refreshed)
                    {
                        RefreshRegistry();
                    }
                    return _allFooterTypes.ToArray();
                }
            }
        }

        public static IList<IControlPanelFooter> CreateFootersOfType<T>()
            where T : IControlPanelFooter
        {
            return CreateFootersOfType(typeof(T));
        }

        /// <summary>
        /// Creates a new instance of every footer type assignable to the given one.
        /// </summary>
        public static IList<IControlPanelFooter> CreateFootersOfType(Type footerSuperType,
            Func<Type, IControlPanelFooter> instanceFactory = null)
        {
            instanceFactory ??= DefaultInstanceFactory;
            var result = new List<IControlPanelFooter>();
            foreach (var type in AllFooterTypes)
            {
                if (!footerSuperType.IsAssignableFrom(type))
                {
                    continue;
                }

                try
                {
                    var instance = instanceFactory(type);
                    if (instance != null)
                    {
                        result.Add(instance);
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Failed to create instance of " +
                        $"{type.Name}: {ex.Message}");
                }
            }
            return result;
        }

        private static IControlPanelFooter DefaultInstanceFactory(Type type)
        {
            return Activator.CreateInstance(type) as IControlPanelFooter;
        }

        /// <summary>
        /// Clears all cached state, for test isolation.
        /// </summary>
        internal static void ResetForTests()
        {
            lock (_registryLock)
            {
                _allFooterTypes = Array.Empty<Type>();
                _refreshed = false;
            }
        }
    }
}

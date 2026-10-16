using System;
using System.Collections.Generic;
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

            var result = new List<Type>();
            foreach (var type in candidates)
            {
                if (type != null &&
                    _footerType.IsAssignableFrom(type) &&
                    type.IsConcrete())
                {
                    result.Add(type);
                }
            }
            return result.ToArray();
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
                    return (Type[])_allFooterTypes.Clone();
                }
            }
        }

        public static IControlPanelFooter CreateFooterOfType<T>()
            where T : IControlPanelFooter
        {
            return CreateFooterOfType(typeof(T));
        }

        /// <summary>
        /// Creates a new instance of the first footer type assignable to the given one.
        /// Panels only have one footer, so if several types match, the extras are
        /// ignored with a warning. Returns null if none match or creation fails.
        /// </summary>
        public static IControlPanelFooter CreateFooterOfType(Type footerSuperType,
            Func<Type, IControlPanelFooter> instanceFactory = null)
        {
            instanceFactory ??= DefaultInstanceFactory;
            Type chosen = null;
            int matchCount = 0;
            foreach (var type in AllFooterTypes)
            {
                if (!footerSuperType.IsAssignableFrom(type))
                {
                    continue;
                }

                chosen ??= type;
                matchCount++;
            }

            if (chosen == null)
            {
                return null;
            }

            if (matchCount > 1)
            {
                UnityEngine.Debug.LogWarning($"Multiple footer types match " +
                    $"{footerSuperType.Name}; using {chosen.Name}.");
            }

            try
            {
                return instanceFactory(chosen);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to create instance of " +
                    $"{chosen.Name}: {ex.Message}");
                return null;
            }
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

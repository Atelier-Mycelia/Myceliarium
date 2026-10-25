using System;
using System.Collections.Generic;
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
            _registry.Refresh(typeProvider);
        }

        /// <summary>
        /// Pure filtering logic, isolated so it can be unit tested without
        /// touching Unity's TypeCache or AppDomain.
        /// </summary>
        public static Type[] FilterSaverTypes(IEnumerable<Type> candidates)
        {
            return TypeRegistry<IControlPanelEntrySaver>.FilterTypes(candidates);
        }

        public static IEnumerable<Type> AllSaverTypes => _registry.Types;

        internal static IEnumerable<IControlPanelEntrySaver> CreateAllSavers(
            Func<Type, IControlPanelEntrySaver> instanceFactory = null)
        {
            return _registry.CreateAll(instanceFactory);
        }

        /// <summary>
        /// Clears all cached state. Intended for test isolation between test
        /// cases; not meant to be called during normal editor operation.
        /// </summary>
        internal static void ResetForTests()
        {
            _registry.Reset();
        }

        public static IReadOnlyList<IControlPanelEntrySaver> Savers => _registry.Instances;

        public static IList<IControlPanelEntrySaver> GetSaversOfType<T>()
            where T : IControlPanelEntrySaver
        {
            return GetSaversOfType(typeof(T));
        }

        public static IList<IControlPanelEntrySaver> GetSaversOfType(Type saverType)
        {
            return _registry.GetInstancesOfType(saverType);
        }

        private static readonly TypeRegistry<IControlPanelEntrySaver> _registry =
            new TypeRegistry<IControlPanelEntrySaver>();
    }
}

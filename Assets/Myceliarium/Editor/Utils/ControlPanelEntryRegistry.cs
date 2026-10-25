using System;
using System.Collections.Generic;
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
            _registry.Refresh(typeProvider);
        }

        /// <summary>
        /// Pure filtering logic, isolated so it can be unit tested without
        /// touching Unity's TypeCache or AppDomain.
        /// </summary>
        public static Type[] FilterEntryTypes(IEnumerable<Type> candidates)
        {
            return TypeRegistry<IControlPanelEntry>.FilterTypes(candidates, HasDefaultConstructor);
        }

        private static bool HasDefaultConstructor(Type type)
        {
            return type.GetConstructor(Type.EmptyTypes) != null;
        }

        /// <summary>
        /// When false (the default), entries whose <see cref="IControlPanelEntry.IsTestOnly"/>
        /// is true are excluded from <see cref="CreateAllEntries"/>. This keeps test-only
        /// doubles discovered via Unity's TypeCache (which scans every loaded assembly,
        /// including test assemblies) from leaking into a real editor session. Tests that
        /// need to exercise IsTestOnly entries should set this to true first.
        /// </summary>
        internal static bool InTestMode { get; set; } = false;

        public static IEnumerable<Type> AllEntryTypes => _registry.Types;

        internal static IEnumerable<IControlPanelEntry> CreateAllEntries(
            Func<Type, IControlPanelEntry> instanceFactory = null)
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
            InTestMode = false;
        }

        /// <summary>
        /// For every single entry in the registry. If you want entries that derive
        /// from a particular implementor of IControlPanelEntry, better to use
        /// GetEntriesOfType instead.
        /// </summary>
        public static IReadOnlyList<IControlPanelEntry> Entries => _registry.Instances;

        public static IList<T> GetEntriesOfType<T>() where T : IControlPanelEntry
        {
            return _registry.GetInstancesOfType<T>();
        }

        public static IList<IControlPanelEntry> GetEntriesOfType(Type entryType)
        {
            return _registry.GetInstancesOfType(entryType);
        }

        // Test-only entries are left out unless InTestMode is on
        private static readonly TypeRegistry<IControlPanelEntry> _registry =
            new TypeRegistry<IControlPanelEntry>(HasDefaultConstructor,
                entry => !entry.IsTestOnly || InTestMode);
    }
}

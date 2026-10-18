using System;
using System.Collections.Generic;
using UnityEditor;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Discovers all the concrete IControlPanelFooter types in the project. Unlike savers,
    /// footers are bound to one panel's UI, so this hands out a new footer instance per
    /// request (one per panel) rather than a shared one.
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
            _registry.Refresh(typeProvider);
        }

        /// <summary>
        /// Pure filtering logic, isolated so it can be unit tested without
        /// touching Unity's TypeCache.
        /// </summary>
        public static Type[] FilterFooterTypes(IEnumerable<Type> candidates)
        {
            return TypeRegistry<IControlPanelFooter>.FilterTypes(candidates);
        }

        public static IEnumerable<Type> AllFooterTypes => _registry.Types;

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
            return _registry.CreateOneOfType(footerSuperType, instanceFactory);
        }

        /// <summary>
        /// Clears all cached state, for test isolation.
        /// </summary>
        internal static void ResetForTests()
        {
            _registry.Reset();
        }

        private static readonly TypeRegistry<IControlPanelFooter> _registry =
            new TypeRegistry<IControlPanelFooter>(refreshOnFirstUse: true);
    }
}

using System;
using System.Collections.Generic;
using UnityEditor;

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
            #region Gather up instances of savers
            var derived = TypeCache.GetTypesDerivedFrom<IControlPanelEntrySaver>();
            for (int i = 0; i < derived.Count; i++)
            {
                var element = derived[i];
                if (element.IsAbstract || element.IsInterface)
                {
                    continue;
                }

                var toRegister = Activator.CreateInstance(element) as IControlPanelEntrySaver;
                _savers.Add(toRegister);
            }
            #endregion
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
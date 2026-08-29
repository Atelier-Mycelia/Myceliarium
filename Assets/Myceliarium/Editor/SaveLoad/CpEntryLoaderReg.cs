using System;
using System.Collections.Generic;
using UnityEditor;

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
            #region Gather up instances of loaders
            var derived = TypeCache.GetTypesDerivedFrom<IControlPanelEntryLoader>();
            for (int i = 0; i < derived.Count; i++)
            {
                var element = derived[i];
                if (element.IsAbstract || element.IsInterface)
                {
                    continue;
                }

                var toRegister = Activator.CreateInstance(element) as IControlPanelEntryLoader;
                _loaders.Add(toRegister);
            }
            #endregion
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
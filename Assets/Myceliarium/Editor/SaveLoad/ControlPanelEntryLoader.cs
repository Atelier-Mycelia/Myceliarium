using System;
using System.Collections;

namespace AtMycelia.Myceliarium
{
    public abstract class ControlPanelEntryLoader : IControlPanelEntryLoader
    {
        public virtual void Load(IControlPanelEntry toLoadFor, Action onComplete = null)
        {
            var coroutine = LoadProcess(toLoadFor, onComplete);
            EditorCoroutineUtility.StartCoroutine(coroutine, this);
        }

        public virtual T GetLoadResult<T>()
        {
            var toReturn = (T)_loadResult;
            return toReturn;
        }

        protected object _loadResult; 
        // ^If the entry is expecting a load result, it can be stored here for
        // retrieval after the load process is complete.

        protected virtual IEnumerator LoadProcess(IControlPanelEntry toLoadFor, 
            Action onComplete = null)
        {
            onComplete?.Invoke();
            yield break;
        }
    }

    public interface IControlPanelEntryLoader
    {
        void Load(IControlPanelEntry toLoadFor, Action onComplete = null);
    }

    public class DefaultControlPanelEntryLoader : ControlPanelEntryLoader
    {
    }

    public interface IAtMyceliaControlPanelEntryLoader : IControlPanelEntryLoader
    {
        // This interface is a marker interface for loaders that are specific
        // to the AtMycelia ecosys.
    }
}
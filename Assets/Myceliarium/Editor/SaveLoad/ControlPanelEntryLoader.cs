using System;
using System.Collections;

namespace AtMycelia.Myceliarium
{
    public abstract class ControlPanelEntryLoader : IControlPanelEntryLoader
    {
        public virtual void Load(IControlPanelEntry toLoadFor, ref object loadResult, 
            Action onComplete = null)
        {
            if (!IsCompatibleWith(toLoadFor))
            {
                return;
            }
            _loadResult = loadResult;
            var coroutine = LoadProcess(toLoadFor, onComplete);
            EditorCoroutineUtility.StartCoroutine(coroutine, this);
        }

        protected object _loadResult; // Since iterators can't have ref parameters,
                                    // we store this in a field.

        public abstract bool IsCompatibleWith(IControlPanelEntry toLoadFor);

        protected virtual IEnumerator LoadProcess(IControlPanelEntry toLoadFor, 
            Action onComplete = null)
        {
            // Default implementation does nothing, just invokes the onComplete callback.
            onComplete?.Invoke();
            yield break;
        }
    }

    public interface IControlPanelEntryLoader
    {
        void Load(IControlPanelEntry toLoadFor, ref object loadResult, Action onComplete = null);
        bool IsCompatibleWith(IControlPanelEntry toLoadFor);
    }

    public class DefaultControlPanelEntryLoader : ControlPanelEntryLoader
    {
        public override bool IsCompatibleWith(IControlPanelEntry toLoadFor)
        {
            // This default loader is compatible with all entries.
            return true;
        }
    }

    public interface IAtMyceliaControlPanelEntryLoader : IControlPanelEntryLoader
    {
        // This interface is a marker interface for loaders that are specific
        // to the AtMycelia ecosys.
    }
}
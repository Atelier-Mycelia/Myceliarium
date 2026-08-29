using System;
using System.Collections;

namespace AtMycelia.Myceliarium
{
    public abstract class ControlPanelEntryLoader : IControlPanelEntryLoader
    {
        public virtual void Load(IControlPanelEntry toLoadFor, Action onComplete = null)
        {
            if (!IsCompatibleWith(toLoadFor))
            {
                return;
            }
            var coroutine = LoadProcess(toLoadFor, onComplete);
            EditorCoroutineUtility.StartCoroutine(coroutine, this);
        }

        protected abstract IEnumerator LoadProcess(IControlPanelEntry toLoadFor,
            Action onComplete = null);

        public abstract bool IsCompatibleWith(IControlPanelEntry toLoadFor);
    }

    public interface IControlPanelEntryLoader
    {
        void Load(IControlPanelEntry toLoadFor, Action onComplete = null);
        bool IsCompatibleWith(IControlPanelEntry toLoadFor);
    }

    public interface IAtMyceliaControlPanelEntryLoader : IControlPanelEntryLoader
    {
        // This interface is a marker interface for loaders that are specific
        // to the AtMycelia ecosys.
    }
}
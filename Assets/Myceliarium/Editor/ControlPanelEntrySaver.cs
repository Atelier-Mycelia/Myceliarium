using System;
using System.Collections;

namespace AtMycelia.Myceliarium
{
    public abstract class ControlPanelEntrySaver : IControlPanelEntrySaver
    {
        public virtual void Save(IControlPanelEntry toSaveFor, Action onComplete = null)
        {
            if (!IsCompatibleWith(toSaveFor))
            {
                return;
            }
            var coroutine = SaveProcess(toSaveFor, onComplete);
            EditorCoroutineUtility.StartCoroutine(coroutine, this);
        }

        protected abstract bool IsCompatibleWith(IControlPanelEntry toSaveFor);
        protected abstract IEnumerator SaveProcess(IControlPanelEntry toSaveFor,
            Action onComplete);

    }

    public interface IControlPanelEntrySaver
    {
        void Save(IControlPanelEntry toSaveFor, Action onComplete = null);
    }

    public interface IAtMyceliaControlPanelEntrySaver : IControlPanelEntrySaver
    {
        // This interface is a marker interface for savers that are specific
        // to the AtMycelia ecosys.
    }
}
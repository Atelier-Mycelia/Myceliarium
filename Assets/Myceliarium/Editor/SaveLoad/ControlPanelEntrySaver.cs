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

        public abstract bool IsCompatibleWith(IControlPanelEntry toSaveFor);

        /// <summary>
        /// Subclasses should override this, calling ControlPanelSignals.SaveCompleted when 
        /// appropriate. The base implementation simply waits two frames so that delayed
        /// Ui controls have enough time to commit their values.
        /// </summary>
        protected virtual IEnumerator SaveProcess(IControlPanelEntry toSaveFor,
            Action onComplete)
        {
            yield return null; yield return null;
        }

    }

    public interface IControlPanelEntrySaver
    {
        void Save(IControlPanelEntry toSaveFor, Action onComplete = null);
        bool IsCompatibleWith(IControlPanelEntry toSaveFor);
    }

    public interface IAtMyceliaControlPanelEntrySaver : IControlPanelEntrySaver
    {
        // This interface is a marker interface for savers that are specific
        // to the AtMycelia ecosys.
    }
}
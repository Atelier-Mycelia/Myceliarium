using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium
{
    public abstract class ControlPanelSubwindow : VisualElement, IControlPanelSubwindow, IDisposable
    {
        public virtual void Init()
        {
            if (_isInitted)
            {
                return;
            }

            LoadUxml();
            RegisterVisualElements();
            _isInitted = true;
            _isDisposed = false;
        }

        public VisualElement Root
        {
            get => this;
        }

        protected bool _isInitted;

        public virtual void Show()
        {
            style.display = DisplayStyle.Flex;
        }

        public virtual void Hide()
        {
            style.display = DisplayStyle.None;
        }

        protected virtual void LoadUxml()
        {
            var vta = Resources.Load<VisualTreeAsset>(PathToUxml);
            if (vta == null)
            {
                string logMessage = $"Failed to load subwindow UXML at {PathToUxml} " +
                    $"for {GetType().Name}. Ensure the UXML file is placed in a " +
                    $"Resources folder and the path is correct.";
                throw new InvalidOperationException(logMessage);
            }

            vta.CloneTree(this);
        }

        public abstract string PathToUxml { get; }

        #region Registration and Binding of Visual Elements
        // These by default do nothing. Subclasses are expected to override
        // them as appropriate.
        protected virtual void RegisterVisualElements()
        {
            // Default: nothing. Subclasses override.
        }

        protected bool _isDisposed;

        public virtual void Bind()
        {
            // Default: nothing. Subclasses override.
        }

        public virtual void Unbind()
        {
            // Default: nothing. Subclasses override.
        }
        #endregion

        protected virtual void SetSubs(bool wantsSubsActive) { } // No-op by default.
        // Subclasses can override to implement logic for activating/deactivating sub-elements.

        public virtual void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            Unbind();
            _isDisposed = true;
        }

        public virtual bool IsVisible => style.display == DisplayStyle.Flex;

        /// <summary>
        /// No-op by default. Subclasses can override to implement refreshing logic.
        /// </summary>
        public virtual void Refresh()
        {

        }
    }

    public interface IControlPanelSubwindow : IRefreshable
    {
        VisualElement Root { get; }

        void Init();
        void Bind();
        void Unbind();
        void Dispose();
        void Show();
        void Hide();
        /// <summary>
        /// Relative to Resources.
        /// </summary>
        string PathToUxml { get; }
        bool IsVisible { get; }
    }

}
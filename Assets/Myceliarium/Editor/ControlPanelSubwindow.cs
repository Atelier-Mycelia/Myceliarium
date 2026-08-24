using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium
{
    public abstract class ControlPanelSubwindow : IControlPanelSubwindow, IDisposable
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

        protected bool _isInitted;

        public virtual void Show()
        {
            if (Root == null)
            {
                string logMessage = $"Cannot show subwindow {GetType().Name} because its " +
                    $"Root VisualElement is null. Ensure Init() has been called.";
                throw new InvalidOperationException(logMessage);
            }
            Root.style.display = DisplayStyle.Flex;
        }

        public virtual void Hide()
        {
            if (Root == null)
            {
                string logMessage = $"Cannot hide subwindow {GetType().Name} because its " +
                    $"Root VisualElement is null. Ensure Init() has been called.";
                throw new InvalidOperationException(logMessage);
            }
            Root.style.display = DisplayStyle.None;
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

            Root = vta.CloneTree();
        }

        public abstract string PathToUxml { get; }
        public VisualElement Root { get; protected set; }

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

        public virtual void RemoveFromHierarchy()
        {
            Root?.RemoveFromHierarchy();
        }

        public virtual T Q<T>(string name) where T : VisualElement
        {
            string logMessage;

            if (Root == null)
            {
                logMessage = $"Cannot query for {typeof(T).Name} named '{name}' because " +
                    $"the Root VisualElement is null. Ensure the subwindow has been " +
                    $"initialized and the UXML loaded.";
                throw new InvalidOperationException(logMessage);
            }

            var element = Root.Q<T>(name);

            if (element == null)
            {
                logMessage = $"Failed to find a {typeof(T).Name} named '{name}' " +
                    $"in the subwindow's hierarchy. Ensure the UXML contains an " +
                    $"element with this name and type.";
                throw new InvalidOperationException(logMessage);
            }
            return element;
        }
        public virtual void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            Unbind();

            // Let's avoid nulling the root. Subwindows (along with the entries
            // they are a part of) are expected to persist until Unity
            // either recompiles scripts or closes. Nulling the root ourselves
            // is just asking for trouble.
            Root?.RemoveFromHierarchy();

            _isDisposed = true;
        }

        public virtual bool IsVisible => Root != null && Root.style.display == DisplayStyle.Flex;
    }

    public interface IControlPanelSubwindow
    {
        VisualElement Root { get; }

        void Init();
        void Bind();
        void Unbind();
        void Dispose();
        void RemoveFromHierarchy();
        void Show();
        void Hide();

        /// <summary>
        /// Searches for a VisualElement of type T with the given name in the subwindow's hierarchy.
        /// </summary>
        T Q<T>(string name) where T : VisualElement;

        /// <summary>
        /// Relative to Resources.
        /// </summary>
        string PathToUxml { get; }
        bool IsVisible { get; }
    }

}
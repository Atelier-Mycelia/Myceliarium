using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UitkButton = UnityEngine.UIElements.Button;

namespace AtMycelia.Myceliarium
{
    public abstract class ControlPanelTab : IControlPanelTab, IDisposable
    {
        public abstract string DisplayName { get; }
        public abstract string PathToUxml { get; }

        public virtual void Init()
        {
            PrepRoot();
            RegisterVisualElements();
            ToggleSubs(true);
        }

        protected virtual void PrepRoot()
        {
            string logMessage = null;
            var vta = Resources.Load<VisualTreeAsset>(PathToUxml);
            if (vta == null)
            {
                logMessage = $"Failed to load tab UXML at {PathToUxml} for {GetType().Name}.";
                throw new InvalidOperationException(logMessage);
            }

            Root = vta.CloneTree();
            Root.name = $"{GetType().Name}_Root";
        }

        public virtual void Register(IControlPanelTab subtab)
        {
            ValidateSubtabPreRegistration(subtab);
            _subtabs.Add(subtab);
            _mainClickable.Add(subtab.Root);
            ApplyOverridesTo(subtab);
        }

        private void ValidateSubtabPreRegistration(IControlPanelTab subtab)
        {
            string logMessage;

            if (subtab == null)
            {
                throw new ArgumentNullException(nameof(subtab));
            }

            if (subtab == this)
            {
                logMessage = $"Cannot attach {GetType().Name} to itself.";
                throw new InvalidOperationException(logMessage);
            }

            if (_subtabs.Contains(subtab))
            {
                logMessage = $"Subtab {subtab.GetType().Name} is already attached " +
                    $"to {GetType().Name}.";
                throw new InvalidOperationException(logMessage);
            }
        }

        public VisualElement Root { get; protected set; }

        /// <summary>
        /// By default, this applies right after the subtab is registered.
        /// This method is to help deal with any Uitk weirdness screwing 
        /// up things like the height and width of the subtab.
        /// </summary>
        protected virtual void ApplyOverridesTo(IControlPanelTab subtab)
        {
            bool doNothing = OverrideSubtabHeight == StyleKeyword.Null && 
                OverrideSubtabWidth == StyleKeyword.Null;
            if (doNothing)
            {
                return;
            }

            var subRoot = subtab.Root;
            var subStyle = subRoot.style;
            subStyle.flexGrow = subStyle.flexShrink = 0; // Helps make sure the overrides stick

            if (OverrideSubtabHeight != StyleKeyword.Null)
            {
                subStyle.height = OverrideSubtabHeight;
            }

            if (OverrideSubtabWidth != StyleKeyword.Null)
            {
                subStyle.width = OverrideSubtabWidth;
            }
        }

        protected virtual StyleLength OverrideSubtabWidth { get; set; } = StyleKeyword.Null;
        protected virtual StyleLength OverrideSubtabHeight { get; set; } = StyleKeyword.Null;

        protected virtual void RegisterVisualElements()
        {
            RegisterMainClickable();
            RegisterIcon();
        }

        /// <summary>
        /// By default, this registers the clickable as a Button. If your tab is using
        /// a different type of VisualElement as the main clickable (say, a Foldout),
        /// override this method to register it appropriately.
        /// </summary>
        protected virtual void RegisterMainClickable()
        {
            _mainClickable = Root.Q<UitkButton>();
            if (_mainClickable == null)
            {
                string logMessage = $"Failed to find a Button in the tab UXML " +
                    $"at {PathToUxml} for {GetType().Name}.";
                throw new InvalidOperationException(logMessage);
            }
        }

        protected VisualElement _mainClickable;

        protected virtual void RegisterIcon()
        {
            _icon = Root.Q<VisualElement>("Icon");
            HideIconAsAppropriate();
        }

        protected VisualElement _icon;

        private void HideIconAsAppropriate()
        {
            // Since we don't want the icon taking up space if it has no bg image.
            if (_icon != null && _icon.style.backgroundImage.value.texture == null)
            {
                _icon.style.width = 0;
                _icon.style.height = 0;
            }
        }

        public virtual string Text
        {
            get
            {
                if (_mainClickable is UitkButton uitkBtn)
                {
                    return uitkBtn.text;
                }
                else
                {
                    string logMessage = $"Cannot get Text for {GetType().Name} because the " +
                        $"button is not a UitkButton.";
                    throw new InvalidOperationException(logMessage);
                }
            }
            set
            {
                if (_mainClickable is UitkButton uitkBtn)
                {
                    uitkBtn.text = value;
                }
                else
                {
                    string logMessage = $"Cannot set Text for {GetType().Name} because the " +
                        $"button is not a UitkButton.";
                    throw new InvalidOperationException(logMessage);
                }
            }
        }

        protected virtual void ToggleSubs(bool on)
        {
            ToggleSubsForButton(on);
        }

        /// <summary>
        /// Will want to override this if you aren't using a UitkButton to
        /// serve as the button for this tab.
        /// </summary>
        /// <param name="on"></param>
        protected virtual void ToggleSubsForButton(bool on)
        {
            if (on)
            {
                _mainClickable.RegisterCallback<ClickEvent>(OnClicked);
            }
            else
            {
                _mainClickable.UnregisterCallback<ClickEvent>(OnClicked);
            }
        }

        public virtual void InvokeClicked()
        {
            OnClicked(null);
        }

        protected virtual void OnClicked(ClickEvent evt)
        {
            IsSelected = true;
            Clicked.Invoke(this);
        }

        public event Action<IControlPanelTab> Clicked = delegate { };

        public virtual void Dispose()
        {
            if (Root != null)
            {
                // No nulling the VisualElements here. Remember, the entries these tabs 
                // are meant to be attached to are expected to persist even when the
                // Control Panel window is closed.
                ToggleSubs(false);
                Clicked = delegate { };
                RemoveFromHierarchy();
            }
        }

        public virtual void RemoveFromHierarchy()
        {
            Root?.RemoveFromHierarchy();
        }

        public virtual bool IsSelected
        {
            get => _mainClickable?.ClassListContains("selected") ?? false;
            set
            {
                if (_mainClickable == null)
                {
                    throw new InvalidOperationException(
                        $"Cannot set IsSelected for {GetType().Name} because the button is null.");
                }
                if (value)
                {
                    _mainClickable.AddToClassList("selected");
                }
                else
                {
                    _mainClickable.RemoveFromClassList("selected");
                }
            }
        }

        public virtual IReadOnlyList<IControlPanelTab> Subtabs => _subtabs;
        private readonly List<IControlPanelTab> _subtabs = new List<IControlPanelTab>();
    }

    public interface IControlPanelTab
    {
        VisualElement Root { get; }

        void Init();

        /// <summary>
        /// The one defining the tab layout. This should be relative to Resources.
        /// </summary>
        string PathToUxml { get; }

        string DisplayName { get; }

        event Action<IControlPanelTab> Clicked;
        void InvokeClicked();

        string Text { get; set; }
        bool IsSelected { get; set; }
        IReadOnlyList<IControlPanelTab> Subtabs { get; }
        void Register(IControlPanelTab subtab);
        void RemoveFromHierarchy();
    }
}


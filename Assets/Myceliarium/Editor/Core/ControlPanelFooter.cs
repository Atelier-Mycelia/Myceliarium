using System;
using System.Collections.Generic;
using AtMycelia.EditorExt;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Handles the elements of a ControlPanel's footer: the language dropdown and the
    /// reinit, save and cancel buttons. Rather than acting on the panel directly, it
    /// emits ControlPanelSignals for the panel to respond to.
    /// </summary>
    public abstract class ControlPanelFooter : IControlPanelFooter
    {
        #region Configurable Properties
        protected virtual string SaveButtonName => "SaveButton";
        protected virtual string CancelButtonName => "CancelButton";
        protected virtual string ReinitButtonName => "ReinitButton";
        protected virtual string LanguageDropdownName => "LanguageDropdown";

        /// <summary>
        /// The languages offered by the dropdown. The first one is selected on Init.
        /// </summary>
        protected virtual IReadOnlyList<string> Languages { get; } = new[] { "English" };

        protected virtual DisplayDialogArgs ReinitDialogArgs
        {
            get
            {
                DisplayDialogArgs args;
                args.title = "Reinit All Entries";
                args.message = "This will reset every entry in this Control Panel back to its " +
                               "startup state, discarding any unsaved runtime changes. Continue?";
                args.acceptanceText = "Reinit";
                args.denialText = "Cancel";
                return args;
            }
        }

        protected virtual DisplayDialogArgs CancelDialogArgs
        {
            get
            {
                DisplayDialogArgs args;
                args.title = "Discard Unsaved Changes";
                args.message = "Some entries have unsaved changes. Closing now will " +
                               "discard them. Are you sure?";
                args.acceptanceText = "Discard and Close";
                args.denialText = "Keep Editing";
                return args;
            }
        }
        #endregion

        public virtual void Init(IControlPanel controlPanel)
        {
            if (controlPanel == null)
            {
                throw new ArgumentNullException(nameof(controlPanel));
            }

            Dispose();
            ControlPanel = controlPanel;
            RegisterVisualElements(controlPanel.Root);
            ConfigureVisualElements();
            SetSubs(true);
            _cancelButton?.SetEnabled(true);
            _isDisposed = false;
        }

        public virtual void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            SetSubs(false);
            _reinitButtonController.Dispose();
            _cancelButtonController.Dispose();
            _saveButton = null;
            _cancelButton = null;
            _reinitButton = null;
            _langDropdown = null;
            ControlPanel = null;
            _isDisposed = true;
        }

        private readonly DisplayDialogButton _reinitButtonController = new DisplayDialogButton();
        private readonly DisplayDialogButton _cancelButtonController = new DisplayDialogButton();

        public IControlPanel ControlPanel { get; private set; }

        protected virtual void RegisterVisualElements(VisualElement root)
        {
            _saveButton = root.Q<Button>(SaveButtonName);
            _cancelButton = root.Q<Button>(CancelButtonName);
            _reinitButton = root.Q<Button>(ReinitButtonName);
            _langDropdown = root.Q<DropdownField>(LanguageDropdownName);
        }

        protected Button _saveButton;
        protected Button _cancelButton;
        protected Button _reinitButton;
        protected DropdownField _langDropdown;

        protected virtual void ConfigureVisualElements()
        {
            PrepLanguageDropdown();
            PrepButtonControllers();
        }

        protected virtual void PrepLanguageDropdown()
        {
            var choices = new List<string>(Languages);
            _langDropdown.choices = choices;
            if (choices.Count > 0)
            {
                _langDropdown.SetValueWithoutNotify(choices[0]);
            }
        }

        protected virtual void PrepButtonControllers()
        {
            _reinitButtonController.Init(_reinitButton, ReinitDialogArgs);
            _cancelButtonController.Init(_cancelButton, CancelDialogArgs, HasUnsavedChanges);
        }

        protected virtual void SetSubs(bool wantsSubsActive)
        {
            if (wantsSubsActive)
            {
                _saveButton.clicked += OnSaveButtonClicked;
                _langDropdown.RegisterValueChangedCallback(OnLanguageDropdownChanged);
                _reinitButtonController.PromptConfirmed += OnReinitConfirmed;

                _cancelButtonController.PromptConfirmed += OnCancelConfirmed;
                _cancelButtonController.BringUpDenied += OnCancelWithoutChanges;

                ControlPanelSignals.CloseRequested += OnCloseRequested;
            }
            else
            {
                _saveButton.clicked -= OnSaveButtonClicked;
                _langDropdown.UnregisterValueChangedCallback(OnLanguageDropdownChanged);
                _reinitButtonController.PromptConfirmed -= OnReinitConfirmed;

                _cancelButtonController.PromptConfirmed -= OnCancelConfirmed;
                _cancelButtonController.BringUpDenied -= OnCancelWithoutChanges;

                ControlPanelSignals.CloseRequested -= OnCloseRequested;
            }
        }

        protected virtual void OnSaveButtonClicked()
        {
            ControlPanelSignals.SaveRequested(ControlPanel);
        }

        protected virtual void OnLanguageDropdownChanged(ChangeEvent<string> evt)
        {
            ControlPanelSignals.LanguageChanged(ControlPanel, evt.newValue);
        }

        /// <summary>
        /// The panel is on its way out, so there's no point in letting the user click
        /// Cancel again.
        /// </summary>
        protected virtual void OnCloseRequested(IControlPanel controlPanel)
        {
            bool weCare = ReferenceEquals(controlPanel, ControlPanel);
            if (!weCare)
            {
                return;
            }

            _cancelButton?.SetEnabled(false);
        }

        protected virtual void OnReinitConfirmed()
        {
            ControlPanelSignals.ReinitRequested(ControlPanel);
        }

        private bool HasUnsavedChanges()
        {
            return EntryCollection.AnyEntryHasUnsavedChanges(ControlPanel.TopLevelEntries);
        }

        protected virtual void OnCancelConfirmed()
        {
            ControlPanelSignals.ReinitRequested(ControlPanel);
            ControlPanelSignals.CloseRequested(ControlPanel);
        }

        protected virtual void OnCancelWithoutChanges()
        {
            ControlPanelSignals.CloseRequested(ControlPanel);
        }

        private bool _isDisposed = true;
    }

    /// <summary>
    /// Handles the footer elements of one ControlPanel. Instances are created through
    /// reflection (see ControlPanelFooterRegistry), one per panel that uses them.
    /// </summary>
    public interface IControlPanelFooter : IDisposable
    {
        /// <summary>
        /// The panel this footer is currently bound to; null before Init and after Dispose.
        /// </summary>
        IControlPanel ControlPanel { get; }

        /// <summary>
        /// Binds this footer to the given panel's footer elements.
        /// </summary>
        void Init(IControlPanel controlPanel);
    }

    /// <summary>
    /// The footer used by AtMyceliaControlPanel; found through reflection.
    /// </summary>
    public class AtMyceliaControlPanelFooter : ControlPanelFooter
    {
    }
}

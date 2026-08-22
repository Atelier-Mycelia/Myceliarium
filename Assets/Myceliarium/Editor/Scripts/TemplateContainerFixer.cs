using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Fixes a known UITK issue where TemplateContainers (and other VE roots
    /// created programmatically) do not correctly calculate their height when
    /// parented under Foldouts. This causes subtabs to collapse or render with
    /// zero height until interacted with.
    ///
    /// This utility applies flex constraints and listens for GeometryChangedEvent
    /// to update the height once the child content resolves its layout.
    /// </summary>
    public static class TemplateContainerFixer
    {
        /// <summary>
        /// Applies the fix to the given tab root. Safe to call multiple times;
        /// existing callbacks are removed before new ones are registered.
        /// </summary>
        public static void FixForFoldout(IControlPanelTab subtab)//
        {
            ValidateInFixForFoldout(subtab, out bool success);
            if (!success)
            {
                return;
            }

            var tempCon = subtab.Root;
            tempCon.style.flexShrink = 0;
            tempCon.style.flexGrow = 0;

            VisualElement actualContent = tempCon.ElementAt(0);
            if (actualContent != null)
            {
                subtab.Root.UnregisterCallback<GeometryChangedEvent>(FixTemplateContainerHeight);
                // ^These tabs persist between ControlPanels opening and closing,
                // so just in case...

                void FixTemplateContainerHeight(GeometryChangedEvent evt)
                {
                    var childStyle = actualContent.resolvedStyle;
                    var contentHeight = childStyle.height;
                    if (contentHeight > 0)
                    {
                        tempCon.style.height = contentHeight;
                    }
                }

                subtab.Root.RegisterCallback<GeometryChangedEvent>(FixTemplateContainerHeight);
            }
        }

        private static void ValidateInFixForFoldout(IControlPanelTab subtab, out bool success)
        {
            success = false;
            string logMessage;
            if (subtab == null)
            {
                logMessage = "TemplateContainerFixer: subtab is null.";
                Debug.LogError(logMessage);
                return;
            }
            var root = subtab.Root;
            if (root == null)
            {
                logMessage = $"TemplateContainerFixer: {subtab.GetType().Name} has a null Root.";
                Debug.LogError(logMessage);
                return;
            }
            if (root.childCount == 0)
            {
                logMessage = $"TemplateContainerFixer: {subtab.GetType().Name} has no child content.";
                Debug.LogWarning(logMessage);
                return;
            }

            float currentHeight = root.resolvedStyle.height;
            bool fixNeeded = float.IsNaN(currentHeight) || (currentHeight < _okayHeightThresh);
            if (!fixNeeded)
            {
                return;
            }

            // We expect the actual content container to be the first child
            VisualElement actualContent = root.childCount > 0 ?
                root.ElementAt(0) :
                null;

            if (actualContent == null)
            {
                logMessage = $"TemplateContainerFixer: Cannot apply fix because " +
                    $"{subtab.GetType().Name} has no child content.";
                Debug.LogWarning(logMessage);
                return;
            }

            success = true;
        }
        private static readonly float _okayHeightThresh = 10;
    }
}

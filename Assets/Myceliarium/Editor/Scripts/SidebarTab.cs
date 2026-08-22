using System;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Represents a tab in the left sidebar of a Myceliarium ControlPanel window.
    /// Each tab corresponds to a specific submenu or control panel entry.
    /// </summary>
    public abstract class SidebarTab : ISidebarTab
    {
        public virtual void Init(VisualElement owner, bool forceReinit = false)
        {
            if (Owner == null || forceReinit)
            {
                Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }
        }

        public bool IsMain { get; set; } = false;
        public virtual VisualElement Owner { get; protected set; }
    }

    public interface ISidebarTab
    {
        void Init(VisualElement owner, bool forceReinit = false);
        bool IsMain { get; set; }
        /// <summary>
        /// Usually the parent.
        /// </summary>
        VisualElement Owner { get; }

    }
}
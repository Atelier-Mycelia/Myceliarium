using System;
using System.Collections.Generic;
using System.Data;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// These handle the logic for their own entries in the appropriate Control 
    /// Panel, represented by a tab on the left sidebar and its associated 
    /// subwindow in the appropriate holder.
    /// 
    /// These should be automatically found through reflection and added to the 
    /// CP when appropriate.
    /// </summary>
    public abstract class ControlPanelEntry : IControlPanelEntry
    {
        public virtual bool IsTestOnly => false;

        /// <summary>
        /// Affects how this is sorted in the Control Panel's left sidebar.
        /// Lower numbers are sorted higher.
        /// </summary>
        public virtual int SortingOrder => 100;

        public virtual bool IsTopLevel => false;
        // ^Why false as the default? We expect that most entries will be
        // nested under others.
        public abstract string SortingName { get; }

        public virtual void Init(bool forceReinit = false)
        {
            if (forceReinit)
            {
                ResetState();
            }

            if (forceReinit || !_isInitted)
            {
                PrepareLeftSidebarTab();
                PrepareSubentries();
                PrepareSubwindow();
                SetSubs(true);
                _isInitted = true;
            }
        }

        public virtual bool IsInitted
        {
            get => _isInitted;
            protected set => _isInitted = value;
        }
        private bool _isInitted;

        private void ResetState()
        {
            SetSubs(false);
            RemoveFromHierarchy();

            for (int i = 0; i < _subentries.Count; i++)
            {
                _subentries[i].RemoveFromHierarchy();
            }
            _subentries.Clear();

            _tab.Dispose();
            _subwindow?.Dispose();
            _tab = null;
            _subwindow = null;
            _isInitted = false;
        }

        protected abstract void PrepareLeftSidebarTab();

        protected IControlPanelTab _tab;

        // Expected for subclasses to override this method if they have subentries.
        // The default implementation does nothing.
        protected virtual void PrepareSubentries() { }

        public virtual IReadOnlyList<IControlPanelEntry> GetSubentries(bool recursive = false)
        {
            List<IControlPanelEntry> result;

            if (recursive)
            {
                result = new List<IControlPanelEntry>(_subentries);
                for (int i = 0; i < _subentries.Count; i++)
                {
                    var directChild = _subentries[i];
                    if (directChild == null)
                    {
                        string logMessage = $"Subentry at index {i} of {GetType().Name} was null. " +
                            $"This should not happen if PrepareSubentries() has been called.";
                        throw new InvalidOperationException(logMessage);
                    }

                    // This is a depth-first traversal of the subentry tree.
                    // Note that this will include the direct child itself in the result, so
                    // we don't need to add it separately.
                    // This is because GetSubentries(true) will return a list that includes
                    // the entry itself as well as its subentries.
                    var childSubs = directChild.GetSubentries(true);
                    result.AddRange(childSubs);
                }
            }
            else
            {
                result = _subentries; // So we won't need as many allocations
            }

            return result;
        }
        protected readonly List<IControlPanelEntry> _subentries = new List<IControlPanelEntry>();

        protected virtual void PrepareSubwindow() { }

        public virtual bool IsMeantToHaveSubwindow => true; 
        // ^Most tabs are expected to have subwindows, so...

        protected IControlPanelSubwindow _subwindow;

        protected virtual void SetSubs(bool on)
        {
            if (on)
            {
                if (_tab != null)
                {
                    _tab.Clicked += OnTabClicked;
                }
            }
            else
            {
                if (_tab != null)
                {
                    _tab.Clicked -= OnTabClicked;
                }
            }
        }

        protected void OnTabClicked(IControlPanelTab tabClicked)
        {
            ControlPanelSignals.OnEntryTabClicked(this);
        }

        // Clients shouldn't even try to access the Subwindow or tab before
        // the Init call, hence why the getters here throw exceptions if the
        // subwindow or tab button is null. This is to help catch bugs.
        public virtual IControlPanelTab Tab
        {
            get
            {
                return _tab;
            }
            protected set => _tab = value;
        }

        public virtual IControlPanelSubwindow Subwindow
        {
            get
            {
                if (IsMeantToHaveSubwindow && _subwindow == null)
                {
                    string logMessage = $"Subwindow for {GetType().Name} was null. " +
                        $"This should not happen if Init() has been called.";
                    throw new InvalidOperationException(logMessage);
                }
                return _subwindow;
            }
            protected set
            {
                if (!IsMeantToHaveSubwindow)
                {
                    string logMessage = $"Attempted to set Subwindow for {GetType().Name}, " +
                        $"but this entry is not meant to have one.";
                    throw new InvalidOperationException(logMessage);
                }
                _subwindow = value;
            }
        }

        public virtual void RemoveFromHierarchy()
        {
            _tab?.Root.RemoveFromHierarchy();
            _subwindow?.Root.RemoveFromHierarchy();
        }

        public virtual void Select()
        {
            if (_tab == null || _tab.IsSelected)
            {
                return;
            }

            _subwindow?.Refresh();
            _subwindow?.Show();

            _tab.IsSelected = true;
            if (this.IsMeantToHaveSubwindow)
            {
                // This should keep the subentries' windows from getting in the
                // way of this one's.
                for (int i = 0; i < _subentries.Count; i++)
                {
                    var subentry = _subentries[i];
                    subentry.Deselect();
                }
            }
        }

        public virtual void Deselect()
        {
            if (_tab == null || !_tab.IsSelected)
            {
                return;
            }

            _subwindow?.Hide();
            _tab.IsSelected = false;
        }

        public virtual bool HasSubentries => _subentries.Count > 0;

    }

    public interface IControlPanelEntry
    {
        /// <summary>
        /// Functions as the constructor for this entry. Should be called once when the 
        /// entry is first created, and can be called again if the entry needs to 
        /// be reinitialized.
        /// </summary>
        void Init(bool forceReinit = false);
        bool IsInitted { get; }

        /// <summary>
        /// Decides how this entry is sorted in the Control Panel's left sidebar.
        /// Lower numbers are sorted higher. When two entries have the same sorting order,
        /// they are sorted alphabetically by their SortingName.
        /// </summary>
        int SortingOrder { get; }
        /// <summary>
        /// When two ControlPanelEntries have the same SortingOrder, they are then sorted
        /// based on this. Alphabetically.
        /// </summary>
        string SortingName { get; }

        IControlPanelTab Tab { get; }

        bool IsMeantToHaveSubwindow { get; }
        IControlPanelSubwindow Subwindow { get; }

        bool IsTopLevel { get; }

        bool HasSubentries { get; }
        IReadOnlyList<IControlPanelEntry> GetSubentries(bool recursive = false);

        void RemoveFromHierarchy();

        void Select();
        void Deselect();

        bool IsTestOnly { get; }
    }

    public interface IAtMyceliaControlPanelEntry : IControlPanelEntry
    {
        // This interface can be used to mark entries that are specific
        // to the Atelycelia ecosys. This is to avoid needing to use magic
        // strings when filtering entries to attach and whatnot.
    }
}
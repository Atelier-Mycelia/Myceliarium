using System;
using System.Collections.Generic;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// The subentries of one ControlPanelEntry. They're created just once, 
    /// which is the first time the entry is prepared. The instances are kept
    /// for the entry's lifetime. Reinitializing the entry reinitializes
    /// those same instances, so their identity stays stable.
    /// </summary>
    internal sealed class SubentryGroup
    {
        

        /// <summary>
        /// Creates the subentries if that hasn't happened yet, (re)initializes them, and
        /// registers their tabs under the parent's tab.
        /// </summary>
        public void Prepare(Func<IEnumerable<IControlPanelEntry>> creationFunc,
            IControlPanelTab parentTab)
        {
            if (!_alreadyCreated)
            {
                var toAdd = creationFunc();
                _entries.AddRange(toAdd);
                _alreadyCreated = true;
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                var elem = _entries[i];
                // Subentries that have been initialized before (and so maybe disposed
                // along with the parent) need the forced reinit to come back up.
                elem.Init(forceReinit: elem.IsInitted);
                if (elem.Tab != null)
                {
                    parentTab.Register(elem.Tab);
                }
            }
        }

        private bool _alreadyCreated;

        private readonly List<IControlPanelEntry> _entries = new List<IControlPanelEntry>();

        public void DisposeAll()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                _entries[i].Dispose();
            }
        }

        public void DeselectAll()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                _entries[i].Deselect();
            }
        }

        /// <param name="recursive">Includes the subentries' own subentries, depth-first
        /// (each direct child comes before what's nested under it).</param>
        /// <param name="ownerName">For the error message about a null subentry.</param>
        public IReadOnlyList<IControlPanelEntry> Get(bool recursive, string ownerName)
        {
            if (!recursive)
            {
                return _entries; // So we won't need as many allocations
            }

            var result = new List<IControlPanelEntry>(_entries);
            for (int i = 0; i < _entries.Count; i++)
            {
                var directChild = _entries[i];
                if (directChild == null)
                {
                    string logMessage = $"Subentry at index {i} of {ownerName} was null. " +
                        $"This should not happen if Init() has been called.";
                    throw new InvalidOperationException(logMessage);
                }

                // The child's own recursive list includes the child itself, so
                // we don't need to add it separately.
                var childSubs = directChild.GetSubentries(true);
                result.AddRange(childSubs);
            }
            return result;
        }

        public bool Any => _entries.Count > 0;
        
    }
}

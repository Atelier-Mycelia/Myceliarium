using UnityEngine;

namespace AtMycelia.Myceliarium.Demos.RpgStats
{
    [System.Serializable]
    public class Trait
    {
        [SerializeField] protected int _id = 0;
        [SerializeField] protected string _name = string.Empty;
        [SerializeField] protected string _displayName = string.Empty;
        [SerializeField] protected string _description = string.Empty;
        [SerializeField] protected string _typeHeader = string.Empty;

        public virtual int Id => _id;
        public virtual string Name => _name;
        public virtual string DisplayName => _displayName;
        public virtual string Description => _description;
        public virtual string TypeHeader => _typeHeader;

    }
}
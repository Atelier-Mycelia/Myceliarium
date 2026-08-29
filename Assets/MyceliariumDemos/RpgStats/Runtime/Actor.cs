using UnityEngine;

namespace AtMycelia.Myceliarium.Demos.RpgStats
{
    [System.Serializable]
    public class Actor
    {
        [Header("General Settings")]
        [SerializeField] protected string _name = string.Empty;
        [SerializeField] protected string _displayName = string.Empty;
        [SerializeField] protected string _nickname = string.Empty;
        [SerializeField] protected string _profile = string.Empty;
        [SerializeField] protected int _initialLevel = 1;
        [SerializeField] protected int _maxLevel = 99;
        [SerializeField] protected int _classId = 0;

        [Header("Images")]
        [SerializeField] protected Sprite _faceImage = null;
        [SerializeField] protected Sprite _overworldImage = null;
        [SerializeField] protected Sprite _sideViewBattlerImage = null;

        [SerializeField] protected string _notes = string.Empty;

        public string Name => _name;
        public string DisplayName => _displayName;
        public string Nickname => _nickname;
        public string Profile => _profile;
        public int InitialLevel => _initialLevel;
        public int MaxLevel => _maxLevel;
        public int ClassId => _classId;

        public Sprite FaceImage => _faceImage;
        public Sprite OverworldImage => _overworldImage;
        public Sprite SideViewBattlerImage => _sideViewBattlerImage;
        public string Notes => _notes;

    }
}
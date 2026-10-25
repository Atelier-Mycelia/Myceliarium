using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleExample
{
    public class NoteContents : ScriptableObject
    {
        [SerializeField] private string _text;
        public string Text
        {
            get => _text;
            set => _text = value;
        }
    }
}
using System.Collections.Generic;
using System.Reflection;
using AtMycelia.Myceliarium;
using ComplexExample;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium.Tests
{
    public class CharaBioLogicTests
    {
        private CharaBioRegistry _registry;
        private CharaBioLogic _logic;

        [SetUp]
        public void SetUp()
        {
            _registry = ScriptableObject.CreateInstance<CharaBioRegistry>();
            _logic = new CharaBioLogic();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_registry);
        }

        /// <summary>
        /// Puts the given bios in the registry (its list is private) and loads them.
        /// </summary>
        private void LoadWith(int bioCount)
        {
            var bios = new List<CharaBio>();
            for (int i = 0; i < bioCount; i++)
            {
                bios.Add(new CharaBio { FirstName = $"Bio{i}" });
            }
            typeof(CharaBioRegistry)
                .GetField("_characterBios", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(_registry, bios);

            _logic.Load(_registry);
        }

        private static FieldEdit EditOf(string fieldName, CharaBio bio, object oldValue,
            object newValue)
        {
            return new FieldEdit(new VisualElement { name = fieldName }, bio, oldValue, newValue);
        }

        #region Loading
        [Test]
        public void Load_CopiesTheRegistrysBios_AndPadsToTheMinimum()
        {
            LoadWith(3);

            Assert.That(_logic.Bios.Count, Is.EqualTo(CharaBioLogic.MinBioCount));
            Assert.That(_logic.Bios[0].FirstName, Is.EqualTo("Bio0"));
            Assert.That(_logic.Bios[0], Is.Not.SameAs(_registry.CharacterBios[0]));
            Assert.That(_logic.Registry, Is.SameAs(_registry));
        }

        [Test]
        public void Load_AfterEdits_ClearsTheHistoryAndTheUnsavedFlag()
        {
            LoadWith(10);
            _logic.AddBio();
            Assert.That(_logic.HasUnsavedChanges, Is.True);

            LoadWith(10);

            Assert.That(_logic.HasUnsavedChanges, Is.False);
            Assert.That(_logic.History.CanUndo, Is.False);
        }
        #endregion

        #region Adding and deleting
        [Test]
        public void AddBio_AddsOne_AndMakesTheStateDirty()
        {
            LoadWith(10);

            _logic.AddBio();

            Assert.That(_logic.Bios.Count, Is.EqualTo(11));
            Assert.That(_logic.HasUnsavedChanges, Is.True);
        }

        [Test]
        public void AddBio_UndoAndRedo_RemoveAndRestoreTheSameBio()
        {
            LoadWith(10);
            _logic.AddBio();
            var added = _logic.Bios[10];

            _logic.History.Undo();
            Assert.That(_logic.Bios.Count, Is.EqualTo(10));
            Assert.That(_logic.HasUnsavedChanges, Is.False);

            _logic.History.Redo();
            Assert.That(_logic.Bios[10], Is.SameAs(added));
        }

        [Test]
        public void DeleteBio_AtTheMinimumCount_PadsWithABlank()
        {
            LoadWith(10);
            var victim = _logic.Bios[4];

            _logic.DeleteBio(victim);

            Assert.That(_logic.Bios.Count, Is.EqualTo(CharaBioLogic.MinBioCount));
            Assert.That(_logic.Bios, Has.No.Member(victim));
        }

        [Test]
        public void DeleteBio_Undo_RestoresTheBioAtItsIndexWithoutLeavingPadding()
        {
            LoadWith(10);
            var victim = _logic.Bios[4];

            _logic.DeleteBio(victim);
            _logic.History.Undo();

            Assert.That(_logic.Bios.Count, Is.EqualTo(10));
            Assert.That(_logic.Bios[4], Is.SameAs(victim));
            Assert.That(_logic.HasUnsavedChanges, Is.False);
        }

        [Test]
        public void DeleteBio_UndoAndRedo_Repeatedly_StaysConsistent()
        {
            LoadWith(12);
            var victim = _logic.Bios[2];

            _logic.DeleteBio(victim);
            Assert.That(_logic.Bios.Count, Is.EqualTo(11)); // No padding needed
            _logic.History.Undo();
            _logic.History.Redo();
            _logic.History.Undo();

            Assert.That(_logic.Bios.Count, Is.EqualTo(12));
            Assert.That(_logic.Bios[2], Is.SameAs(victim));
        }

        [Test]
        public void DeleteBio_UndoRaisesBiosChangedWithTheRestoredBio()
        {
            LoadWith(10);
            var victim = _logic.Bios[1];
            _logic.DeleteBio(victim);
            CharaBio shown = null;
            _logic.MultiBiosChanged += bio => shown = bio;

            _logic.History.Undo();

            Assert.That(shown, Is.SameAs(victim));
        }

        [Test]
        public void DeleteBio_NotInTheList_DoesNothing()
        {
            LoadWith(10);

            _logic.DeleteBio(new CharaBio());

            Assert.That(_logic.History.CanUndo, Is.False);
        }
        #endregion

        #region Edits
        [Test]
        public void CreateEditCommand_ForABioField_UndoesAndRedoesTheValue()
        {
            LoadWith(10);
            var bio = _logic.Bios[0];
            bio.FirstName = "New"; // As if the view already put it in
            var command = _logic.CreateEditCommand(EditOf("FirstName", bio, "Bio0", "New"));
            _logic.History.Record(command);

            _logic.History.Undo();
            Assert.That(bio.FirstName, Is.EqualTo("Bio0"));
            Assert.That(_logic.HasUnsavedChanges, Is.False);

            _logic.History.Redo();
            Assert.That(bio.FirstName, Is.EqualTo("New"));
            Assert.That(_logic.HasUnsavedChanges, Is.True);
        }

        [Test]
        public void CreateEditCommand_HandlesEachKindOfValue()
        {
            LoadWith(10);
            var bio = _logic.Bios[0];

            _logic.CreateEditCommand(EditOf("Age", bio, 0, 30)).Execute();
            _logic.CreateEditCommand(EditOf("IsAlive", bio, true, false)).Execute();
            _logic.CreateEditCommand(EditOf("Alignment", bio, CharaAlignment.Neutral,
                CharaAlignment.Chaotic)).Execute();
            _logic.CreateEditCommand(EditOf("Occupation", bio, "Unemployed", "Thief")).Execute();

            Assert.That(bio.Age, Is.EqualTo(30));
            Assert.That(bio.IsAlive, Is.False);
            Assert.That(bio.Alignment, Is.EqualTo(CharaAlignment.Chaotic));
            Assert.That(bio.Occupation, Is.EqualTo("Thief"));
        }

        [Test]
        public void CreateEditCommand_Applying_RaisesBioChanged()
        {
            LoadWith(10);
            var bio = _logic.Bios[0];
            CharaBio changed = null;
            _logic.BioChanged += b => changed = b;

            _logic.CreateEditCommand(EditOf("Nickname", bio, "", "Nick")).Execute();

            Assert.That(changed, Is.SameAs(bio));
        }

        [Test]
        public void CreateEditCommand_ForSomethingThatIsNotABio_ReturnsNull()
        {
            var edit = new FieldEdit(new VisualElement { name = "FirstName" }, "not a bio", "a", "b");

            Assert.That(_logic.CreateEditCommand(edit), Is.Null);
        }
        #endregion
    }
}

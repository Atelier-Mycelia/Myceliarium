using System.Collections.Generic;
using System.Reflection;
using AtMycelia.Myceliarium;
using ComplexExample;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium.Tests
{
    public class SkillLogicTests
    {
        private SkillRegistry _registry;
        private SkillLogic _logic;

        [SetUp]
        public void SetUp()
        {
            _registry = ScriptableObject.CreateInstance<SkillRegistry>();
            _logic = new SkillLogic();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_registry);
        }

        /// <summary>
        /// Puts that many named skills in the registry (its list is private) and loads them.
        /// </summary>
        private void LoadWith(int skillCount)
        {
            var skills = new List<Skill>();
            for (int i = 0; i < skillCount; i++)
            {
                skills.Add(new Skill { skillName = $"Skill{i}" });
            }
            typeof(SkillRegistry)
                .GetField("_skills", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(_registry, skills);

            _logic.Load(_registry);
        }

        private static FieldEdit EditOf(string fieldName, Skill skill, object oldValue,
            object newValue)
        {
            return new FieldEdit(new VisualElement { name = fieldName }, skill, oldValue,
                newValue);
        }

        #region Loading
        [Test]
        public void Load_CopiesTheRegistrysSkills()
        {
            LoadWith(12);

            Assert.That(_logic.Skills.Count, Is.EqualTo(12));
            Assert.That(_logic.Skills[0].skillName, Is.EqualTo("Skill0"));
            Assert.That(_logic.Skills[0], Is.Not.SameAs(_registry.Skills[0]));
            Assert.That(_logic.Registry, Is.SameAs(_registry));
        }

        [Test]
        public void Load_EmptyRegistry_StartsOutWithTheStarterSkills_ButNotDirty()
        {
            LoadWith(0);

            var starters = SkillRegistry.StarterSkills;
            Assert.That(_logic.Skills[0].skillName, Is.EqualTo(starters[0].skillName));
            Assert.That(_logic.HasUnsavedChanges, Is.False);
            Assert.That(_registry.Skills.Count, Is.EqualTo(0));
        }

        [Test]
        public void Load_FewerThanTheMinimum_IsPaddedWithBlanks_ButNotDirty()
        {
            LoadWith(3);

            Assert.That(_logic.Skills.Count, Is.EqualTo(SkillLogic.MinSkillCount));
            Assert.That(_logic.Skills[2].skillName, Is.EqualTo("Skill2"));
            Assert.That(_logic.Skills[3].skillName, Is.Empty);
            Assert.That(_logic.HasUnsavedChanges, Is.False);
        }

        [Test]
        public void Load_Null_IsAlsoPaddedToTheMinimum()
        {
            _logic.Load(null);

            Assert.That(_logic.Skills.Count, Is.EqualTo(SkillLogic.MinSkillCount));
        }

        [Test]
        public void Load_AfterEdits_ClearsTheHistoryAndTheUnsavedFlag()
        {
            LoadWith(3);
            _logic.AddSkill();
            Assert.That(_logic.HasUnsavedChanges, Is.True);

            LoadWith(3);

            Assert.That(_logic.HasUnsavedChanges, Is.False);
            Assert.That(_logic.History.CanUndo, Is.False);
        }

        [Test]
        public void Load_Null_StillGivesTheStarterSkills()
        {
            _logic.Load(null);

            Assert.That(_logic.Skills.Count, Is.GreaterThan(0));
            Assert.That(_logic.Registry, Is.Null);
        }
        #endregion

        #region Adding and deleting
        [Test]
        public void AddSkill_AddsOne_ShowsIt_AndMakesTheStateDirty()
        {
            LoadWith(10);
            Skill shown = null;
            _logic.SkillsChanged += skill => shown = skill;

            _logic.AddSkill();

            Assert.That(_logic.Skills.Count, Is.EqualTo(11));
            Assert.That(shown, Is.SameAs(_logic.Skills[10]));
            Assert.That(_logic.HasUnsavedChanges, Is.True);
        }

        [Test]
        public void AddSkill_Undo_RemovesIt_AndRedoBringsBackTheSameOne()
        {
            LoadWith(10);
            _logic.AddSkill();
            var added = _logic.Skills[10];

            _logic.Undo();
            Assert.That(_logic.Skills.Count, Is.EqualTo(10));
            Assert.That(_logic.HasUnsavedChanges, Is.False);

            _logic.Redo();
            Assert.That(_logic.Skills[10], Is.SameAs(added));
        }

        [Test]
        public void DeleteSkill_RemovesIt_AndUndoPutsItBackWhereItWas()
        {
            LoadWith(12);
            var doomed = _logic.Skills[1];

            _logic.DeleteSkill(doomed);
            Assert.That(_logic.Skills.Count, Is.EqualTo(11));
            Assert.That(System.Linq.Enumerable.Contains(_logic.Skills, doomed), Is.False);

            Skill shown = null;
            _logic.SkillsChanged += skill => shown = skill;
            _logic.Undo();

            Assert.That(_logic.Skills[1], Is.SameAs(doomed));
            Assert.That(shown, Is.SameAs(doomed));
        }

        [Test]
        public void DeleteSkill_NotInTheList_DoesNothing()
        {
            LoadWith(12);

            _logic.DeleteSkill(new Skill());

            Assert.That(_logic.Skills.Count, Is.EqualTo(12));
            Assert.That(_logic.HasUnsavedChanges, Is.False);
        }

        [Test]
        public void DeleteSkill_AtTheMinimum_ReplacesItWithABlank()
        {
            LoadWith(SkillLogic.MinSkillCount);
            var doomed = _logic.Skills[0];

            _logic.DeleteSkill(doomed);

            Assert.That(_logic.Skills.Count, Is.EqualTo(SkillLogic.MinSkillCount));
            Assert.That(System.Linq.Enumerable.Contains(_logic.Skills, doomed), Is.False);
            Assert.That(_logic.Skills[SkillLogic.MinSkillCount - 1].skillName, Is.Empty);
        }

        [Test]
        public void DeleteSkill_AtTheMinimum_UndoRemovesTheBlank_AndRedoReplacesItAgain()
        {
            LoadWith(SkillLogic.MinSkillCount);
            var doomed = _logic.Skills[4];
            _logic.DeleteSkill(doomed);

            _logic.Undo();
            Assert.That(_logic.Skills.Count, Is.EqualTo(SkillLogic.MinSkillCount));
            Assert.That(_logic.Skills[4], Is.SameAs(doomed));
            Assert.That(_logic.HasUnsavedChanges, Is.False);

            _logic.Redo();
            Assert.That(_logic.Skills.Count, Is.EqualTo(SkillLogic.MinSkillCount));
            Assert.That(System.Linq.Enumerable.Contains(_logic.Skills, doomed), Is.False);
        }
        #endregion

        #region Field edits
        [Test]
        public void CreateEditCommand_ForASkill_IsRecordedNotExecuted()
        {
            LoadWith(2);
            var skill = _logic.Skills[0];
            skill.skillName = "Typed"; // The view has already put it in
            var command = _logic.CreateEditCommand(EditOf("SkillName", skill, "Skill0", "Typed"));
            bool raised = false;
            _logic.SkillChanged += _ => raised = true;

            Assert.That(command, Is.Not.Null);
            Assert.That(skill.skillName, Is.EqualTo("Typed"));
            Assert.That(raised, Is.False);
        }

        [Test]
        public void CreateEditCommand_ForSomethingElse_ReturnsNull()
        {
            LoadWith(2);

            var command = _logic.CreateEditCommand(EditOf("SkillName", null, "a", "b"));

            Assert.That(command, Is.Null);
        }

        [Test]
        public void EditCommand_UndoAndRedo_ChangeTheSkillThatWasEdited()
        {
            LoadWith(2);
            var edited = _logic.Skills[0];
            var shownOnUndo = new List<Skill>();
            _logic.SkillChanged += shownOnUndo.Add;
            edited.mpCost = 7;
            _logic.History.Record(_logic.CreateEditCommand(EditOf("MpCost", edited, 0, 7)));

            _logic.Undo();
            Assert.That(edited.mpCost, Is.EqualTo(0));
            Assert.That(_logic.HasUnsavedChanges, Is.False);

            _logic.Redo();
            Assert.That(edited.mpCost, Is.EqualTo(7));
            Assert.That(shownOnUndo, Is.EqualTo(new[] { edited, edited }));
        }

        [Test]
        public void EditCommand_UndoesEnumFields()
        {
            LoadWith(1);
            var skill = _logic.Skills[0];
            skill.scope = SkillScope.AllAllies;
            _logic.History.Record(_logic.CreateEditCommand(
                EditOf("Scope", skill, SkillScope.OneEnemy, SkillScope.AllAllies)));

            _logic.Undo();

            Assert.That(skill.scope, Is.EqualTo(SkillScope.OneEnemy));
        }

        [Test]
        public void EditCommand_ForAnUnmappedField_ChangesNothingAndRaisesNothing()
        {
            LoadWith(1);
            var skill = _logic.Skills[0];
            var raised = new List<Skill>();
            _logic.SkillChanged += raised.Add;
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("NotAField"));
            _logic.History.Record(_logic.CreateEditCommand(EditOf("NotAField", skill, 1, 2)));

            _logic.Undo();

            Assert.That(raised, Is.Empty);
        }
        #endregion

        [Test]
        public void MarkSaved_AfterEdits_ClearsTheUnsavedFlag()
        {
            LoadWith(2);
            _logic.AddSkill();

            _logic.MarkSaved();

            Assert.That(_logic.HasUnsavedChanges, Is.False);
        }
    }
}

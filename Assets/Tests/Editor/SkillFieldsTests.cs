using System.Collections.Generic;
using ComplexExample;
using NUnit.Framework;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium.Tests
{
    public class SkillFieldsTests
    {
        [Test]
        public void Get_ReturnsWhatTheSkillHolds()
        {
            var skill = new Skill { skillName = "Fire", mpCost = 12, scope = SkillScope.AllEnemies };

            Assert.That(SkillFields.Get(skill, "SkillName"), Is.EqualTo("Fire"));
            Assert.That(SkillFields.Get(skill, "MpCost"), Is.EqualTo(12));
            Assert.That(SkillFields.Get(skill, "Scope"), Is.EqualTo(SkillScope.AllEnemies));
        }

        [Test]
        public void TrySet_PutsTheValueInTheSkill()
        {
            var skill = new Skill();

            Assert.That(SkillFields.TrySet(skill, "Formula", "a.atk"), Is.True);
            Assert.That(SkillFields.TrySet(skill, "CriticalHits", false), Is.True);
            Assert.That(SkillFields.TrySet(skill, "DamageType", SkillDamageType.MpDrain), Is.True);

            Assert.That(skill.formula, Is.EqualTo("a.atk"));
            Assert.That(skill.criticalHits, Is.False);
            Assert.That(skill.damageType, Is.EqualTo(SkillDamageType.MpDrain));
        }

        [Test]
        public void TrySet_UnknownField_ReturnsFalseAndWarns()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Nope"));

            Assert.That(SkillFields.TrySet(new Skill(), "Nope", 1), Is.False);
        }

        [Test]
        public void Has_NullOrUnknown_IsFalse()
        {
            Assert.That(SkillFields.Has(null), Is.False);
            Assert.That(SkillFields.Has("Nope"), Is.False);
            Assert.That(SkillFields.Has("SkillName"), Is.True);
        }

        [Test]
        public void EveryField_RoundTripsItsOwnValue()
        {
            var source = new Skill();
            var target = new Skill { skillName = "other", mpCost = 99 };

            foreach (string name in SkillFields.Names)
            {
                SkillFields.TrySet(target, name, SkillFields.Get(source, name));
            }

            foreach (string name in SkillFields.Names)
            {
                Assert.That(SkillFields.Get(target, name), Is.EqualTo(SkillFields.Get(source, name)),
                    name);
            }
        }

        [Test]
        public void EveryField_ExistsInTheViewsUxml_AsAFieldTheViewCanShow()
        {
            var vta = Resources.Load<VisualTreeAsset>("Editor/Uxml/SkillView");
            Assert.That(vta, Is.Not.Null, "SkillView.uxml should load from Resources");
            var root = vta.Instantiate();

            var skill = new Skill();
            foreach (string name in SkillFields.Names)
            {
                var field = root.Q(name);
                Assert.That(field, Is.Not.Null, $"{name} is missing from SkillView.uxml");

                // Throws or logs a warning if the field is of a type that can't show it
                UitkFieldValues.SetWithoutNotify(field, SkillFields.Get(skill, name));
                LogAssert.NoUnexpectedReceived();
            }
        }

        [Test]
        public void SkillCopy_HasTheSameValuesButIsAnotherObject()
        {
            var original = new Skill
            {
                skillName = "Copied",
                variance = 5,
                weaponType2 = "Bow",
                note = "n",
            };

            var copy = new Skill(original);

            Assert.That(copy, Is.Not.SameAs(original));
            foreach (string name in SkillFields.Names)
            {
                Assert.That(SkillFields.Get(copy, name), Is.EqualTo(SkillFields.Get(original, name)),
                    name);
            }
        }

        [Test]
        public void UitkFieldValues_SetsEachKindOfFieldWithoutRaisingAChangeEvent()
        {
            var fields = new List<VisualElement>
            {
                new TextField(), new IntegerField(), new Toggle(), new EnumField(SkillScope.None),
                new DropdownField(new List<string> { "a", "b" }, 0), new ObjectField(),
            };
            var values = new object[] { "x", 3, true, SkillScope.User, "b", null };
            int changes = 0;

            for (int i = 0; i < fields.Count; i++)
            {
                fields[i].RegisterCallback<ChangeEvent<string>>(_ => changes++);
                fields[i].RegisterCallback<ChangeEvent<int>>(_ => changes++);
                fields[i].RegisterCallback<ChangeEvent<bool>>(_ => changes++);
                fields[i].RegisterCallback<ChangeEvent<System.Enum>>(_ => changes++);
                UitkFieldValues.SetWithoutNotify(fields[i], values[i]);
            }

            Assert.That(changes, Is.EqualTo(0));
            Assert.That(((TextField)fields[0]).value, Is.EqualTo("x"));
            Assert.That(((IntegerField)fields[1]).value, Is.EqualTo(3));
            Assert.That(((Toggle)fields[2]).value, Is.True);
            Assert.That(((EnumField)fields[3]).value, Is.EqualTo(SkillScope.User));
            Assert.That(((DropdownField)fields[4]).value, Is.EqualTo("b"));
        }
    }
}

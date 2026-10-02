using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AtMycelia.Myceliarium.Tests
{
    public class ControlPanelEntryRegistryTests
    {
        [SetUp]
        public void SetUp()
        {
            ControlPanelEntryRegistry.InTestMode = true;
        }

        [TearDown]
        public void TearDown()
        {
            ControlPanelEntryRegistry.ResetForTests();

            // The registry is static/global, shared with the live editor session.
            // Since EditMode tests don't necessarily trigger an assembly reload,
            // leaving it cleared here would make real tabs (e.g. in
            // AtMyceliaControlPanel) disappear after the test run. Restore it to
            // reflect the actual project types so the live editor keeps working.
            ControlPanelEntryRegistry.RefreshRegistry();
            ControlPanelEntryRegistry.CreateAllEntries();
        }

        #region FilterEntryTypes
        [Test]
        public void FilterEntryTypes_ExcludesAbstractTypes()
        {
            var candidates = new[] { typeof(AbstractEntry), typeof(ConcreteEntryA) };

            var result = ControlPanelEntryRegistry.FilterEntryTypes(candidates);

            Assert.That(result.Contains(typeof(AbstractEntry)), Is.False);
            Assert.That(result.Contains(typeof(ConcreteEntryA)), Is.True);
        }

        [Test]
        public void FilterEntryTypes_ExcludesInterfaceTypes()
        {
            var candidates = new[] { typeof(IControlPanelEntry), typeof(ConcreteEntryA) };

            var result = ControlPanelEntryRegistry.FilterEntryTypes(candidates);

            Assert.That(result.Contains(typeof(IControlPanelEntry)), Is.False);
            Assert.That(result.Contains(typeof(ConcreteEntryA)), Is.True);
        }

        [Test]
        public void FilterEntryTypes_ExcludesTypesNotImplementingIControlPanelEntry()
        {
            var candidates = new[] { typeof(string), typeof(ConcreteEntryA) };

            var result = ControlPanelEntryRegistry.FilterEntryTypes(candidates);

            Assert.That(result.Contains(typeof(string)), Is.False);
            Assert.That(result.Contains(typeof(ConcreteEntryA)), Is.True);
        }

        [Test]
        public void FilterEntryTypes_IgnoresNullEntriesInCandidateList()
        {
            var candidates = new[] { null, typeof(ConcreteEntryA) };

            var result = ControlPanelEntryRegistry.FilterEntryTypes(candidates);

            Assert.That(result.Contains(typeof(ConcreteEntryA)), Is.True);
            Assert.That(result.Length, Is.EqualTo(1));
        }

        [Test]
        public void FilterEntryTypes_NullCandidates_ReturnsEmptyArray()
        {
            var result = ControlPanelEntryRegistry.FilterEntryTypes(null);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }
        #endregion

        #region RefreshRegistry / AllEntryTypes
        [Test]
        public void RefreshRegistry_WithCustomProvider_PopulatesAllEntryTypesAfterFiltering()
        {
            ControlPanelEntryRegistry.RefreshRegistry(() => new[]
            {
                typeof(ConcreteEntryA),
                typeof(AbstractEntry),
                typeof(string)
            });

            var allTypes = ControlPanelEntryRegistry.AllEntryTypes.ToArray();

            Assert.That(allTypes, Is.EquivalentTo(new[] { typeof(ConcreteEntryA) }));
        }

        [Test]
        public void RefreshRegistry_CalledTwice_ReplacesPreviousResults()
        {
            ControlPanelEntryRegistry.RefreshRegistry(() => new[] { typeof(ConcreteEntryA) });
            ControlPanelEntryRegistry.RefreshRegistry(() => new[] { typeof(ConcreteEntryB) });

            var allTypes = ControlPanelEntryRegistry.AllEntryTypes.ToArray();

            Assert.That(allTypes, Is.EquivalentTo(new[] { typeof(ConcreteEntryB) }));
        }
        #endregion

        #region CreateAllEntries
        [Test]
        public void CreateAllEntries_UsesDefaultActivatorWhenNoFactoryProvided()
        {
            ControlPanelEntryRegistry.RefreshRegistry(() => new[] { typeof(ConcreteEntryA) });

            ControlPanelEntryRegistry.CreateAllEntries();

            Assert.That(ControlPanelEntryRegistry.Entries.Count, Is.EqualTo(1));
            Assert.That(ControlPanelEntryRegistry.Entries[0], Is.InstanceOf<ConcreteEntryA>());
        }

        [Test]
        public void CreateAllEntries_WithCustomFactory_UsesFactoryInstances()
        {
            ControlPanelEntryRegistry.RefreshRegistry(() => new[] { typeof(ConcreteEntryA) });

            var injected = new ConcreteEntryA();
            ControlPanelEntryRegistry.CreateAllEntries(type => injected);

            Assert.That(ControlPanelEntryRegistry.Entries.Count, Is.EqualTo(1));
            Assert.That(ControlPanelEntryRegistry.Entries[0], Is.SameAs(injected));
        }

        [Test]
        public void CreateAllEntries_FactoryThrows_SkipsThatTypeAndContinues()
        {
            ControlPanelEntryRegistry.RefreshRegistry(() => new[]
            {
                typeof(ConcreteEntryA),
                typeof(ConcreteEntryB)
            });

            // CreateAllEntries intentionally logs an error when a factory throws
            // for a given type, so we need to tell Unity's test framework that
            // this specific error log is expected; otherwise it treats any
            // unhandled Debug.LogError as a test failure.
            LogAssert.Expect(LogType.Error,
                $"Failed to create instance of {nameof(ConcreteEntryA)}: Simulated failure");

            ControlPanelEntryRegistry.CreateAllEntries(type =>
            {
                if (type == typeof(ConcreteEntryA))
                {
                    throw new InvalidOperationException("Simulated failure");
                }
                return new ConcreteEntryB();
            });

            Assert.That(ControlPanelEntryRegistry.Entries.Count, Is.EqualTo(1));
            Assert.That(ControlPanelEntryRegistry.Entries[0], Is.InstanceOf<ConcreteEntryB>());
        }

        [Test]
        public void CreateAllEntries_FactoryReturnsNull_IsNotAdded()
        {
            ControlPanelEntryRegistry.RefreshRegistry(() => new[] { typeof(ConcreteEntryA) });

            ControlPanelEntryRegistry.CreateAllEntries(type => null);

            Assert.That(ControlPanelEntryRegistry.Entries.Count, Is.EqualTo(0));
        }

        [Test]
        public void CreateAllEntries_CalledTwice_ClearsPreviousCache()
        {
            ControlPanelEntryRegistry.RefreshRegistry(() => new[] { typeof(ConcreteEntryA) });
            ControlPanelEntryRegistry.CreateAllEntries();
            Assert.That(ControlPanelEntryRegistry.Entries.Count, Is.EqualTo(1));

            ControlPanelEntryRegistry.RefreshRegistry(() => new[] { typeof(ConcreteEntryB) });
            ControlPanelEntryRegistry.CreateAllEntries();

            Assert.That(ControlPanelEntryRegistry.Entries.Count, Is.EqualTo(1));
            Assert.That(ControlPanelEntryRegistry.Entries[0], Is.InstanceOf<ConcreteEntryB>());
        }
        #endregion

        #region GetEntriesOfType
        [Test]
        public void GetEntriesOfType_Generic_ReturnsCorrectlyCastInstances()
        {
            ControlPanelEntryRegistry.RefreshRegistry(() => new[]
            {
                typeof(ConcreteEntryA),
                typeof(ConcreteEntryB)
            });
            ControlPanelEntryRegistry.CreateAllEntries();

            var result = ControlPanelEntryRegistry.GetEntriesOfType<ConcreteEntryA>();

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0], Is.InstanceOf<ConcreteEntryA>());
        }

        [Test]
        public void GetEntriesOfType_Generic_WithInterfaceFilter_ReturnsAllMatchingImplementations()
        {
            ControlPanelEntryRegistry.RefreshRegistry(() => new[]
            {
                typeof(ConcreteEntryA),
                typeof(ConcreteEntryB)
            });
            ControlPanelEntryRegistry.CreateAllEntries();

            var result = ControlPanelEntryRegistry.GetEntriesOfType<IControlPanelEntry>();

            Assert.That(result.Count, Is.EqualTo(2));
        }

        [Test]
        public void GetEntriesOfType_NonGeneric_FiltersByAssignability()
        {
            ControlPanelEntryRegistry.RefreshRegistry(() => new[]
            {
                typeof(ConcreteEntryA),
                typeof(ConcreteEntryB)
            });
            ControlPanelEntryRegistry.CreateAllEntries();

            var result = ControlPanelEntryRegistry.GetEntriesOfType(typeof(ConcreteEntryB));

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0], Is.InstanceOf<ConcreteEntryB>());
        }

        [Test]
        public void GetEntriesOfType_NoMatches_ReturnsEmptyList()
        {
            ControlPanelEntryRegistry.RefreshRegistry(() => new[] { typeof(ConcreteEntryA) });
            ControlPanelEntryRegistry.CreateAllEntries();

            var result = ControlPanelEntryRegistry.GetEntriesOfType(typeof(ConcreteEntryB));

            Assert.That(result, Is.Empty);
        }
        #endregion

        #region Test Doubles
        private abstract class AbstractEntry : IControlPanelEntry
        {
            public virtual bool IsTestOnly => true;
            public int SortingOrder => 0;
            public string SortingName => string.Empty;
            public IControlPanelTab Tab => null;
            public IControlPanelSubwindow Subwindow => null;
            public bool IsTopLevel => false;
            public bool IsMeantToHaveSubwindow => false;
            public bool IsInitted => false;
            public bool HasSubentries => false;

            public void Init(bool forceReinit = false) { }
            public IReadOnlyList<IControlPanelEntry> GetSubentries(bool recursive = false) =>
                Array.Empty<IControlPanelEntry>();
            public void RemoveFromHierarchy() { }
            public void OnSelected() { }
            public void OnDeselected() { }
        }

        private class ConcreteEntryA : IControlPanelEntry
        {
            public virtual bool IsTestOnly => true;
            public int SortingOrder => 0;
            public string SortingName => nameof(ConcreteEntryA);
            public IControlPanelTab Tab => null;
            public IControlPanelSubwindow Subwindow => null;
            public bool IsTopLevel => true;
            public bool IsMeantToHaveSubwindow => false;
            public bool IsInitted => false;
            public bool HasSubentries => false;

            public void Init(bool forceReinit = false) { }
            public IReadOnlyList<IControlPanelEntry> GetSubentries(bool recursive = false) =>
                Array.Empty<IControlPanelEntry>();
            public void RemoveFromHierarchy() { }
            public void OnSelected() { }
            public void OnDeselected() { }
        }

        private class ConcreteEntryB : IControlPanelEntry
        {
            public virtual bool IsTestOnly => true;
            public int SortingOrder => 0;
            public string SortingName => nameof(ConcreteEntryB);
            public IControlPanelTab Tab => null;
            public IControlPanelSubwindow Subwindow => null;
            public bool IsTopLevel => false;
            public bool IsMeantToHaveSubwindow => false;
            public bool IsInitted => false;
            public bool HasSubentries => false;

            public void Init(bool forceReinit = false) { }
            public IReadOnlyList<IControlPanelEntry> GetSubentries(bool recursive = false) =>
                Array.Empty<IControlPanelEntry>();
            public void RemoveFromHierarchy() { }
            public void OnSelected() { }
            public void OnDeselected() { }
        }
        #endregion
    }
}

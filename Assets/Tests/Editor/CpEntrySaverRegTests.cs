using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AtMycelia.Myceliarium.Tests
{
    public class CpEntrySaverRegTests
    {
        [TearDown]
        public void TearDown()
        {
            CpEntrySaverReg.ResetForTests();
        }

        #region FilterSaverTypes
        [Test]
        public void FilterSaverTypes_ExcludesAbstractTypes()
        {
            var candidates = new[] { typeof(AbstractSaver), typeof(FakeSaverA) };

            var result = CpEntrySaverReg.FilterSaverTypes(candidates);

            Assert.That(result.Contains(typeof(AbstractSaver)), Is.False);
            Assert.That(result.Contains(typeof(FakeSaverA)), Is.True);
        }

        [Test]
        public void FilterSaverTypes_ExcludesInterfaceTypes()
        {
            var candidates = new[] { typeof(IControlPanelEntrySaver), typeof(FakeSaverA) };

            var result = CpEntrySaverReg.FilterSaverTypes(candidates);

            Assert.That(result.Contains(typeof(IControlPanelEntrySaver)), Is.False);
            Assert.That(result.Contains(typeof(FakeSaverA)), Is.True);
        }

        [Test]
        public void FilterSaverTypes_ExcludesTypesNotImplementingIControlPanelEntrySaver()
        {
            var candidates = new[] { typeof(string), typeof(FakeSaverA) };

            var result = CpEntrySaverReg.FilterSaverTypes(candidates);

            Assert.That(result.Contains(typeof(string)), Is.False);
            Assert.That(result.Contains(typeof(FakeSaverA)), Is.True);
        }

        [Test]
        public void FilterSaverTypes_IgnoresNullEntriesInCandidateList()
        {
            var candidates = new[] { null, typeof(FakeSaverA) };

            var result = CpEntrySaverReg.FilterSaverTypes(candidates);

            Assert.That(result.Contains(typeof(FakeSaverA)), Is.True);
            Assert.That(result.Length, Is.EqualTo(1));
        }

        [Test]
        public void FilterSaverTypes_NullCandidates_ReturnsEmptyArray()
        {
            var result = CpEntrySaverReg.FilterSaverTypes(null);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }
        #endregion

        #region RefreshRegistry / AllSaverTypes
        [Test]
        public void RefreshRegistry_WithCustomProvider_PopulatesAllSaverTypesAfterFiltering()
        {
            CpEntrySaverReg.RefreshRegistry(() => new[]
            {
                typeof(FakeSaverA),
                typeof(AbstractSaver),
                typeof(string)
            });

            var allTypes = CpEntrySaverReg.AllSaverTypes.ToArray();

            Assert.That(allTypes, Is.EquivalentTo(new[] { typeof(FakeSaverA) }));
        }

        [Test]
        public void RefreshRegistry_CalledTwice_ReplacesPreviousResults()
        {
            CpEntrySaverReg.RefreshRegistry(() => new[] { typeof(FakeSaverA) });
            CpEntrySaverReg.RefreshRegistry(() => new[] { typeof(FakeSaverB) });

            var allTypes = CpEntrySaverReg.AllSaverTypes.ToArray();

            Assert.That(allTypes, Is.EquivalentTo(new[] { typeof(FakeSaverB) }));
        }
        #endregion

        #region CreateAllSavers
        [Test]
        public void CreateAllSavers_UsesDefaultActivatorWhenNoFactoryProvided()
        {
            CpEntrySaverReg.RefreshRegistry(() => new[] { typeof(FakeSaverA) });

            CpEntrySaverReg.CreateAllSavers();

            Assert.That(CpEntrySaverReg.Savers.Count, Is.EqualTo(1));
            Assert.That(CpEntrySaverReg.Savers[0], Is.InstanceOf<FakeSaverA>());
        }

        [Test]
        public void CreateAllSavers_WithCustomFactory_UsesFactoryInstances()
        {
            CpEntrySaverReg.RefreshRegistry(() => new[] { typeof(FakeSaverA) });

            var injected = new FakeSaverA();
            CpEntrySaverReg.CreateAllSavers(type => injected);

            Assert.That(CpEntrySaverReg.Savers.Count, Is.EqualTo(1));
            Assert.That(CpEntrySaverReg.Savers[0], Is.SameAs(injected));
        }

        [Test]
        public void CreateAllSavers_FactoryThrows_SkipsThatTypeAndContinues()
        {
            CpEntrySaverReg.RefreshRegistry(() => new[]
            {
                typeof(FakeSaverA),
                typeof(FakeSaverB)
            });

            LogAssert.Expect(LogType.Error,
                $"Failed to create instance of {nameof(FakeSaverA)}: Simulated failure");

            CpEntrySaverReg.CreateAllSavers(type =>
            {
                if (type == typeof(FakeSaverA))
                {
                    throw new InvalidOperationException("Simulated failure");
                }
                return new FakeSaverB();
            });

            Assert.That(CpEntrySaverReg.Savers.Count, Is.EqualTo(1));
            Assert.That(CpEntrySaverReg.Savers[0], Is.InstanceOf<FakeSaverB>());
        }

        [Test]
        public void CreateAllSavers_FactoryReturnsNull_IsNotAdded()
        {
            CpEntrySaverReg.RefreshRegistry(() => new[] { typeof(FakeSaverA) });

            CpEntrySaverReg.CreateAllSavers(type => null);

            Assert.That(CpEntrySaverReg.Savers.Count, Is.EqualTo(0));
        }

        [Test]
        public void CreateAllSavers_CalledTwice_ClearsPreviousCache()
        {
            CpEntrySaverReg.RefreshRegistry(() => new[] { typeof(FakeSaverA) });
            CpEntrySaverReg.CreateAllSavers();
            Assert.That(CpEntrySaverReg.Savers.Count, Is.EqualTo(1));

            CpEntrySaverReg.RefreshRegistry(() => new[] { typeof(FakeSaverB) });
            CpEntrySaverReg.CreateAllSavers();

            Assert.That(CpEntrySaverReg.Savers.Count, Is.EqualTo(1));
            Assert.That(CpEntrySaverReg.Savers[0], Is.InstanceOf<FakeSaverB>());
        }
        #endregion

        #region GetSaversOfType
        [Test]
        public void GetSaversOfType_Generic_ReturnsCorrectlyCastInstances()
        {
            CpEntrySaverReg.RefreshRegistry(() => new[]
            {
                typeof(FakeSaverA),
                typeof(FakeSaverB)
            });
            CpEntrySaverReg.CreateAllSavers();

            var result = CpEntrySaverReg.GetSaversOfType<FakeSaverA>();

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0], Is.InstanceOf<FakeSaverA>());
        }

        [Test]
        public void GetSaversOfType_Generic_WithInterfaceFilter_ReturnsAllMatchingImplementations()
        {
            CpEntrySaverReg.RefreshRegistry(() => new[]
            {
                typeof(FakeAtMyceliaSaver),
                typeof(FakeSaverA)
            });
            CpEntrySaverReg.CreateAllSavers();

            var result = CpEntrySaverReg.GetSaversOfType<IControlPanelEntrySaver>();

            Assert.That(result.Count, Is.EqualTo(2));
        }

        [Test]
        public void GetSaversOfType_NonGeneric_FiltersByAssignability()
        {
            CpEntrySaverReg.RefreshRegistry(() => new[]
            {
                typeof(FakeAtMyceliaSaver),
                typeof(FakeSaverA)
            });
            CpEntrySaverReg.CreateAllSavers();

            var result = CpEntrySaverReg.GetSaversOfType(typeof(IAtMyceliaControlPanelEntrySaver));

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0], Is.InstanceOf<FakeAtMyceliaSaver>());
        }

        [Test]
        public void GetSaversOfType_NoMatches_ReturnsEmptyList()
        {
            CpEntrySaverReg.RefreshRegistry(() => new[] { typeof(FakeSaverA) });
            CpEntrySaverReg.CreateAllSavers();

            var result = CpEntrySaverReg.GetSaversOfType<FakeSaverB>();

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetSaversOfType_EmptyRegistry_ReturnsEmptyList()
        {
            var result = CpEntrySaverReg.GetSaversOfType<FakeSaverA>();

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }
        #endregion

        #region Test Doubles
        private abstract class AbstractSaver : IControlPanelEntrySaver
        {
            public abstract void Save(IControlPanelEntry toSaveFor, Action onComplete = null);
            public abstract bool IsCompatibleWith(IControlPanelEntry toSaveFor);
        }

        private class FakeSaverA : IControlPanelEntrySaver
        {
            public void Save(IControlPanelEntry toSaveFor, Action onComplete = null) { }
            public bool IsCompatibleWith(IControlPanelEntry toSaveFor) => true;
        }

        private class FakeSaverB : IControlPanelEntrySaver
        {
            public void Save(IControlPanelEntry toSaveFor, Action onComplete = null) { }
            public bool IsCompatibleWith(IControlPanelEntry toSaveFor) => true;
        }

        private class FakeAtMyceliaSaver : IAtMyceliaControlPanelEntrySaver
        {
            public void Save(IControlPanelEntry toSaveFor, Action onComplete = null) { }
            public bool IsCompatibleWith(IControlPanelEntry toSaveFor) => true;
        }
        #endregion
    }
}

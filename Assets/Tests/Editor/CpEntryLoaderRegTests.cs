using System;
using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AtMycelia.Myceliarium.Tests
{
    public class CpEntryLoaderRegTests
    {
        [SetUp]
        public void SetUp()
        {
            CpEntryLoaderReg.ResetForTests();//
        }

        [TearDown]
        public void TearDown()
        {
            CpEntryLoaderReg.ResetForTests();

            // The registry is static/global, shared with the live editor session.
            // Since EditMode tests don't necessarily trigger an assembly reload,
            // leaving it cleared here would make real loaders disappear after the
            // test run. Restore it to reflect the actual project types so the
            // live editor keeps working.
            CpEntryLoaderReg.RefreshRegistry();
            CpEntryLoaderReg.CreateAllLoaders();
        }

        #region FilterLoaderTypes
        [Test]
        public void FilterLoaderTypes_ExcludesAbstractTypes()
        {
            var candidates = new[] { typeof(AbstractLoader), typeof(FakeLoaderA) };

            var result = CpEntryLoaderReg.FilterLoaderTypes(candidates);

            Assert.That(result.Contains(typeof(AbstractLoader)), Is.False);
            Assert.That(result.Contains(typeof(FakeLoaderA)), Is.True);
        }

        [Test]
        public void FilterLoaderTypes_ExcludesInterfaceTypes()
        {
            var candidates = new[] { typeof(IControlPanelEntryLoader), typeof(FakeLoaderA) };

            var result = CpEntryLoaderReg.FilterLoaderTypes(candidates);

            Assert.That(result.Contains(typeof(IControlPanelEntryLoader)), Is.False);
            Assert.That(result.Contains(typeof(FakeLoaderA)), Is.True);
        }

        [Test]
        public void FilterLoaderTypes_ExcludesTypesNotImplementingIControlPanelEntryLoader()
        {
            var candidates = new[] { typeof(string), typeof(FakeLoaderA) };

            var result = CpEntryLoaderReg.FilterLoaderTypes(candidates);

            Assert.That(result.Contains(typeof(string)), Is.False);
            Assert.That(result.Contains(typeof(FakeLoaderA)), Is.True);
        }

        [Test]
        public void FilterLoaderTypes_IgnoresNullEntriesInCandidateList()
        {
            var candidates = new[] { null, typeof(FakeLoaderA) };

            var result = CpEntryLoaderReg.FilterLoaderTypes(candidates);

            Assert.That(result.Contains(typeof(FakeLoaderA)), Is.True);
            Assert.That(result.Length, Is.EqualTo(1));
        }

        [Test]
        public void FilterLoaderTypes_NullCandidates_ReturnsEmptyArray()
        {
            var result = CpEntryLoaderReg.FilterLoaderTypes(null);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }
        #endregion

        #region RefreshRegistry / AllLoaderTypes
        [Test]
        public void RefreshRegistry_WithCustomProvider_PopulatesAllLoaderTypesAfterFiltering()
        {
            CpEntryLoaderReg.RefreshRegistry(() => new[]
            {
                typeof(FakeLoaderA),
                typeof(AbstractLoader),
                typeof(string)
            });

            var allTypes = CpEntryLoaderReg.AllLoaderTypes.ToArray();

            Assert.That(allTypes, Is.EquivalentTo(new[] { typeof(FakeLoaderA) }));
        }

        [Test]
        public void RefreshRegistry_CalledTwice_ReplacesPreviousResults()
        {
            CpEntryLoaderReg.RefreshRegistry(() => new[] { typeof(FakeLoaderA) });
            CpEntryLoaderReg.RefreshRegistry(() => new[] { typeof(FakeLoaderB) });

            var allTypes = CpEntryLoaderReg.AllLoaderTypes.ToArray();

            Assert.That(allTypes, Is.EquivalentTo(new[] { typeof(FakeLoaderB) }));
        }
        #endregion

        #region CreateAllLoaders
        [Test]
        public void CreateAllLoaders_UsesDefaultActivatorWhenNoFactoryProvided()
        {
            CpEntryLoaderReg.RefreshRegistry(() => new[] { typeof(FakeLoaderA) });

            CpEntryLoaderReg.CreateAllLoaders();

            Assert.That(CpEntryLoaderReg.Loaders.Count, Is.EqualTo(1));
            Assert.That(CpEntryLoaderReg.Loaders[0], Is.InstanceOf<FakeLoaderA>());
        }

        [Test]
        public void CreateAllLoaders_WithCustomFactory_UsesFactoryInstances()
        {
            CpEntryLoaderReg.RefreshRegistry(() => new[] { typeof(FakeLoaderA) });

            var injected = new FakeLoaderA();
            CpEntryLoaderReg.CreateAllLoaders(type => injected);

            Assert.That(CpEntryLoaderReg.Loaders.Count, Is.EqualTo(1));
            Assert.That(CpEntryLoaderReg.Loaders[0], Is.SameAs(injected));
        }

        [Test]
        public void CreateAllLoaders_FactoryThrows_SkipsThatTypeAndContinues()
        {
            CpEntryLoaderReg.RefreshRegistry(() => new[]
            {
                typeof(FakeLoaderA),
                typeof(FakeLoaderB)
            });

            LogAssert.Expect(LogType.Error,
                $"Failed to create instance of {nameof(FakeLoaderA)}: Simulated failure");

            CpEntryLoaderReg.CreateAllLoaders(type =>
            {
                if (type == typeof(FakeLoaderA))
                {
                    throw new InvalidOperationException("Simulated failure");
                }
                return new FakeLoaderB();
            });

            Assert.That(CpEntryLoaderReg.Loaders.Count, Is.EqualTo(1));
            Assert.That(CpEntryLoaderReg.Loaders[0], Is.InstanceOf<FakeLoaderB>());
        }

        [Test]
        public void CreateAllLoaders_FactoryReturnsNull_IsNotAdded()
        {
            CpEntryLoaderReg.RefreshRegistry(() => new[] { typeof(FakeLoaderA) });

            CpEntryLoaderReg.CreateAllLoaders(type => null);

            Assert.That(CpEntryLoaderReg.Loaders.Count, Is.EqualTo(0));
        }

        [Test]
        public void CreateAllLoaders_CalledTwice_ClearsPreviousCache()
        {
            CpEntryLoaderReg.RefreshRegistry(() => new[] { typeof(FakeLoaderA) });
            CpEntryLoaderReg.CreateAllLoaders();
            Assert.That(CpEntryLoaderReg.Loaders.Count, Is.EqualTo(1));

            CpEntryLoaderReg.RefreshRegistry(() => new[] { typeof(FakeLoaderB) });
            CpEntryLoaderReg.CreateAllLoaders();

            Assert.That(CpEntryLoaderReg.Loaders.Count, Is.EqualTo(1));
            Assert.That(CpEntryLoaderReg.Loaders[0], Is.InstanceOf<FakeLoaderB>());
        }
        #endregion

        #region GetLoadersOfType
        [Test]
        public void GetLoadersOfType_Generic_ReturnsCorrectlyCastInstances()
        {
            CpEntryLoaderReg.RefreshRegistry(() => new[]
            {
                typeof(FakeLoaderA),
                typeof(FakeLoaderB)
            });
            CpEntryLoaderReg.CreateAllLoaders();

            var result = CpEntryLoaderReg.GetLoadersOfType<FakeLoaderA>();

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0], Is.InstanceOf<FakeLoaderA>());
        }

        [Test]
        public void GetLoadersOfType_Generic_WithInterfaceFilter_ReturnsAllMatchingImplementations()
        {
            CpEntryLoaderReg.RefreshRegistry(() => new[]
            {
                typeof(FakeAtMyceliaLoader),
                typeof(FakeLoaderA)
            });
            CpEntryLoaderReg.CreateAllLoaders();

            var result = CpEntryLoaderReg.GetLoadersOfType<IControlPanelEntryLoader>();

            Assert.That(result.Count, Is.EqualTo(2));
        }

        [Test]
        public void GetLoadersOfType_NonGeneric_FiltersByAssignability()
        {
            CpEntryLoaderReg.RefreshRegistry(() => new[]
            {
                typeof(FakeAtMyceliaLoader),
                typeof(FakeLoaderA)
            });
            CpEntryLoaderReg.CreateAllLoaders();

            var result = CpEntryLoaderReg.GetLoadersOfType(typeof(IAtMyceliaControlPanelEntryLoader));

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0], Is.InstanceOf<FakeAtMyceliaLoader>());
        }

        [Test]
        public void GetLoadersOfType_NoMatches_ReturnsEmptyList()
        {
            CpEntryLoaderReg.RefreshRegistry(() => new[] { typeof(FakeLoaderA) });
            CpEntryLoaderReg.CreateAllLoaders();

            var result = CpEntryLoaderReg.GetLoadersOfType<FakeLoaderB>();

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetLoadersOfType_EmptyRegistry_ReturnsEmptyList()
        {
            var result = CpEntryLoaderReg.GetLoadersOfType<FakeLoaderA>();

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }
        #endregion

        #region Test Doubles
        private abstract class AbstractLoader : IControlPanelEntryLoader
        {
            public abstract void Load(IControlPanelEntry toLoadFor, ref object loadResult, Action onComplete = null);
            public abstract bool IsCompatibleWith(IControlPanelEntry toLoadFor);
        }

        private class FakeLoaderA : IControlPanelEntryLoader
        {
            public void Load(IControlPanelEntry toLoadFor, ref object loadResult, Action onComplete = null)
            {
                onComplete?.Invoke();
            }
            public bool IsCompatibleWith(IControlPanelEntry toLoadFor) => true;
        }

        private class FakeLoaderB : IControlPanelEntryLoader
        {
            public void Load(IControlPanelEntry toLoadFor, ref object loadResult, Action onComplete = null)
            {
                onComplete?.Invoke();
            }
            public bool IsCompatibleWith(IControlPanelEntry toLoadFor) => true;
        }

        private class FakeAtMyceliaLoader : IAtMyceliaControlPanelEntryLoader
        {
            public void Load(IControlPanelEntry toLoadFor, ref object loadResult, Action onComplete = null)
            {
                onComplete?.Invoke();
            }
            public bool IsCompatibleWith(IControlPanelEntry toLoadFor) => true;
        }
        #endregion
    }
}

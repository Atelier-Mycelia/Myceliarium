using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AtMycelia.Myceliarium.Tests
{
    public class TypeRegistryTests
    {
        private static Type[] AllCandidates => new[]
        {
            typeof(ThingA), typeof(ThingB), typeof(AbstractThing), typeof(IThing),
            typeof(string), null
        };

        #region FilterTypes
        [Test]
        public void FilterTypes_KeepsOnlyConcreteImplementations()
        {
            var result = TypeRegistry<IThing>.FilterTypes(AllCandidates);

            Assert.That(result, Is.EquivalentTo(new[] { typeof(ThingA), typeof(ThingB) }));
        }

        [Test]
        public void FilterTypes_NullCandidates_ReturnsEmpty()
        {
            Assert.That(TypeRegistry<IThing>.FilterTypes(null), Is.Empty);
        }

        [Test]
        public void FilterTypes_AppliesTheExtraFilter()
        {
            var result = TypeRegistry<IThing>.FilterTypes(AllCandidates,
                type => type == typeof(ThingB));

            Assert.That(result, Is.EqualTo(new[] { typeof(ThingB) }));
        }
        #endregion

        #region Refresh / Types
        [Test]
        public void Refresh_WithProvider_PopulatesTypes()
        {
            var registry = new TypeRegistry<IThing>();

            registry.Refresh(() => AllCandidates);

            Assert.That(registry.Types, Is.EquivalentTo(new[] { typeof(ThingA), typeof(ThingB) }));
        }

        [Test]
        public void Refresh_CalledTwice_ReplacesPreviousTypes()
        {
            var registry = new TypeRegistry<IThing>();
            registry.Refresh(() => new[] { typeof(ThingA) });

            registry.Refresh(() => new[] { typeof(ThingB) });

            Assert.That(registry.Types, Is.EqualTo(new[] { typeof(ThingB) }));
        }

        [Test]
        public void Types_ReturnsACopy()
        {
            var registry = new TypeRegistry<IThing>();
            registry.Refresh(() => new[] { typeof(ThingA) });

            registry.Types[0] = typeof(ThingB);

            Assert.That(registry.Types, Is.EqualTo(new[] { typeof(ThingA) }));
        }

        [Test]
        public void Types_NeverRefreshed_IsEmptyUnlessRefreshOnFirstUse()
        {
            Assert.That(new TypeRegistry<IThing>().Types, Is.Empty);
        }

        [Test]
        public void Types_RefreshOnFirstUse_FindsTypesThroughTypeCache()
        {
            var registry = new TypeRegistry<IThing>(refreshOnFirstUse: true);

            Assert.That(registry.Types, Does.Contain(typeof(ThingA)));
        }
        #endregion

        #region CreateAll / Instances
        [Test]
        public void CreateAll_CreatesAnInstanceOfEachType()
        {
            var registry = RegistryWith(typeof(ThingA), typeof(ThingB));

            var result = registry.CreateAll();

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(registry.Instances, Is.EqualTo(result));
        }

        [Test]
        public void CreateAll_CalledTwice_ReplacesPreviousInstances()
        {
            var registry = RegistryWith(typeof(ThingA));
            registry.CreateAll();

            registry.CreateAll();

            Assert.That(registry.Instances.Count, Is.EqualTo(1));
        }

        [Test]
        public void CreateAll_UsesTheFactoryWhenGiven()
        {
            var registry = RegistryWith(typeof(ThingA));
            var injected = new ThingA();

            registry.CreateAll(type => injected);

            Assert.That(registry.Instances[0], Is.SameAs(injected));
        }

        [Test]
        public void CreateAll_FactoryReturnsNull_IsSkipped()
        {
            var registry = RegistryWith(typeof(ThingA));

            registry.CreateAll(type => null);

            Assert.That(registry.Instances, Is.Empty);
        }

        [Test]
        public void CreateAll_FactoryThrows_LogsAndKeepsGoing()
        {
            var registry = RegistryWith(typeof(ThingA), typeof(ThingB));
            LogAssert.Expect(LogType.Error,
                new System.Text.RegularExpressions.Regex(".*Failed to create instance of ThingA.*"));

            registry.CreateAll(type =>
            {
                if (type == typeof(ThingA)) throw new InvalidOperationException("nope");
                return new ThingB();
            });

            Assert.That(registry.Instances.Count, Is.EqualTo(1));
            Assert.That(registry.Instances[0], Is.InstanceOf<ThingB>());
        }

        [Test]
        public void CreateAll_InstanceFilterDropsRejectedInstances()
        {
            var registry = new TypeRegistry<IThing>(instanceFilter: thing => thing is ThingB);
            registry.Refresh(() => new[] { typeof(ThingA), typeof(ThingB) });

            registry.CreateAll();

            Assert.That(registry.Instances.Count, Is.EqualTo(1));
            Assert.That(registry.Instances[0], Is.InstanceOf<ThingB>());
        }
        #endregion

        #region GetInstancesOfType
        [Test]
        public void GetInstancesOfType_FiltersByAssignability()
        {
            var registry = RegistryWith(typeof(ThingA), typeof(ThingB));
            registry.CreateAll();

            Assert.That(registry.GetInstancesOfType(typeof(ThingA)).Count, Is.EqualTo(1));
            Assert.That(registry.GetInstancesOfType(typeof(IThing)).Count, Is.EqualTo(2));
        }

        [Test]
        public void GetInstancesOfType_Generic_ReturnsCorrectlyCastInstances()
        {
            var registry = RegistryWith(typeof(ThingA), typeof(ThingB));
            registry.CreateAll();

            IList<ThingA> result = registry.GetInstancesOfType<ThingA>();

            Assert.That(result.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetInstancesOfType_NoInstances_ReturnsEmpty()
        {
            Assert.That(new TypeRegistry<IThing>().GetInstancesOfType(typeof(IThing)), Is.Empty);
        }
        #endregion

        #region CreateOneOfType
        [Test]
        public void CreateOneOfType_ReturnsFreshInstanceEachTimeWithoutCachingIt()
        {
            var registry = RegistryWith(typeof(ThingA));

            var first = registry.CreateOneOfType(typeof(IThing));
            var second = registry.CreateOneOfType(typeof(IThing));

            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(registry.Instances, Is.Empty);
        }

        [Test]
        public void CreateOneOfType_NoMatch_ReturnsNull()
        {
            var registry = RegistryWith(typeof(ThingA));

            Assert.That(registry.CreateOneOfType(typeof(ThingB)), Is.Null);
        }

        [Test]
        public void CreateOneOfType_MultipleMatches_UsesFirstAndWarns()
        {
            var registry = RegistryWith(typeof(ThingA), typeof(ThingB));
            LogAssert.Expect(LogType.Warning,
                new System.Text.RegularExpressions.Regex(".*Multiple types match.*"));

            var result = registry.CreateOneOfType(typeof(IThing));

            Assert.That(result, Is.InstanceOf<ThingA>());
        }

        [Test]
        public void CreateOneOfType_FactoryThrows_LogsAndReturnsNull()
        {
            var registry = RegistryWith(typeof(ThingA));
            LogAssert.Expect(LogType.Error,
                new System.Text.RegularExpressions.Regex(".*Failed to create instance of ThingA.*"));

            var result = registry.CreateOneOfType(typeof(IThing),
                type => throw new InvalidOperationException("nope"));

            Assert.That(result, Is.Null);
        }
        #endregion

        [Test]
        public void Reset_ClearsTypesAndInstances()
        {
            var registry = RegistryWith(typeof(ThingA));
            registry.CreateAll();

            registry.Reset();

            Assert.That(registry.Types, Is.Empty);
            Assert.That(registry.Instances, Is.Empty);
        }

        private static TypeRegistry<IThing> RegistryWith(params Type[] types)
        {
            var registry = new TypeRegistry<IThing>();
            registry.Refresh(() => types);
            return registry;
        }

        #region Test Doubles
        public interface IThing { }
        public class ThingA : IThing { }
        public class ThingB : IThing { }
        public abstract class AbstractThing : IThing { }
        #endregion
    }
}

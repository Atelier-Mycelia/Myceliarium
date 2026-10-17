using System;
using System.Collections.Generic;
using UnityEditor;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Discovers the concrete implementations of <typeparamref name="T"/> through
    /// reflection, and (optionally) keeps instances of them around. This is the shared
    /// core behind the entry, saver and footer registries, which only differ in which
    /// types they accept, which instances they keep, and how they hand them out.
    /// </summary>
    public sealed class TypeRegistry<T> where T : class
    {
        /// <param name="typeFilter">Extra requirement for a type to be accepted, on top
        /// of being a concrete implementation of T.</param>
        /// <param name="instanceFilter">Decides whether a freshly created instance is kept
        /// by CreateAll.</param>
        /// <param name="refreshOnFirstUse">If true, Types refreshes the registry itself
        /// the first time it's read without Refresh having been called.</param>
        public TypeRegistry(Func<Type, bool> typeFilter = null,
            Func<T, bool> instanceFilter = null, bool refreshOnFirstUse = false)
        {
            _typeFilter = typeFilter;
            _instanceFilter = instanceFilter ?? DefaultInstanceFilter;
            _refreshOnFirstUse = refreshOnFirstUse;
        }

        private bool DefaultInstanceFilter(T instance) => true;
        private readonly Func<Type, bool> _typeFilter;
        private readonly Func<T, bool> _instanceFilter;
        private readonly bool _refreshOnFirstUse;

        /// <summary>
        /// Rebuilds the cached list of types. By default this uses Unity's TypeCache
        /// (fast, no need to scan every loaded assembly). A custom
        /// <paramref name="typeProvider"/> can be supplied to make this testable outside
        /// of a live Unity domain reload, or to otherwise override how candidate types
        /// are discovered.
        /// </summary>
        public void Refresh(Func<IEnumerable<Type>> typeProvider = null)
        {
            typeProvider ??= GetTypesFromTypeCache;
            var discovered = FilterTypes(typeProvider(), _typeFilter);

            lock (_lock)
            {
                _types = discovered;
                _refreshed = true;
            }
        }

        private static IEnumerable<Type> GetTypesFromTypeCache()
        {
            return TypeCache.GetTypesDerivedFrom<T>();
        }

        private readonly object _lock = new object();

        /// <summary>
        /// Pure filtering logic, isolated so it can be unit tested without touching
        /// Unity's TypeCache or AppDomain.
        /// </summary>
        public static Type[] FilterTypes(IEnumerable<Type> candidates,
            Func<Type, bool> extraFilter = null)
        {
            if (candidates == null)
            {
                return Array.Empty<Type>();
            }

            extraFilter ??= delegate { return true; };

            var result = new List<Type>();
            foreach (var type in candidates)
            {
                bool isValid = type != null && _baseType.IsAssignableFrom(type) &&
                    type.IsConcrete() && extraFilter(type);
                if (isValid)
                {
                    result.Add(type);
                }
            }
            return result.ToArray();
        }

        private static readonly Type _baseType = typeof(T);
        private Type[] _types = Array.Empty<Type>();

        /// <summary>
        /// A copy of the discovered types.
        /// </summary>
        public Type[] Types
        {
            get
            {
                lock (_lock)
                {
                    if (!_refreshed && _refreshOnFirstUse)
                    {
                        Refresh();
                    }
                    return (Type[])_types.Clone();
                }
            }
        }

        private bool _refreshed;

        /// <summary>
        /// Creates an instance of every discovered type and keeps the ones that pass the
        /// instance filter, replacing whatever was kept before.
        /// </summary>
        public IReadOnlyList<T> CreateAll(Func<Type, T> instanceFactory = null)
        {
            var types = Types;
            lock (_lock)
            {
                _instances.Clear();
                for (int i = 0; i < types.Length; i++)
                {
                    var instance = TryCreate(types[i], instanceFactory);
                    if (instance != null && _instanceFilter(instance))
                    {
                        _instances.Add(instance);
                    }
                }
                return _instances;
            }
        }

        private readonly List<T> _instances = new List<T>();

        /// <summary>
        /// The instances kept by the last CreateAll call.
        /// </summary>
        public IReadOnlyList<T> Instances
        {
            get
            {
                lock (_lock)
                {
                    return _instances;
                }
            }
        }

        public IList<TSub> GetInstancesOfType<TSub>() where TSub : T
        {
            var found = GetInstancesOfType(typeof(TSub));
            var result = new List<TSub>(found.Count);
            for (int i = 0; i < found.Count; i++)
            {
                result.Add((TSub)(object)found[i]);
            }
            return result;
        }

        /// <summary>
        /// The kept instances that are assignable to the given type.
        /// </summary>
        public IList<T> GetInstancesOfType(Type type)
        {
            var result = new List<T>();
            lock (_lock)
            {
                // A regular for-loop, for the sake of performance in Unity 2022.3.
                for (int i = 0; i < _instances.Count; i++)
                {
                    var instance = _instances[i];
                    if (type.IsAssignableFrom(instance.GetType()))
                    {
                        result.Add(instance);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Creates a new, uncached instance of the first discovered type that's
        /// assignable to <paramref name="superType"/>. If several match, the first one
        /// is used and a warning is logged. Returns null if none match or creation
        /// fails.
        /// </summary>
        public T CreateOneOfType(Type superType, Func<Type, T> instanceFactory = null)
        {
            var types = Types;
            Type chosen = null;
            int matchCount = 0;
            for (int i = 0; i < types.Length; i++)
            {
                if (!superType.IsAssignableFrom(types[i]))
                {
                    continue;
                }

                chosen ??= types[i];
                matchCount++;
            }

            if (chosen == null)
            {
                return null;
            }

            if (matchCount > 1)
            {
                UnityEngine.Debug.LogWarning($"Multiple types match {superType.Name}; " +
                    $"using {chosen.Name}.");
            }

            return TryCreate(chosen, instanceFactory);
        }

        private static T TryCreate(Type type, Func<Type, T> instanceFactory)
        {
            try
            {
                return instanceFactory != null
                    ? instanceFactory(type)
                    : Activator.CreateInstance(type) as T;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to create instance of " +
                    $"{type.Name}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Clears all cached state. Intended for test isolation between test cases;
        /// not meant to be called during normal editor operation.
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _types = Array.Empty<Type>();
                _instances.Clear();
                _refreshed = false;
            }
        }

    }
}

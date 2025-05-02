using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FMSoftlab.ObjectMapper
{
    public interface IRegisterMappings
    {
        void Register();
    }
    public static class ObjectMapper
    {
        private static readonly ConcurrentDictionary<string, List<object>> _propertyMapCache = new();

        public static void Register<TSource, TTarget>(Action<MappingConfig<TSource, TTarget>> configure = null)
        {
            var key = GetCacheKey(typeof(TSource), typeof(TTarget));
            if (_propertyMapCache.ContainsKey(key))
                return;

            var config = new MappingConfig<TSource, TTarget>();
            configure?.Invoke(config);

            var defaultMappings = BuildDefaultPropertyMap<TSource, TTarget>();
            var customMappings = config.CustomMappings;

            // Merge: custom overrides default on same Target.Name
            var merged = defaultMappings
                .Where(d => customMappings.All(c => !string.Equals(c.TargetProperty.Name, d.TargetProperty.Name, StringComparison.InvariantCultureIgnoreCase)))
                .Concat(customMappings)
                .Cast<object>()
                .ToList();

            _propertyMapCache[key] = merged;
        }

        public static TTarget Map<TSource, TTarget>(TSource source) where TTarget : new()
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var target = new TTarget();
            var key = GetCacheKey(typeof(TSource), typeof(TTarget));

            if (!_propertyMapCache.TryGetValue(key, out var rawMappings))
            {
                Register<TSource, TTarget>();
                rawMappings = _propertyMapCache[key];
            }

            var mappings = rawMappings.Cast<IPropertyMapping<TSource, TTarget>>();
            foreach (var map in mappings)
            {
                map.Map(source, target);
            }

            return target;
        }
        public static IEnumerable<TTarget> Map<TSource, TTarget>(IEnumerable<TSource> sourceList) where TTarget : new()
        {
            if (sourceList == null) throw new ArgumentNullException(nameof(sourceList));
            return sourceList.Select(Map<TSource, TTarget>);
        }

        private static List<IPropertyMapping<TSource, TTarget>> BuildDefaultPropertyMap<TSource, TTarget>()
        {
            var sourceProps = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var targetProps = typeof(TTarget).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            return (from s in sourceProps
                    from t in targetProps
                    where string.Equals(s.Name, t.Name, StringComparison.InvariantCultureIgnoreCase) &&
                          s.PropertyType == t.PropertyType &&
                          t.CanWrite
                    select CreateDefaultMapping<TSource, TTarget>(s, t)).ToList();
        }

        private static IPropertyMapping<TSource, TTarget> CreateDefaultMapping<TSource, TTarget>(PropertyInfo sourceProp, PropertyInfo targetProp)
        {
            var mappingType = typeof(PropertyMapping<,,>).MakeGenericType(typeof(TSource), typeof(TTarget), sourceProp.PropertyType);
            return (IPropertyMapping<TSource, TTarget>)Activator.CreateInstance(mappingType, sourceProp, targetProp, null);
        }

        private static string GetCacheKey(Type source, Type target) => $"{source.FullName}->{target.FullName}";
    }
}
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FMSoftlab.ObjectMapper
{
    public interface IRegisterMappings
    {
        void Register();
    }
    public static class ObjectMapper
    {
        private static readonly Dictionary<(Type, Type), object> _configs = new Dictionary<(Type, Type), object>();

        public static void Register<TSource, TTarget>(Action<MappingConfig<TSource, TTarget>>? configAction = null)
        {
            var config = new MappingConfig<TSource, TTarget>();
            configAction?.Invoke(config);
            _configs[(typeof(TSource), typeof(TTarget))] = config;
        }

        public static bool IsRegistered(Type source, Type target) => _configs.ContainsKey((source, target));

        public static TTarget Map<TSource, TTarget>(TSource source)
            where TTarget : new()
        {
            if (!_configs.TryGetValue((typeof(TSource), typeof(TTarget)), out var configObj))
                throw new InvalidOperationException($"Mapping not registered for {typeof(TSource)} -> {typeof(TTarget)}");

            var config = (MappingConfig<TSource, TTarget>)configObj;
            var target = new TTarget();
            var defaultMap = BuildDefaultMap<TSource, TTarget>();

            foreach (var map in defaultMap.Values)
                map.Map(source, target);

            foreach (var map in config.CustomMappings)
                defaultMap[map.TargetProperty.Name.ToLowerInvariant()] = map;

            foreach (var map in defaultMap.Values.DistinctBy(m => m.TargetProperty.Name.ToLowerInvariant()))
                map.Map(source, target);

            return target;
        }


        public static IEnumerable<TTarget> MapCollection<TSource, TTarget>(IEnumerable<TSource> sourceList) where TTarget : new()
        {
            if (sourceList?.Any()!=true)
                yield break;
            foreach (var item in sourceList)
            {
                yield return Map<TSource, TTarget>(item);
            }
        }
        public static void MapInto<TSource, TTarget>(TSource source, TTarget target) where TTarget : new()
        {
            if (!_configs.TryGetValue((typeof(TSource), typeof(TTarget)), out var configObj))
                throw new InvalidOperationException($"Mapping not registered for {typeof(TSource)} -> {typeof(TTarget)}");

            var config = (MappingConfig<TSource, TTarget>)configObj;
            var defaultMap = BuildDefaultMap<TSource, TTarget>();

            foreach (var map in config.CustomMappings)
                defaultMap[map.TargetProperty.Name.ToLowerInvariant()] = map;

            foreach (var map in defaultMap.Values.DistinctBy(m => m.TargetProperty.Name.ToLowerInvariant()))
                map.Map(source, target);
        }

        private static Dictionary<string, IPropertyMapping<TSource, TTarget>> BuildDefaultMap<TSource, TTarget>() where TTarget : new()
        {
            var result = new Dictionary<string, IPropertyMapping<TSource, TTarget>>(StringComparer.OrdinalIgnoreCase);
            var sourceProps = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var targetProps = typeof(TTarget).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var targetProp in targetProps)
            {
                var sourceProp = sourceProps.FirstOrDefault(sp => string.Equals(sp.Name, targetProp.Name, StringComparison.OrdinalIgnoreCase));
                if (sourceProp == null) continue;

                if (sourceProp.PropertyType == targetProp.PropertyType && targetProp.CanWrite)
                {
                    var param = Expression.Parameter(typeof(TSource), "src");
                    var srcAccess = Expression.Property(param, sourceProp);
                    var lambda = Expression.Lambda<Func<TSource, object>>(Expression.Convert(srcAccess, typeof(object)), param).Compile();

                    var map = new PropertyMapping<TSource, TTarget, object, object>(sourceProp, targetProp, src => lambda(src));
                    result[targetProp.Name.ToLowerInvariant()] = map;
                }
                else if (IsEnumerable(sourceProp.PropertyType) && IsEnumerable(targetProp.PropertyType))
                {
                    var sourceElem = GetEnumerableType(sourceProp.PropertyType);
                    var targetElem = GetEnumerableType(targetProp.PropertyType);
                    if (sourceElem != null && targetElem != null && _configs.ContainsKey((sourceElem, targetElem)))
                    {
                        var map = new PropertyMapping<TSource, TTarget, object, object>(
                            sourceProp,
                            targetProp,
                            src => MapCollection(sourceProp.GetValue(src), sourceElem, targetElem)
                        );
                        result[targetProp.Name.ToLowerInvariant()] = map;
                    }
                }
                else if (_configs.ContainsKey((sourceProp.PropertyType, targetProp.PropertyType)))
                {
                    var map = new PropertyMapping<TSource, TTarget, object, object>(
                        sourceProp,
                        targetProp,
                        src =>
                        {
                            var nestedSource = sourceProp.GetValue(src);
                            if (nestedSource == null) return null!;

                            var targetInstance = targetProp.GetValue(new TTarget());
                            if (targetProp.CanWrite)
                            {
                                var mapMethod = typeof(ObjectMapper).GetMethod("Map")!.MakeGenericMethod(sourceProp.PropertyType, targetProp.PropertyType);
                                return mapMethod.Invoke(null, new[] { nestedSource })!;
                            }
                            else if (targetInstance != null)
                            {
                                var mapMethodVoid = typeof(ObjectMapper).GetMethod("MapInto")!.MakeGenericMethod(sourceProp.PropertyType, targetProp.PropertyType);
                                mapMethodVoid.Invoke(null, new[] { nestedSource, targetInstance });
                                return targetInstance;
                            }

                            return null!;
                        });
                    result[targetProp.Name.ToLowerInvariant()] = map;
                }
            }

            return result;
        }

        private static bool IsEnumerable(Type type) =>
            type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);

        private static Type? GetEnumerableType(Type type)
        {
            if (type.IsArray) return type.GetElementType();
            if (type.IsGenericType && typeof(IEnumerable<>).IsAssignableFrom(type.GetGenericTypeDefinition()))
                return type.GetGenericArguments()[0];
            return type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))?
                .GetGenericArguments()[0];
        }
        
        private static object MapCollection(object? sourceCollection, Type sourceElem, Type targetElem)
        {
            if (sourceCollection is not IEnumerable enumerable) return Activator.CreateInstance(typeof(List<>).MakeGenericType(targetElem))!;

            var resultList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(targetElem))!;
            var mapMethod = typeof(ObjectMapper).GetMethod("Map")!.MakeGenericMethod(sourceElem, targetElem);

            foreach (var item in enumerable)
            {
                var mapped = mapMethod.Invoke(null, new[] { item });
                resultList.Add(mapped);
            }
            return resultList;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace FMSoftlab.ObjectMapper
{
    public class MappingConfig<TSource, TTarget>
    {
        internal List<IPropertyMapping<TSource, TTarget>> CustomMappings { get; } = new();

        public MappingConfig<TSource, TTarget> Map<TSourceProp, TTargetProp>(
            Expression<Func<TSource, TSourceProp>> sourceSelector,
            Expression<Func<TTarget, TTargetProp>> targetSelector
            )
        {
            var sourceProp = GetPropertyInfo(sourceSelector);
            var targetProp = GetPropertyInfo(targetSelector);

            CustomMappings.Add(new PropertyMapping<TSource, TTarget, TSourceProp, TTargetProp>(sourceProp, targetProp, null));
            return this;
        }


        public MappingConfig<TSource, TTarget> MapCustom<TTargetProp>(
            Expression<Func<TTarget, TTargetProp>> targetSelector,
            Func<TSource, TTargetProp> converter) 
        {
            var targetProp = GetPropertyInfo(targetSelector);

            // We pass `null` for sourceProp since this is a computed mapping
            var mapping = new PropertyMapping<TSource, TTarget, object, TTargetProp>(
                null, targetProp, src => converter(src));

            CustomMappings.Add(mapping);
            return this;
        }

        /*public MappingConfig<TSource, TTarget> Map<TSourceProp, TTargetProp>(
            Expression<Func<TSource, TSourceProp>> sourceSelector,
            Expression<Func<TTarget, TTargetProp>> targetSelector,
            Func<TSource, TTargetProp> converter)
        {
            var sourceProp = GetPropertyInfo(sourceSelector);
            var targetProp = GetPropertyInfo(targetSelector);

            var mapping = new PropertyMapping<TSource, TTarget, TSourceProp, TTargetProp>(sourceProp, targetProp, converter);
            CustomMappings.Add(mapping);
            return this;
        }*/

        private static PropertyInfo GetPropertyInfo<T, TProp>(Expression<Func<T, TProp>> expr)
        {
            if (expr.Body is MemberExpression member && member.Member is PropertyInfo prop)
                return prop;

            throw new ArgumentException("Expression must be a property access", nameof(expr));
        }
    }
}

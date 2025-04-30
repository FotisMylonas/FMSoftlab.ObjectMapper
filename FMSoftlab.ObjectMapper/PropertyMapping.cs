using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FMSoftlab.ObjectMapper
{
    public interface IPropertyMapping<TSource, TTarget>
    {
        void Map(TSource source, TTarget target);
        PropertyInfo TargetProperty { get; }
    }

    public class PropertyMapping<TSource, TTarget, TProp> : IPropertyMapping<TSource, TTarget>
    {
        private readonly PropertyInfo _source;
        private readonly PropertyInfo _target;
        private readonly Func<TProp, TProp> _converter;

        public PropertyMapping(PropertyInfo source, PropertyInfo target, Func<TProp, TProp> converter)
        {
            _source = source;
            _target = target;
            _converter = converter;
        }

        public PropertyInfo TargetProperty => _target;

        public void Map(TSource source, TTarget target)
        {
            var value = (TProp)_source.GetValue(source);
            var result = _converter != null ? _converter(value) : value;
            _target.SetValue(target, result);
        }
    }

    public class PropertyMapping<TSource, TTarget, TSourceProp, TTargetProp> : IPropertyMapping<TSource, TTarget>
    {
        private readonly PropertyInfo? _source;
        private readonly PropertyInfo _target;
        private readonly Func<TSource, TTargetProp> _converter;

        public PropertyMapping(PropertyInfo? source, PropertyInfo target, Func<TSource, TTargetProp> converter)
        {
            _source = source; // may be null for computed mappings
            _target = target;
            _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        }

        public PropertyInfo TargetProperty => _target;

        public void Map(TSource source, TTarget target)
        {
            var convertedValue = _converter(source);
            _target.SetValue(target, convertedValue);
        }
    }

}
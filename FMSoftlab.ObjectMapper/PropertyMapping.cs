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

    public class PropertyMapping<TSource, TTarget, TSourceProp, TTargetProp> : IPropertyMapping<TSource, TTarget>
    {
        private readonly PropertyInfo? _source;
        private readonly PropertyInfo _target;
        private readonly Func<TSource, TTargetProp>? _converter;

        public PropertyMapping(PropertyInfo? source, PropertyInfo target, Func<TSource, TTargetProp>? converter)
        {
            _source = source;
            _target = target;
            _converter = converter;
        }

        public PropertyInfo TargetProperty => _target;

        public void Map(TSource source, TTarget target)
        {
            TTargetProp? value = default(TTargetProp);
            if (_converter!=null)
                value = _converter(source);
            if (_target.CanWrite)
            {
                _target.SetValue(target, value);
            }
            else if (value != null)
            {
                // For read-only properties, attempt to map into the existing instance
                var targetValue = _target.GetValue(target);
                if (targetValue != null && _source != null && ObjectMapper.IsRegistered(_source.PropertyType, _target.PropertyType))
                {
                    var mapMethod = typeof(ObjectMapper).GetMethod("MapInto")!.MakeGenericMethod(_source.PropertyType, _target.PropertyType);
                    mapMethod.Invoke(null, new[] { _source.GetValue(source), targetValue });
                }
            }
        }
    }
}

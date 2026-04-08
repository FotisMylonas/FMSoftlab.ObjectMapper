using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

public interface IRelationNode
{
    void Apply(object parent, LookupCache cache);
    void BuildLookup(LookupCache cache, IDictionary<Type, object> data);
    IEnumerable<IRelationNode> GetAll();
}

public class RelationNode<TParent, TChild, TKey> : IRelationNode
{
    private readonly Func<TParent, TKey> _parentKey;
    private readonly Func<TChild, TKey> _childKey;
    private readonly Action<TParent, List<TChild>> _setter;

    public List<IRelationNode> Children { get; } = new List<IRelationNode>();

    public RelationNode(
        Func<TParent, TKey> parentKey,
        Func<TChild, TKey> childKey,
        Action<TParent, List<TChild>> setter)
    {
        _parentKey = parentKey;
        _childKey = childKey;
        _setter = setter;
    }

    public void Apply(object parentObj, LookupCache cache)
    {
        var parent = (TParent)parentObj;
        var lookup = cache.Get<TChild, TKey>();
        var key = _parentKey(parent);

        var children = lookup[key].ToList();
        _setter(parent, children);

        foreach (var child in children)
        {
            foreach (var sub in Children)
            {
                sub.Apply(child, cache);
            }
        }
    }

    public void BuildLookup(LookupCache cache, IDictionary<Type, object> data)
    {
        if (!data.TryGetValue(typeof(TChild), out var raw))
            throw new InvalidOperationException(
                $"Missing data for type {typeof(TChild).Name}");

        var items = (IEnumerable<TChild>)raw;
        cache.AddLookup(items, _childKey);
    }

    public IEnumerable<IRelationNode> GetAll()
    {
        yield return this;

        foreach (var child in Children)
        {
            foreach (var sub in child.GetAll())
                yield return sub;
        }
    }
}

public class LookupCache
{
    private readonly Dictionary<Type, object> _cache = new Dictionary<Type, object>();

    public void AddLookup<T, TKey>(IEnumerable<T> items, Func<T, TKey> keySelector)
    {
        // Guard against duplicate registrations (same child type, different key)
        if (!_cache.ContainsKey(typeof(T)))
        {
            _cache[typeof(T)] = items.ToLookup(keySelector);
        }
    }

    public ILookup<TKey, T> Get<T, TKey>()
    {
        if (!_cache.TryGetValue(typeof(T), out var lookup))
            throw new InvalidOperationException(
                $"Lookup for type {typeof(T).Name} not found in cache.");

        return (ILookup<TKey, T>)lookup;
    }
}

public class GraphMap<T>
{
    internal List<IRelationNode> Roots { get; } = new List<IRelationNode>();

    // Fixed: removed the redundant duplicate type parameter from the original
    public static GraphMap<T> For() => new GraphMap<T>();

    public GraphMap<T> HasMany<TChild, TKey>(
        Expression<Func<T, List<TChild>>> property,
        Func<TChild, TKey> childKey,
        Func<T, TKey> parentKey,
        Action<GraphMap<TChild>> build = null)
    {
        var setter = CreateSetter(property);

        var node = new RelationNode<T, TChild, TKey>(
            parentKey,
            childKey,
            setter);

        Roots.Add(node);

        if (build != null)
        {
            var childMap = new GraphMap<TChild>();
            build(childMap);

            foreach (var childNode in childMap.Roots)
            {
                node.Children.Add(childNode);
            }
        }

        return this;
    }

    private static Action<T, List<TChild>> CreateSetter<TChild>(
        Expression<Func<T, List<TChild>>> property)
    {
        // Fixed: replaced C# 9 'is not' pattern with explicit null check + type check
        var member = property.Body as MemberExpression;
        if (member == null)
            throw new ArgumentException(
                "Property expression must be a member expression");

        var prop = member.Member as PropertyInfo;
        if (prop == null)
            throw new ArgumentException("Expression must point to a property");

        // Capture prop in closure; avoid repeated reflection on every call
        return (parent, value) => prop.SetValue(parent, value);
    }
}

public static class GraphMapper
{
    public static List<T> Map<T>(
        IEnumerable<T> roots,
        IDictionary<Type, object> data,
        GraphMap<T> map)
    {
        var cache = BuildCache(data, map);
        var list = roots.ToList();

        foreach (var root in list)
        {
            foreach (var relation in map.Roots)
            {
                relation.Apply(root, cache);
            }
        }

        return list;
    }

    private static LookupCache BuildCache<T>(
        IDictionary<Type, object> data,
        GraphMap<T> map)
    {
        var cache = new LookupCache();

        // GetAll() yields each RelationNode (self + descendants),
        // so every child type gets a lookup built exactly once
        foreach (var relation in map.Roots.SelectMany(r => r.GetAll()))
        {
            relation.BuildLookup(cache, data);
        }

        return cache;
    }
}
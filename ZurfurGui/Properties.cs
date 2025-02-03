using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ZurfurGui;


/// <summary>
/// Light weight dynamically typed property key, used to lookup property name and type.
/// </summary>
public readonly struct PropertyKeyId : IEquatable<PropertyKeyId>
{
    static object _lock = new();
    static List<(string name, Type type)> s_properties = new() { ("(unknown)", typeof(void)) };

    readonly int _id;

    public PropertyKeyId(string name, Type type)
    {
        lock (_lock)
        {
            var index = s_properties.FindIndex(n => n.name == name);
            if (index >= 0)
                throw new ArgumentException($"The property '{name}' is already used");
            s_properties.Add((name, type));
            _id = s_properties.Count - 1;
        }
    }

    public string Name => s_properties[_id].name;
    public Type Type => s_properties[_id].type;


    public bool Equals(PropertyKeyId id) => _id == id._id;
    public override bool Equals(object? obj) => obj is PropertyKeyId id && Equals(id);
    public static bool operator ==(PropertyKeyId a, PropertyKeyId b) => a.Equals(b);
    public static bool operator !=(PropertyKeyId a, PropertyKeyId b) => !a.Equals(b);
    public override int GetHashCode() => _id;
    public override string ToString()
    {
        return $"{Name}:{_id}:{Type}";
    }
}


/// <summary>
/// Light weight statically typed property key, used to lookup property name
/// </summary>
public struct PropertyKey<T> : IEquatable<PropertyKey<T>>
{
    readonly PropertyKeyId _id;

    public PropertyKey(string name)
    {
        _id = new PropertyKeyId(name, typeof(T));
    }

    public static implicit operator PropertyKeyId(PropertyKey<T> propertyIndex)
    {
        return propertyIndex._id;
    }

    public string Name => _id.Name;
    public Type Type => _id.Type;
    public PropertyKeyId Id => _id;

    public bool Equals(PropertyKey<T> id) => _id == id._id;
    public override bool Equals(object? obj) => obj is PropertyKey<T> id && Equals(id);
    public static bool operator ==(PropertyKey<T> a, PropertyKey<T> b) => a.Equals(b);
    public static bool operator !=(PropertyKey<T> a, PropertyKey<T> b) => !a.Equals(b);
    public override int GetHashCode() => _id.GetHashCode();
    public override string ToString()
    {
        return _id.ToString();
    }

}

/// <summary>
/// Dictionary of properties
/// </summary>
public class Properties : IEnumerable<(PropertyKeyId key, object value)>
{
    Dictionary<PropertyKeyId, object> _properties = new();

    public Properties()
    {
    }

    public IEnumerator<(PropertyKeyId key, object value)> GetEnumerator()
    {
        foreach (var propertyIndex in _properties)
            yield return (propertyIndex.Key, propertyIndex.Value);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Add a property, throw an exception if it already exists in the collection.
    /// This allows using collection expressions
    /// TBD: Use dictionary collection, when they are added to C#.
    /// </summary>
    public void Add((PropertyKeyId key, object value) kv)
    {
        if (_properties.ContainsKey(kv.key))
            throw new ArgumentException($"The property '{kv.key.Name}' already exists");
        if (kv.key.Type != kv.value.GetType() && !kv.value.GetType().IsAssignableTo(kv.key.Type))
            throw new ArgumentException($"The property '{kv.key.Name}' must have type '{kv.key.Type}', but has '{kv.value.GetType()}'");
        _properties[kv.key] = kv.value;
    }

    /// <summary>
    /// Get a property, or return the default when not present.
    /// </summary>
    public T? Get<T>(PropertyKey<T> property, T? defaultProperty = default)
    {
        if (_properties.TryGetValue(property.Id, out var value) && value is T v)
            return v;
        return defaultProperty;
    }

    /// <summary>
    /// Set a property.  Cannot be null, use Remove instead.  Cannot be an event, use AddEvent instead.
    /// </summary>
    public void Set<T>(PropertyKey<T> property, T value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value), "Use Remove instead of assigning null");
        if (typeof(T).IsAssignableTo(typeof(Delegate)))
            throw new ArgumentException($"Use AddEvent for delegate types", nameof(T));

        _properties[property.Id] = value;
    }

    /// <summary>
    /// Remove a property.  Cannot be an event, use RemoveEvent instead.
    /// </summary>
    public void Remove<T>(PropertyKey<T> property)
    {
        if (typeof(T).IsAssignableTo(typeof(Delegate)))
            throw new ArgumentException($"Use RemoveEvent for delegate types", nameof(T));
        _properties.Remove(property.Id);
    }

    /// <summary>
    /// Add an event
    /// </summary>
    public void AddEvent<T>(PropertyKey<T> property, T ev) where T : Delegate
    {
        if (ev is null)
            return;

        if (!_properties.TryGetValue(property.Id, out var delegateObject))
        {
            _properties[property.Id] = ev;
            return;
        }
        if (delegateObject is not Delegate del)
            throw new InvalidOperationException($"The property '{property.Name}' must be a delegate type, "
                +$"but it contains a property of type '{delegateObject.GetType()}'");
        _properties[property.Id] = Delegate.Combine(del, ev);
    }

    /// <summary>
    /// Add an event
    /// </summary>
    public void RemoveEvent<T>(PropertyKey<T> property, T ev) where T : Delegate
    {
        if (ev is null)
            return;

        if (!_properties.TryGetValue(property.Id, out var delegateObject))
            return;

        if (delegateObject is not Delegate del)
            throw new InvalidOperationException($"The property '{property.Name}' must be a delegate type, "
                + $"but it contains a property of type '{delegateObject.GetType()}'");

        var removedEvent = Delegate.RemoveAll(del, ev);
        if (removedEvent is null)
            _properties.Remove(property.Id);
        else
            _properties[property.Id] = removedEvent;
    }

    public void SetUnion(Properties p)
    {
        foreach (var property in p)
            _properties[property.key] = property.value; 
    }
    

    public bool Has<T>(PropertyKey<T> property)
    {
        return _properties.ContainsKey(property.Id);
    }

}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ZurfurGui.Controls;


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
    public PropertyKeyId Id => _id;

    public bool Equals(PropertyKey<T> id) => _id == id._id;
    public override bool Equals(object? obj) => obj is PropertyKey<T> id && Equals(id);
    public static bool operator ==(PropertyKey<T> a, PropertyKey<T> b) => a.Equals(b);
    public static bool operator !=(PropertyKey<T> a, PropertyKey<T> b) => !a.Equals(b);
    public override int GetHashCode() => _id.GetHashCode();

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

    public void Add((PropertyKeyId key, object value) kv)
    {
        if (_properties.ContainsKey(kv.key))
            throw new ArgumentException($"The property '{kv.key.Name}' already exists");
        if (kv.key.Type != kv.value.GetType() && !kv.value.GetType().IsAssignableTo(kv.key.Type))
            throw new ArgumentException($"The property '{kv.key.Name}' must have type '{kv.key.Type}', but has '{kv.value.GetType()}'");
        _properties[kv.key] = kv.value;
    }

    public T? Gets<T>(PropertyKey<T> property) where T : struct
    {
        if (_properties.TryGetValue(property.Id, out var value) && value is T v)
            return v;
        return null;
    }

    public T? Getc<T>(PropertyKey<T> property) where T : class
    {
        if (_properties.TryGetValue(property.Id, out var value) && value is T v)
            return v;
        return null;
    }

    public void Set<T>(PropertyKey<T> property, T value)
    {
        if (value is null) 
            throw new ArgumentNullException(nameof(value));
        _properties[property.Id] = value;
    }

}

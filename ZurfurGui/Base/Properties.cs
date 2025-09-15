using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ZurfurGui.Base;

/// <summary>
/// Property key info
/// </summary>
public record class PropertyKeyInfo(PropertyKeyId Id, string Name, Type Type)
{
    public override string ToString() => Id.ToString();
};

/// <summary>
/// Global cache of property keys
/// </summary>
public static class PropertyKeys
{
    static object _lock = new();
    static List<PropertyKeyInfo> s_propertiesById = [];
    static Dictionary<string, PropertyKeyInfo> s_propertiesByName = new() ;

    internal static PropertyKeyId Create(string name, Type type)
    {
        lock (_lock)
        {
            if (s_propertiesByName.ContainsKey(name))
                throw new ArgumentException($"The property '{name}' is already used");
            var id = new PropertyKeyId(s_propertiesById.Count);
            var info = new PropertyKeyInfo(id, name, type);
            s_propertiesByName[name] = info;
            s_propertiesById.Add(info);
            return id;
        }
    }

    internal static PropertyKeyInfo GetInfo(int id)
    {
        return s_propertiesById[id];
    }

    public static PropertyKeyInfo? GetInfo(string name)
    {
        if (s_propertiesByName.TryGetValue(name, out var info))
            return info;
        return null;
    }

}

/// <summary>
/// Dynamically typed property key, used to lookup property name and type.
/// </summary>
public readonly struct PropertyKeyId : IEquatable<PropertyKeyId>
{
    readonly int _id;

    internal PropertyKeyId(int id)
    {
        _id = id;
    }

    public string Name => PropertyKeys.GetInfo(_id).Name;
    public Type Type => PropertyKeys.GetInfo(_id).Type;

    public bool Equals(PropertyKeyId id) => _id == id._id;
    public override bool Equals(object? obj) => obj is PropertyKeyId id && Equals(id);
    public static bool operator ==(PropertyKeyId a, PropertyKeyId b) => a.Equals(b);
    public static bool operator !=(PropertyKeyId a, PropertyKeyId b) => !a.Equals(b);
    public override int GetHashCode() => _id;
    public override string ToString()
    {
        return $"{_id}: '{Name}' is {Type.Name}";
    }
}


/// <summary>
/// Statically typed property key, used to lookup property name.
/// </summary>
public struct PropertyKey<T> : IEquatable<PropertyKey<T>>
{
    readonly PropertyKeyId _id;

    public PropertyKey(string name)
    {
        _id = PropertyKeys.Create(name, typeof(T));
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

    public int Count => _properties.Count;

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

    public bool TryGet<T>(PropertyKey<T> property, [MaybeNullWhen(false)]  out T? value)
    {
        if (_properties.TryGetValue(property.Id, out var obj) && obj is T v)
        {
            value = v;
            return true;
        }
        value = default;
        return false;
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

    public void SetById(PropertyKeyId property, object value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value), "Use Remove instead of assigning null");
        if (property.Type.IsAssignableTo(typeof(Delegate)))
            throw new ArgumentException($"Use AddEvent for delegate types", nameof(property));
        if (property.Type != value.GetType())
            throw new ArgumentException($"The property '{property.Name}' must have type '{property.Type}', but has '{value.GetType()}'");
        _properties[property] = value;
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
    /// Remove an event
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

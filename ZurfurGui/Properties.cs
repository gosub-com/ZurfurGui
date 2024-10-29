using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Controls;

namespace ZurfurGui;


public readonly struct PropertyIndexId
{
    static object _lock = new();
    static List<(string name, Type type)> s_properties = new() { ("(unknown)", typeof(void)) };

    readonly int _id;

    public PropertyIndexId(string name, Type type)
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
    public int Id => _id;
    public Type Type => s_properties[_id].type;
}

public class PropertyIndex<T>
{
    readonly PropertyIndexId _id;

    public PropertyIndex(string name)
    {
        _id = new PropertyIndexId(name, typeof(T));
    }

    public static implicit operator PropertyIndexId(PropertyIndex<T> propertyIndex)
    {
        return propertyIndex._id;
    }

    public string Name => _id.Name;
    public PropertyIndexId Id => _id;
}


public class Properties : IEnumerable<(PropertyIndexId key, object value)>
{
    Dictionary<int, object?> _properties = new();

    public Properties() 
    {
    }


    public IEnumerator<(PropertyIndexId key, object value)> GetEnumerator()
    {
        for (int i = 0; i <= 3; i += 2)
        {
            yield return (View.Text, "");
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add((PropertyIndexId key, object value) kv)
    {
        if (_properties.ContainsKey(kv.key.Id))
            throw new ArgumentException($"The property '{kv.key.Name}' already exists");
        if (kv.key.Type != kv.value.GetType())
            throw new ArgumentException($"The property '{kv.key.Name}' must have type '{kv.key.Type}', but has '{kv.value.GetType()}'");
        _properties[kv.key.Id] = kv.value;
    }


    public T? Gets<T>(PropertyIndex<T> property) where T : struct
    {
        if (_properties.TryGetValue(property.Id.Id, out var value) && value is T v)
            return v;
        return null;
    }

    public T? Getc<T>(PropertyIndex<T> property) where T : class
    {
        if (_properties.TryGetValue(property.Id.Id, out var value) && value is T v)
            return v;
        return null;
    }

    public void Set<T>(PropertyIndex<T> property, T value)
    {
        _properties[property.Id.Id] = value;
    }

}

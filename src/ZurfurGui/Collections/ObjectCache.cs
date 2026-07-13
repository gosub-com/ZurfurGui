using System;
using System.Collections.Generic;
using System.Text;

namespace ZurfurGui.Collections;

/// <summary>
/// Manages a cache of objects of type T, allowing for efficient marshalling of objects to unique indices.
/// </summary>
public class ObjectCache<T> where T : class, IEquatable<T>
{
    LruDictionary<T, int> _cache = new();
    Stack<int> _freeStack = new();
    List<T?> _objectList = new();
    Action<T?, int> _marshal;

    public long Hits { get; private set; }
    public long Misses { get; private set; }
    public long Purged { get; private set; }
    public int Count => _cache.Count;
    public long TotalAccesses => Hits + Misses;

    /// <summary>
    /// This function is called to add or remove an object from the cache.
    /// When T? is null, the object at the specified index has been purged from the cache.
    /// </summary>
    public ObjectCache(Action<T?, int> marshal)
    {
        _marshal = marshal;
    }

    /// <summary>
    /// Returns the index of the string in the cache. 
    /// If the string is not in the cache, it is added and its index is returned.
    /// </summary>
    public int GetIndex(T value)
    {
        if (_cache.TryGetValue(value, out int index))
        {
            Hits++;
            return index;
        }

        if (_freeStack.Count > 0)
        {
            index = _freeStack.Pop();
            _objectList[index] = value;
        }
        else
        {
            index = _objectList.Count;
            _objectList.Add(value);
        }
        Misses++;
        _cache[value] = index;
        _marshal(value, index);
        return index;
    }


    /// <summary>
    /// Returns the string at the specified index in the cache.
    /// Throws an exception if the index is out of range or if the string has been purged from the cache.
    /// </summary>
    public T GetObject(int index)
    {
        if (index < 0 || index >= _objectList.Count)
            throw new IndexOutOfRangeException($"Index {index} is out of range for string cache.");
        var s = _objectList[index];
        if (s == null)
            throw new InvalidOperationException($"String at index {index} has been purged from the cache.");
        return s;
    }

    /// <summary>
    /// Purge the least recently used item from the cache.
    /// Throws an exception if the cache is empty. 
    /// </summary>
    public void PurgeLru(int maxSize)
    {
        // Remove least recently used items from the cache
        maxSize = Math.Max(0, maxSize);
        while (_cache.Count > maxSize)
        {
            Purged++;
            var lruKV = _cache.RemoveLru();
            _objectList[lruKV.Value] = null;
            _freeStack.Push(lruKV.Value);
            _marshal(null, lruKV.Value);
        }
    }
}

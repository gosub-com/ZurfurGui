using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace ZurfurGui.Collections;

public class LruDictionary<Key, Value> : IDictionary<Key, Value>, IReadOnlyDictionary<Key, Value> where Key : notnull
{
    struct Node
    {
        public Key Key;
        public Value Value;
        public int Next;
        public int Prev;
    }

    int _headIndex = -1;
    int _tailIndex = -1;
    int _freeIndex = -1;
    Dictionary<Key, int> _keys = new();
    Node[] _nodes = new Node[16];
    int _nodesLength = 0;

    public int Count => _keys.Count;
    public bool IsReadOnly => false;


    /// <summary>
    /// Read a value by key. If the key is found, the value is promoted to most recently used.
    /// If not found, a KeyNotFoundException is thrown. Use TryGetValueAndPromote to avoid exceptions.
    /// </summary>
    public Value this[Key key]
    {
        get
        {
            if (!_keys.TryGetValue(key, out int nodeIndex))
                throw new KeyNotFoundException($"Key not found: {key}");

            MoveToHead(nodeIndex);
            return _nodes[nodeIndex].Value;
        }
        set
        {
            if (_keys.TryGetValue(key, out int nodeIndex))
            {
                // Update existing value
                _nodes[nodeIndex].Value = value;
                MoveToHead(nodeIndex);
            }
            else
            {
                // Add new value
                Add(key, value);
            }
        }
    }

    public void Add(Key key, Value value)
    {
        if (_keys.ContainsKey(key))
            throw new ArgumentException($"Key already exists: {key}");

        int nodeIndex = AllocateNode();
        _nodes[nodeIndex].Key = key;
        _nodes[nodeIndex].Value = value;

        _keys[key] = nodeIndex;
        AddToHead(nodeIndex);
    }

    public void Add(KeyValuePair<Key, Value> item) => Add(item.Key, item.Value);

    public bool Remove(Key key)
    {
        if (!_keys.TryGetValue(key, out int nodeIndex))
            return false;

        _keys.Remove(key);
        RemoveFromList(nodeIndex);
        FreeNode(nodeIndex);
        return true;
    }

    public bool Remove(KeyValuePair<Key, Value> item)
    {
        if (_keys.TryGetValue(item.Key, out int nodeIndex) &&
            EqualityComparer<Value>.Default.Equals(_nodes[nodeIndex].Value, item.Value))
        {
            return Remove(item.Key);
        }
        return false;
    }

    public bool ContainsKey(Key key) => _keys.ContainsKey(key);

    public bool Contains(KeyValuePair<Key, Value> item)
    {
        if (_keys.TryGetValue(item.Key, out int nodeIndex))
            return EqualityComparer<Value>.Default.Equals(_nodes[nodeIndex].Value, item.Value);
        return false;
    }

    /// <summary>
    /// Tries to get a value by key and promotes it to most recently used if successful.
    /// </summary>
    public bool TryGetValue(Key key, [MaybeNullWhen(false)] out Value value)
    {
        if (_keys.TryGetValue(key, out int nodeIndex))
        {
            MoveToHead(nodeIndex);
            value = _nodes[nodeIndex].Value;
            return true;
        }
        value = default;
        return false;
    }


    /// <summary>
    /// Removes the least recently used (oldest) item from the dictionary.
    /// Throws an exception if the dictionary is empty
    /// </summary>
    /// <returns>The removed key-value pair.</returns>
    public KeyValuePair<Key, Value> RemoveLru()
    {
        if (_tailIndex == -1)
            throw new InvalidOperationException("The dictionary is empty.");

        var tailIndex = _tailIndex;
        var tailNode = _nodes[tailIndex];
        Key keyToRemove = tailNode.Key;

        if (keyToRemove ==null)
            throw new InvalidOperationException("The key of the tail node is null, which should not happen.");

        _keys.Remove(keyToRemove);
        RemoveFromList(tailIndex);
        FreeNode(tailIndex);
        return new KeyValuePair<Key, Value>(keyToRemove, tailNode.Value);
    }

    public void Clear()
    {
        _keys.Clear();
        _nodes = new Node[16];
        _nodesLength = 0;
        _headIndex = -1;
        _tailIndex = -1;
        _freeIndex = -1;
    }

    public void CopyTo(KeyValuePair<Key, Value>[] array, int arrayIndex)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));
        if (arrayIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        if (array.Length - arrayIndex < Count)
            throw new ArgumentException("Destination array is not large enough.");

        foreach (var kvp in GetEnumeratorLruFirst())
        {
            array[arrayIndex++] = new KeyValuePair<Key, Value>(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// Gets the collection of keys in the dictionary. 
    /// The order of keys is not guaranteed to be in any particular order.
    /// For ordered enumeration, use GetEnumerator() or GetEnumeratorLruFirst().
    /// </summary>
    public ICollection<Key> Keys => _keys.Keys;


    /// <summary>
    /// Gets the collection of values in the dictionary.
    /// The order of values is not guaranteed to be in any particular order.
    /// NOTE: This is inefficient, use GetEnumerator() or GetEnumeratorLruFirst().
    /// </summary>
    public ICollection<Value> Values => _keys.Values.Select(i => _nodes[i].Value).ToList();

    IEnumerable<Key> IReadOnlyDictionary<Key, Value>.Keys => _keys.Keys;
    IEnumerable<Value> IReadOnlyDictionary<Key, Value>.Values => _keys.Values.Select(i => _nodes[i].Value);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    /// <summary>
    /// Enumerates the dictionary in MRU order (most recently used to least recently used).
    /// Use GetEnumeratorLruFirst() to iterate in reverse order.
    /// </summary>
    public IEnumerator<KeyValuePair<Key, Value>> GetEnumerator()
    {
        // Enumerate from head (most recently used) to tail (least recently used)
        int current = _headIndex;
        while (current != -1)
        {
            var node = _nodes[current];
            yield return new KeyValuePair<Key, Value>(node.Key, node.Value);
            current = node.Next; // Move from head towards tail
        }
    }

    /// <summary>
    /// Enumerates the dictionary in LRU order (least recently used to most recently used).
    /// </summary>
    public IEnumerable<KeyValuePair<Key, Value>> GetEnumeratorLruFirst()
    {
        // Enumerate from tail (least recently used) to head (most recently used)
        int tailIndex = _tailIndex;
        while (tailIndex != -1)
        {
            var node = _nodes[tailIndex];
            yield return new KeyValuePair<Key, Value>(node.Key, node.Value);
            tailIndex = node.Prev; // Move from tail towards head
        }
    }


    private int AllocateNode()
    {
        if (_freeIndex != -1)
        {
            // Reuse from free list
            int nodeIndex = _freeIndex;
            _freeIndex = _nodes[nodeIndex].Next;
            return nodeIndex;
        }
        else
        {
            // Allocate new node
            if (_nodesLength == _nodes.Length)
            {
                Array.Resize(ref _nodes, _nodes.Length * 2);
            }
            int nodeIndex = _nodesLength++;
            _nodes[nodeIndex] = new Node { Next = -1, Prev = -1 };
            return nodeIndex;
        }
    }

    private void FreeNode(int nodeIndex)
    {
        _nodes[nodeIndex].Key = default!; // Release reference for GC
        _nodes[nodeIndex].Value = default!; // Release reference for GC
        _nodes[nodeIndex].Next = _freeIndex;
        _nodes[nodeIndex].Prev = -1; // Not used in free list
        _freeIndex = nodeIndex;
    }

    private void MoveToHead(int nodeIndex)
    {
        if (_headIndex == nodeIndex)
            return; // Already at head

        RemoveFromList(nodeIndex);
        AddToHead(nodeIndex);
    }

    private void RemoveFromList(int nodeIndex)
    {
        int prevIndex = _nodes[nodeIndex].Prev;
        int nextIndex = _nodes[nodeIndex].Next;

        if (prevIndex != -1)
        {
            _nodes[prevIndex].Next = nextIndex;
        }
        else
        {
            _headIndex = nextIndex;
        }

        if (nextIndex != -1)
        {
            _nodes[nextIndex].Prev = prevIndex;
        }
        else
        {
            _tailIndex = prevIndex;
        }
    }

    private void AddToHead(int nodeIndex)
    {
        _nodes[nodeIndex].Next = _headIndex;
        _nodes[nodeIndex].Prev = -1;

        if (_headIndex != -1)
            _nodes[_headIndex].Prev = nodeIndex;

        _headIndex = nodeIndex;

        if (_tailIndex == -1)
            _tailIndex = nodeIndex;
    }


}

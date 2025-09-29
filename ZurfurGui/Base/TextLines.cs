using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using ZurfurGui.Base.Helpers;
using ZurfurGui.Property;

namespace ZurfurGui.Base;

/// <summary>
/// A read-only list of strings, where strings cannot contain new lines (CR or LF).
/// During creation, CR and LF are converted into new array entries.
/// This class has two main purposes. First, it ensures that the array of strings
/// is immutable. Second, it makes it easy to work with multi-line text without having to
/// search for and break apart strings.
/// </summary>
[CollectionBuilder(typeof(TextLinesBuilder), nameof(TextLinesBuilder.Create))]
public class TextLines : IEquatable<TextLines>, IEnumerable<string>, IReadOnlyList<string>
{
    public static readonly TextLines Empty = new();
    public static readonly TextLines Unknown = new("�");


    private readonly string[] _lines;

    public TextLines() => _lines = Array.Empty<string>();

    public TextLines(string text) => _lines = [.. text.Split(["\r\n", "\n", "\r"], StringSplitOptions.None)];

    public TextLines(IEnumerable<string> lines)
    {
        _lines = lines
            .SelectMany(line => line.Split(["\r\n", "\n", "\r"], StringSplitOptions.None))
            .ToArray();
    }

    public TextLines(ReadOnlySpan<string> lines)
    {
        if (lines.Length == 0)
        {
            _lines = Array.Empty<string>();
            return;
        }
        var list = new List<string>();
        foreach (var line in lines)
            list.AddRange(line.Split(["\r\n", "\n", "\r"], StringSplitOptions.None));
        _lines = list.ToArray();
    }


    public int Count => _lines.Length;
    public string this[int index] => _lines[index];
    public Enumerator GetEnumerator() => new Enumerator(_lines);
    IEnumerator<string> IEnumerable<string>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Concatenate all lines with LF (just '\n') between them.
    /// </summary>
    public override string ToString()
    {
        var sb = new StringBuilder();
        for (var i = 0;  i < _lines.Length;  i++)
        {
            sb.Append(_lines[i]);
            if (i < _lines.Length - 1)
                sb.Append('\n');
        }
        return sb.ToString();
    }

    public bool HasLine(TextLines textLines)
    {
        foreach (var line in textLines._lines)
            if (_lines.Contains(line))
                return true;
        return false;
    }

    /// <summary>
    /// Determines whether the current TextLines instance is equal to another TextLines instance.
    /// </summary>
    public bool Equals(TextLines? other)
    {
        if (other == null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (_lines.Length != other._lines.Length)
            return false;

        for (int i = 0; i < _lines.Length; i++)
        {
            if (_lines[i] != other._lines[i])
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is TextLines other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = new Hasher(Count);
        foreach (var line in _lines)
        {
            hash.Add(line.GetHashCode());
        }
        return hash.GetHashCode();
    }

    public static bool operator ==(TextLines? left, TextLines? right)
    {
        if (left is null)
            return right is null;

        return left.Equals(right);
    }

    public static bool operator !=(TextLines? left, TextLines? right)
    {
        return !(left == right);
    }

    /// <summary>
    /// A struct enumerator for TextLines that yields each string in _lines.
    /// </summary>
    public struct Enumerator : IEnumerator<string>
    {
        private readonly string[] _lines;
        private int _index;

        public Enumerator(string[] lines)
        {
            _lines = lines;
            _index = -1;
        }

        public string Current => _lines[_index];

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            _index++;
            return _index < _lines.Length;
        }

        public void Reset() => _index = -1;

        public void Dispose() { }
    }

    internal static class TextLinesBuilder
    {
        internal static TextLines Create(ReadOnlySpan<string> values)
        {
            if (values.Length == 0)
                return Empty;
            return new TextLines(values);
        }
    }
}

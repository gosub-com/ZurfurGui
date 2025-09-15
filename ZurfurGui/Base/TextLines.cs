using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ZurfurGui.Base;

/// <summary>
/// A read-only list of strings, where strings cannot contain new lines (CR or LF).
/// During creation, CR and LF are converted into new array entries.
/// This class has two main purposes. First, it ensures that the array of strings
/// is immutable. Second, it makes it easy to work with multi-line text without having to
/// search for and break apart strings.
/// </summary>
[CollectionBuilder(typeof(TextLinesBuilder), nameof(TextLinesBuilder.Create))]
public class TextLines : IEnumerable<string>, IReadOnlyList<string>
{
    public static string[] Empty = Array.Empty<string>();
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
        var list = new List<string>();
        foreach (var line in lines)
            list.AddRange(line.Split(["\r\n", "\n", "\r"], StringSplitOptions.None));
        _lines = list.ToArray();
    }

    public static implicit operator TextLines(string text) => new TextLines(text);

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
            return new TextLines(values);
        }
    }

}

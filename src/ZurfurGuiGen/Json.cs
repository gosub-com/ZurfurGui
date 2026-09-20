using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace ZurfurGuiGen;


/// <summary>
/// We can't use System.Text.Json in the source generator, so use this small JSON5-subset parser instead.
/// Supported JSON5 additions are // and /* */ comments, trailing commas, single-quoted strings,
/// ASCII identifier keys without quotes, and +, .5, and 5. decimal numbers.
/// JSON5 features intentionally not supported are NaN, Infinity, hexadecimal numbers, the full
/// Unicode identifier grammar, and uncommon JSON5 escape forms.
/// Never allow NULL in the JSON file.
/// </summary>
public static class Json
{
    /// <summary>
    /// Parse a JSON object from a string.  Objects are any of the following:
    ///     Dictionary<string, object?>
    ///     List<object?>
    ///     String
    ///     Double
    ///     Long
    ///     true
    ///     false
    ///     null
    /// NOTE: Collects comments in the json and injects them into the dictionary as "#comment".
    ///       The serializer omits generator-only metadata but preserves runtime properties.
    /// </summary>
    public static Dictionary<string, object?> Parse(string json)
    {
        int index = 0;
        var topComment = SkipWhitespaceAndCollectComment(json, ref index);
        if (index >= json.Length || json[index] != '{')
            throw new LocationException("JSON must start with '{'", 0, 0);

        var result = ParseObject(json, ref index);
        SkipWhitespaceAndCollectComment(json, ref index);
        if (index != json.Length)
            throw GetLocationException("Unexpected content after the root object", json, index);
        if (topComment != "")
            result["#comment"] = topComment;
        return result;
    }

    /// <summary>
    /// Remove the specified keys from a parsed JSON object, including nested objects and arrays.
    /// </summary>
    public static void RemoveKeys(Dictionary<string, object?> dictionary, List<string> keys)
    {
        var keysToRemove = new HashSet<string>(keys);
        RemoveKeysFromDictionary(dictionary, keysToRemove);
    }

    private static void RemoveKeysFromDictionary(Dictionary<string, object?> dictionary, HashSet<string> keysToRemove)
    {
        foreach (var key in keysToRemove)
        {
            dictionary.Remove(key);
        }

        foreach (var value in dictionary.Values)
        {
            if (value is Dictionary<string, object?> childDictionary)
                RemoveKeysFromDictionary(childDictionary, keysToRemove);
            else if (value is List<object?> childArray)
                RemoveKeysFromArray(childArray, keysToRemove);
        }
    }

    private static void RemoveKeysFromArray(List<object?> array, HashSet<string> keysToRemove)
    {
        foreach (var value in array)
        {
            if (value is Dictionary<string, object?> childDictionary)
                RemoveKeysFromDictionary(childDictionary, keysToRemove);
            else if (value is List<object?> childArray)
                RemoveKeysFromArray(childArray, keysToRemove);
        }
    }

    private static Dictionary<string, object?> ParseObject(string json, ref int index)
    {
        var dict = new Dictionary<string, object?>();
        index++; // skip '{'

        while (index < json.Length && json[index] != '}')
        {
            // Collect any // comment lines immediately before the next key
            var pendingComment = SkipWhitespaceAndCollectComment(json, ref index);
            if (index >= json.Length || json[index] == '}')
                break;

            if (pendingComment != "" && dict.Count == 0)
                dict["#comment"] = pendingComment;

            var key = ParseKey(json, ref index);
            SkipWhitespaceAndCollectComment(json, ref index); // discard any comment between key and ':'

            if (index >= json.Length || json[index] != ':')
                throw GetLocationException($"Expected ':' after key '{key}' at location {index}", json, index);
            index++; // skip ':'

            SkipWhitespace(json, ref index);
            var value = ParseValue(json, ref index);

            // If the value is a dictionary and there was a comment before the key, inject it
            if (pendingComment != "" && value is Dictionary<string, object?> childDict)
                childDict["#comment"] = pendingComment;

            dict[key] = value;

            SkipWhitespace(json, ref index);
            if (index >= json.Length)
                throw GetLocationException("Expected ',' or '}' in object", json, index);
            if (json[index] == ',')
            {
                index++; // skip ','
                SkipWhitespace(json, ref index);
                continue; // trailing comma allowed: re-check for '}'
            }
            else if (json[index] != '}')
            {
                throw GetLocationException("Expected ',' or '}' in object", json, index);
            }
        }
        if (index >= json.Length || json[index] != '}')
            throw GetLocationException("Expected '}' at end of object", json, index);
        index++; // skip '}'
        return dict;
    }

    private static List<object?> ParseArray(string json, ref int index)
    {
        var list = new List<object?>();
        index++; // skip '['
        SkipWhitespace(json, ref index);

        while (index < json.Length && json[index] != ']')
        {
            var value = ParseValue(json, ref index);
            list.Add(value);
            SkipWhitespace(json, ref index);
            if (index >= json.Length)
                throw GetLocationException("Expected ',' or ']' in array", json, index);
            if (json[index] == ',')
            {
                index++; // skip ','
                SkipWhitespace(json, ref index);
                continue; // trailing comma allowed: re-check for ']'
            }
            else if (json[index] != ']')
            {
                throw GetLocationException("Expected ',' or ']' in array", json, index);
            }
        }
        if (index >= json.Length || json[index] != ']')
            throw GetLocationException("Expected ']' at end of array", json, index);
        index++; // skip ']'
        return list;
    }

    private static object? ParseValue(string json, ref int index)
    {
        SkipWhitespace(json, ref index);
        if (index >= json.Length)
            throw GetLocationException("Unexpected end of JSON", json, index);

        char c = json[index];
        if (c == '"' || c == '\'')
            return ParseString(json, ref index);
        if (c == '{')
            return ParseObject(json, ref index);
        if (c == '[')
            return ParseArray(json, ref index);
        if (char.IsDigit(c) || c == '-' || c == '+' || c == '.')
            return ParseNumber(json, ref index);
        if (TryParseLiteral(json, ref index, "true", true, out var trueValue))
            return trueValue;
        if (TryParseLiteral(json, ref index, "false", false, out var falseValue))
            return falseValue;
        if (TryParseLiteral(json, ref index, "null", null, out var nullValue))
            return nullValue;
        throw GetLocationException($"Unexpected character '{c}' at position {index}", json, index);
    }

    private static string ParseKey(string json, ref int index)
    {
        if (index < json.Length && (json[index] == '"' || json[index] == '\''))
            return ParseString(json, ref index);

        int start = index;
        if (index >= json.Length || !IsIdentifierStart(json[index]))
            throw GetLocationException("Expected a quoted or identifier object key", json, index);
        index++;
        while (index < json.Length && IsIdentifierPart(json[index]))
            index++;
        return json.Substring(start, index - start);
    }

    private static string ParseString(string json, ref int index)
    {
        if (index >= json.Length || (json[index] != '"' && json[index] != '\''))
            throw GetLocationException("Expected a quoted string", json, index);
        char quote = json[index];
        index++; // skip '"'
        var sb = new StringBuilder();
        while (index < json.Length)
        {
            char c = json[index++];
            if (c == quote)
                return sb.ToString();
            if (c < ' ' || c == '\r' || c == '\n' || c == '\u2028' || c == '\u2029')
                throw GetLocationException("Unescaped control character in string", json, index - 1);
            if (c == '\\')
            {
                if (index >= json.Length)
                    throw GetLocationException("Unexpected end of string escape", json, index);
                char esc = json[index++];
                switch (esc)
                {
                    case '"': sb.Append('"'); break;
                    case '\'': sb.Append('\''); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (index + 4 > json.Length)
                            throw GetLocationException("Invalid unicode escape", json, index);
                        string hex = json.Substring(index, 4);
                        if (!int.TryParse(hex, System.Globalization.NumberStyles.HexNumber,
                            System.Globalization.CultureInfo.InvariantCulture, out int codePoint))
                            throw GetLocationException("Invalid unicode escape", json, index);
                        sb.Append((char)codePoint);
                        index += 4;
                        break;
                    default:
                        throw GetLocationException($"Invalid escape character '\\{esc}'", json, index);
                }
            }
            else
            {
                sb.Append(c);
            }
        }
        throw GetLocationException("Unterminated string", json, index);
    }

    private static object ParseNumber(string json, ref int index)
    {
        int start = index;
        if (index < json.Length && (json[index] == '-' || json[index] == '+'))
            index++;

        int integerDigits = 0;
        while (index < json.Length && IsAsciiDigit(json[index]))
        {
            index++;
            integerDigits++;
        }

        int fractionalDigits = 0;
        if (index < json.Length && json[index] == '.')
        {
            index++;
            while (index < json.Length && IsAsciiDigit(json[index]))
            {
                index++;
                fractionalDigits++;
            }
        }

        if (integerDigits == 0 && fractionalDigits == 0)
            throw GetLocationException("Invalid number", json, start);

        bool hasExponent = false;
        if (index < json.Length && (json[index] == 'e' || json[index] == 'E'))
        {
            hasExponent = true;
            index++;
            if (index < json.Length && (json[index] == '+' || json[index] == '-'))
                index++;
            int exponentDigits = 0;
            while (index < json.Length && IsAsciiDigit(json[index]))
            {
                index++;
                exponentDigits++;
            }
            if (exponentDigits == 0)
                throw GetLocationException("Invalid number exponent", json, index);
        }
        string numStr = json.Substring(start, index - start);
        if (fractionalDigits > 0 || numStr.Contains(".") || hasExponent || numStr.Contains("e") || numStr.Contains("E"))
        {
            if (double.TryParse(numStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double d))
                return d;
        }
        else
        {
            if (long.TryParse(numStr, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out long l))
                return l;
        }
        throw GetLocationException($"Invalid number: {numStr}", json, index);
    }

    private static bool TryParseLiteral(string json, ref int index, string literal, object? value, out object? result)
    {
        result = null;
        if (index + literal.Length > json.Length || string.CompareOrdinal(json, index, literal, 0, literal.Length) != 0)
            return false;
        int end = index + literal.Length;
        if (end < json.Length && IsIdentifierPart(json[end]))
            return false;
        index = end;
        result = value;
        return true;
    }

    private static bool IsIdentifierStart(char c)
    {
        return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '_' || c == '$';
    }

    private static bool IsIdentifierPart(char c)
    {
        return IsIdentifierStart(c) || (c >= '0' && c <= '9');
    }

    private static bool IsAsciiDigit(char c)
    {
        return c >= '0' && c <= '9';
    }

    private static void SkipWhitespace(string json, ref int index)
    {
        SkipWhitespaceAndCollectComment(json, ref index);
    }

    /// <summary>
    /// Skips whitespace and comments. Only // comment lines retain the existing #comment behavior;
    /// block comments are treated as whitespace.
    /// Returns the concatenated text of all comment lines (trimmed, joined with a single space),
    /// or "" if there were no comments.
    /// </summary>
    private static string SkipWhitespaceAndCollectComment(string json, ref int index)
    {
        var comment = new StringBuilder();
        while (index < json.Length)
        {
            // Skip plain whitespace
            while (index < json.Length && char.IsWhiteSpace(json[index]))
                index++;

            // Capture a // comment line
            if (index + 1 < json.Length && json[index] == '/' && json[index + 1] == '/')
            {
                index += 2;
                var line = new StringBuilder();
                while (index < json.Length && json[index] != '\n' && json[index] != '\r')
                    line.Append(json[index++]);
                var trimmed = line.ToString().Trim();
                if (trimmed.Length > 0)
                {
                    if (comment.Length > 0)
                        comment.Append(' ');
                    comment.Append(trimmed);
                }
                continue;
            }

            // Skip a block comment without changing the existing #comment behavior.
            if (index + 1 < json.Length && json[index] == '/' && json[index + 1] == '*')
            {
                int commentStart = index;
                index += 2;
                while (index + 1 < json.Length && !(json[index] == '*' && json[index + 1] == '/'))
                    index++;
                if (index + 1 >= json.Length)
                    throw GetLocationException("Unterminated block comment", json, commentStart);
                index += 2;
                continue;
            }

            break;
        }
        return comment.ToString();
    }


    static LocationException GetLocationException(string message, string json, int index)
    {
        var (line, column) = GetLineAndColumn(json, index);
        return new LocationException(message, line, column);
    }


    static (int line, int column) GetLineAndColumn(string json, int index)
    {
        int line = 0, column = 0;
        for (int i = 0; i < index && i < json.Length; i++)
        {
            if (json[i] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
        }
        return (line, column);
    }

    /// <summary>
    /// Serialize a JSON parsed by Json.Parse.  The dictionary must only contain null, string,
    /// decimal, double, long, bool, Dictionary<string, object?>, or List<object?> values.
    /// </summary>
    public static string Serialize(Dictionary<string, object?> obj)
    {
        return Serialize((object?)obj);
    }


    static string Serialize(object? obj)
    {
        if (obj == null)
            return "null";

        if (obj is string str)
            return $"\"{EscapeString(str)}\"";

        if (obj is bool boolean)
            return boolean ? "true" : "false";

        if (obj is double || obj is long || obj is int || obj is float || obj is decimal)
            return Convert.ToString(obj, System.Globalization.CultureInfo.InvariantCulture);

        if (obj is Dictionary<string, object?> dict)
        {
            var entries = new List<string>();
            foreach (var kvp in dict)
                entries.Add($"\"{EscapeString(kvp.Key)}\": {Serialize(kvp.Value)}");
            return $"{{{string.Join(", ", entries)}}}";
        }

        if (obj is List<object?> list)
        {
            var items = new List<string>();
            foreach (var item in list)
                items.Add(Serialize(item));
            return $"[{string.Join(", ", items)}]";
        }

        throw new InvalidOperationException($"Unsupported type: {obj.GetType()}");
    }

    private static string EscapeString(string str)
    {
        return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }
}

/// <summary>
/// Exception thrown when a JSON parsing error occurs, including line and column information.
/// Line and column are 0 based.
/// </summary>
public class LocationException : FormatException
{
    public int Line { get; }
    public int Column { get; }

    public LocationException(string message, int line, int column)
        : base($"{message} (Line {line}, Column {column})")
    {
        Line = line;
        Column = column;
    }
}

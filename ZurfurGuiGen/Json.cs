using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace ZurfurGuiGen;


/// <summary>
/// We can't use System.Text.Json in the source generator, so use this minimal AI generated json parser instead.
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
    /// </summary>
    public static Dictionary<string, object?> Parse(string json)
    {
        int index = 0;
        SkipWhitespace(json, ref index);
        if (index >= json.Length || json[index] != '{')
            throw new LocationException("JSON must start with '{'", 0, 0);

        return ParseObject(json, ref index);
    }

    private static Dictionary<string, object?> ParseObject(string json, ref int index)
    {
        var dict = new Dictionary<string, object?>();
        index++; // skip '{'
        SkipWhitespace(json, ref index);

        while (index < json.Length && json[index] != '}')
        {
            SkipWhitespace(json, ref index);
            var key = ParseString(json, ref index);
            SkipWhitespace(json, ref index);

            if (json[index] != ':')
                throw GetLocationException($"Expected ':' after key '{key}' at location {index}", json, index);
            index++; // skip ':'

            SkipWhitespace(json, ref index);
            var value = ParseValue(json, ref index);
            dict[key] = value;

            SkipWhitespace(json, ref index);
            if (json[index] == ',')
            {
                index++; // skip ','
                SkipWhitespace(json, ref index);
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
            if (json[index] == ',')
            {
                index++; // skip ','
                SkipWhitespace(json, ref index);
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
        if (c == '"')
            return ParseString(json, ref index);
        if (c == '{')
            return ParseObject(json, ref index);
        if (c == '[')
            return ParseArray(json, ref index);
        if (char.IsDigit(c) || c == '-')
            return ParseNumber(json, ref index);
        if (json.Substring(index).StartsWith("true"))
        {
            index += 4;
            return true;
        }
        if (json.Substring(index).StartsWith("false"))
        {
            index += 5;
            return false;
        }
        if (json.Substring(index).StartsWith("null"))
        {
            index += 4;
            return null;
        }
        throw GetLocationException($"Unexpected character '{c}' at position {index}", json, index);
    }

    private static string ParseString(string json, ref int index)
    {
        if (json[index] != '"')
            throw GetLocationException("Expected '\"' at start of string", json, index);
        index++; // skip '"'
        var sb = new StringBuilder();
        while (index < json.Length)
        {
            char c = json[index++];
            if (c == '"')
                break;
            if (c == '\\')
            {
                if (index >= json.Length)
                    throw new FormatException("Unexpected end of string escape");
                char esc = json[index++];
                switch (esc)
                {
                    case '"': sb.Append('"'); break;
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
                        sb.Append((char)Convert.ToInt32(hex, 16));
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
        return sb.ToString();
    }

    private static object ParseNumber(string json, ref int index)
    {
        int start = index;
        if (json[index] == '-')
            index++;
        while (index < json.Length && char.IsDigit(json[index]))
            index++;
        if (index < json.Length && json[index] == '.')
        {
            index++;
            while (index < json.Length && char.IsDigit(json[index]))
                index++;
        }
        if (index < json.Length && (json[index] == 'e' || json[index] == 'E'))
        {
            index++;
            if (index < json.Length && (json[index] == '+' || json[index] == '-'))
                index++;
            while (index < json.Length && char.IsDigit(json[index]))
                index++;
        }
        string numStr = json.Substring(start, index - start);
        if (numStr.Contains(".") || numStr.Contains("e") || numStr.Contains("E"))
        {
            if (double.TryParse(numStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double d))
                return d;
        }
        else
        {
            if (long.TryParse(numStr, out long l))
                return l;
        }
        throw GetLocationException($"Invalid number: {numStr}", json, index);
    }

    private static void SkipWhitespace(string json, ref int index)
    {
        while (index < json.Length && char.IsWhiteSpace(json[index]))
            index++;
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
            {
                entries.Add($"\"{EscapeString(kvp.Key)}\": {Serialize(kvp.Value)}");
            }
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

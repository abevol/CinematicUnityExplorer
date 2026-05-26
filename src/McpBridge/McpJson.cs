using System.Collections;
using System.Globalization;
using System.Text;

namespace UnityExplorer.McpBridge
{
    internal static class McpJson
    {
        public static object Parse(string json)
        {
            return new Parser(json).Parse();
        }

        public static string Stringify(object value)
        {
            StringBuilder builder = new();
            WriteValue(builder, value);
            return builder.ToString();
        }

        private static void WriteValue(StringBuilder builder, object value)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            if (value is string str)
            {
                WriteString(builder, str);
                return;
            }

            if (value is char ch)
            {
                WriteString(builder, ch.ToString());
                return;
            }

            if (value is bool boolean)
            {
                builder.Append(boolean ? "true" : "false");
                return;
            }

            if (value is Enum)
            {
                WriteString(builder, value.ToString());
                return;
            }

            if (value is IDictionary dictionary)
            {
                WriteObject(builder, dictionary);
                return;
            }

            if (value is IEnumerable enumerable)
            {
                WriteArray(builder, enumerable);
                return;
            }

            if (value is IFormattable formattable)
            {
                builder.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
                return;
            }

            WriteString(builder, value.ToString());
        }

        private static void WriteObject(StringBuilder builder, IDictionary dictionary)
        {
            builder.Append('{');
            bool first = true;
            foreach (DictionaryEntry entry in dictionary)
            {
                if (!first)
                    builder.Append(',');
                first = false;

                WriteString(builder, entry.Key?.ToString() ?? "");
                builder.Append(':');
                WriteValue(builder, entry.Value);
            }
            builder.Append('}');
        }

        private static void WriteArray(StringBuilder builder, IEnumerable enumerable)
        {
            builder.Append('[');
            bool first = true;
            foreach (object item in enumerable)
            {
                if (!first)
                    builder.Append(',');
                first = false;
                WriteValue(builder, item);
            }
            builder.Append(']');
        }

        private static void WriteString(StringBuilder builder, string value)
        {
            builder.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\b':
                        builder.Append("\\b");
                        break;
                    case '\f':
                        builder.Append("\\f");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (c < 32)
                            builder.Append("\\u").Append(((int)c).ToString("x4"));
                        else
                            builder.Append(c);
                        break;
                }
            }
            builder.Append('"');
        }

        private sealed class Parser
        {
            private readonly string json;
            private int index;

            public Parser(string json)
            {
                this.json = json ?? "";
            }

            public object Parse()
            {
                object value = ParseValue();
                SkipWhitespace();
                if (index != json.Length)
                    throw new FormatException("Unexpected trailing JSON content.");
                return value;
            }

            private object ParseValue()
            {
                SkipWhitespace();
                if (index >= json.Length)
                    throw new FormatException("Unexpected end of JSON.");

                char c = json[index];
                if (c == '{')
                    return ParseObject();
                if (c == '[')
                    return ParseArray();
                if (c == '"')
                    return ParseString();
                if (c == '-' || char.IsDigit(c))
                    return ParseNumber();
                if (ConsumeLiteral("true"))
                    return true;
                if (ConsumeLiteral("false"))
                    return false;
                if (ConsumeLiteral("null"))
                    return null;

                throw new FormatException($"Unexpected JSON token '{c}'.");
            }

            private Dictionary<string, object> ParseObject()
            {
                Dictionary<string, object> obj = new();
                index++;
                SkipWhitespace();

                if (TryConsume('}'))
                    return obj;

                while (true)
                {
                    SkipWhitespace();
                    if (index >= json.Length || json[index] != '"')
                        throw new FormatException("Expected JSON object key.");

                    string key = ParseString();
                    SkipWhitespace();
                    Require(':');
                    obj[key] = ParseValue();
                    SkipWhitespace();

                    if (TryConsume('}'))
                        return obj;
                    Require(',');
                }
            }

            private List<object> ParseArray()
            {
                List<object> array = new();
                index++;
                SkipWhitespace();

                if (TryConsume(']'))
                    return array;

                while (true)
                {
                    array.Add(ParseValue());
                    SkipWhitespace();

                    if (TryConsume(']'))
                        return array;
                    Require(',');
                }
            }

            private string ParseString()
            {
                Require('"');
                StringBuilder builder = new();

                while (index < json.Length)
                {
                    char c = json[index++];
                    if (c == '"')
                        return builder.ToString();

                    if (c != '\\')
                    {
                        builder.Append(c);
                        continue;
                    }

                    if (index >= json.Length)
                        throw new FormatException("Unexpected end of JSON string escape.");

                    char escaped = json[index++];
                    switch (escaped)
                    {
                        case '"':
                        case '\\':
                        case '/':
                            builder.Append(escaped);
                            break;
                        case 'b':
                            builder.Append('\b');
                            break;
                        case 'f':
                            builder.Append('\f');
                            break;
                        case 'n':
                            builder.Append('\n');
                            break;
                        case 'r':
                            builder.Append('\r');
                            break;
                        case 't':
                            builder.Append('\t');
                            break;
                        case 'u':
                            if (index + 4 > json.Length)
                                throw new FormatException("Invalid unicode escape.");
                            string hex = json.Substring(index, 4);
                            builder.Append((char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            index += 4;
                            break;
                        default:
                            throw new FormatException($"Invalid JSON string escape '\\{escaped}'.");
                    }
                }

                throw new FormatException("Unterminated JSON string.");
            }

            private object ParseNumber()
            {
                int start = index;

                if (json[index] == '-')
                    index++;

                while (index < json.Length && char.IsDigit(json[index]))
                    index++;

                bool floatingPoint = false;
                if (index < json.Length && json[index] == '.')
                {
                    floatingPoint = true;
                    index++;
                    while (index < json.Length && char.IsDigit(json[index]))
                        index++;
                }

                if (index < json.Length && (json[index] == 'e' || json[index] == 'E'))
                {
                    floatingPoint = true;
                    index++;
                    if (index < json.Length && (json[index] == '+' || json[index] == '-'))
                        index++;
                    while (index < json.Length && char.IsDigit(json[index]))
                        index++;
                }

                string number = json.Substring(start, index - start);
                if (floatingPoint)
                    return double.Parse(number, CultureInfo.InvariantCulture);

                return long.Parse(number, CultureInfo.InvariantCulture);
            }

            private void SkipWhitespace()
            {
                while (index < json.Length && char.IsWhiteSpace(json[index]))
                    index++;
            }

            private bool ConsumeLiteral(string literal)
            {
                if (index + literal.Length > json.Length)
                    return false;

                for (int i = 0; i < literal.Length; i++)
                {
                    if (json[index + i] != literal[i])
                        return false;
                }

                index += literal.Length;
                return true;
            }

            private bool TryConsume(char c)
            {
                if (index < json.Length && json[index] == c)
                {
                    index++;
                    return true;
                }
                return false;
            }

            private void Require(char c)
            {
                SkipWhitespace();
                if (index >= json.Length || json[index] != c)
                    throw new FormatException($"Expected JSON token '{c}'.");
                index++;
            }
        }
    }
}

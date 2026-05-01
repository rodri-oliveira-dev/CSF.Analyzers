using System.Collections.Immutable;
using System.Text;

namespace Swa.Analyzers.Core.Common;

internal static class JsonStringArrayOptionParser
{
    public static bool TryParse(string value, out ImmutableArray<string> items)
    {
        var parser = new Parser(value);
        return parser.TryParse(out items);
    }

    private struct Parser
    {
        private readonly string _value;
        private int _position;

        public Parser(string value)
        {
            _value = value;
            _position = 0;
        }

        public bool TryParse(out ImmutableArray<string> items)
        {
            var builder = ImmutableArray.CreateBuilder<string>();

            SkipWhitespace();

            if (!TryRead('['))
            {
                items = ImmutableArray<string>.Empty;
                return false;
            }

            SkipWhitespace();

            if (TryRead(']'))
            {
                return Complete(builder, out items);
            }

            while (true)
            {
                SkipWhitespace();

                if (!TryReadString(out var item))
                {
                    items = ImmutableArray<string>.Empty;
                    return false;
                }

                builder.Add(item);
                SkipWhitespace();

                if (TryRead(']'))
                {
                    return Complete(builder, out items);
                }

                if (!TryRead(','))
                {
                    items = ImmutableArray<string>.Empty;
                    return false;
                }
            }
        }

        private bool Complete(ImmutableArray<string>.Builder builder, out ImmutableArray<string> items)
        {
            SkipWhitespace();

            if (_position != _value.Length)
            {
                items = ImmutableArray<string>.Empty;
                return false;
            }

            items = builder.ToImmutable();
            return true;
        }

        private bool TryReadString(out string value)
        {
            var builder = new StringBuilder();

            if (!TryRead('"'))
            {
                value = string.Empty;
                return false;
            }

            while (_position < _value.Length)
            {
                var current = _value[_position++];

                if (current == '"')
                {
                    value = builder.ToString();
                    return true;
                }

                if (current == '\\')
                {
                    if (!TryReadEscapedCharacter(builder))
                    {
                        value = string.Empty;
                        return false;
                    }

                    continue;
                }

                builder.Append(current);
            }

            value = string.Empty;
            return false;
        }

        private bool TryReadEscapedCharacter(StringBuilder builder)
        {
            if (_position >= _value.Length)
            {
                return false;
            }

            var escaped = _value[_position++];

            switch (escaped)
            {
                case '"':
                case '\\':
                case '/':
                    builder.Append(escaped);
                    return true;
                case 'b':
                    builder.Append('\b');
                    return true;
                case 'f':
                    builder.Append('\f');
                    return true;
                case 'n':
                    builder.Append('\n');
                    return true;
                case 'r':
                    builder.Append('\r');
                    return true;
                case 't':
                    builder.Append('\t');
                    return true;
                case 'u':
                    return TryReadUnicodeEscape(builder);
                default:
                    return false;
            }
        }

        private bool TryReadUnicodeEscape(StringBuilder builder)
        {
            if (_position + 4 > _value.Length)
            {
                return false;
            }

            var codePoint = 0;

            for (var index = 0; index < 4; index++)
            {
                var hexValue = GetHexValue(_value[_position + index]);

                if (hexValue < 0)
                {
                    return false;
                }

                codePoint = (codePoint * 16) + hexValue;
            }

            _position += 4;
            builder.Append((char)codePoint);
            return true;
        }

        private static int GetHexValue(char value)
        {
            if (value >= '0' && value <= '9')
            {
                return value - '0';
            }

            if (value >= 'a' && value <= 'f')
            {
                return value - 'a' + 10;
            }

            if (value >= 'A' && value <= 'F')
            {
                return value - 'A' + 10;
            }

            return -1;
        }

        private bool TryRead(char expected)
        {
            if (_position >= _value.Length || _value[_position] != expected)
            {
                return false;
            }

            _position++;
            return true;
        }

        private void SkipWhitespace()
        {
            while (_position < _value.Length && char.IsWhiteSpace(_value[_position]))
            {
                _position++;
            }
        }
    }
}

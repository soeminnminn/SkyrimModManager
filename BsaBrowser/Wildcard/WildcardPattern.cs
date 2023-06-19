using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace BsaBrowser.Wildcard
{
    public sealed class WildcardPattern
    {
        private const char escapeChar = '`';

        private Predicate<string> _isMatch;

        private readonly string pattern;

        private readonly WildcardOptions options;

        internal string Pattern => pattern;

        internal WildcardOptions Options => options;

        internal string PatternConvertedToRegex
        {
            get
            {
                Regex regex = WildcardPatternToRegexParser.Parse(this);
                return regex.ToString();
            }
        }

        public WildcardPattern(string pattern)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException("pattern");
            }
            this.pattern = pattern;
        }

        public WildcardPattern(string pattern, WildcardOptions options)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException("pattern");
            }
            this.pattern = pattern;
            this.options = options;
        }

        private bool Init()
        {
            if (_isMatch == null)
            {
#if REGEX
                Regex regex = WildcardPatternToRegexParser.Parse(this);
                _isMatch = regex.IsMatch;
#else

                WildcardPatternMatcher matcher = new WildcardPatternMatcher(this);
                _isMatch = matcher.IsMatch;
#endif
            }
            return _isMatch != null;
        }

        public bool IsMatch(string input)
        {
            if (input == null)
            {
                return false;
            }
            bool result = false;
            if (Init())
            {
                result = _isMatch(input);
            }
            return result;
        }

        internal static string Escape(string pattern, char[] charsNotToEscape)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException("pattern");
            }
            if (charsNotToEscape == null)
            {
                throw new ArgumentNullException("charsNotToEscape");
            }
            char[] array = new char[pattern.Length * 2 + 1];
            int count = 0;
            foreach (char c in pattern)
            {
                if (IsWildcardChar(c) && !charsNotToEscape.Contains(c))
                {
                    array[count++] = escapeChar;
                }
                array[count++] = c;
            }
            if (count > 0)
            {
                return new string(array, 0, count);
            }
            return string.Empty;
        }

        public static string Escape(string pattern)
        {
            return Escape(pattern, new char[0]);
        }

        public static bool ContainsWildcardCharacters(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                return false;
            }
            bool result = false;
            for (int i = 0; i < pattern.Length; i++)
            {
                if (IsWildcardChar(pattern[i]))
                {
                    result = true;
                    break;
                }
                if (pattern[i] == escapeChar)
                {
                    i++;
                }
            }
            return result;
        }

        public static string Unescape(string pattern)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException("pattern");
            }
            char[] array = new char[pattern.Length];
            int count = 0;
            bool flag = false;
            foreach (char c in pattern)
            {
                if (c == escapeChar)
                {
                    if (flag)
                    {
                        array[count++] = c;
                        flag = false;
                    }
                    else
                    {
                        flag = true;
                    }
                    continue;
                }
                if (flag && !IsWildcardChar(c))
                {
                    array[count++] = escapeChar;
                }
                array[count++] = c;
                flag = false;
            }
            if (flag)
            {
                array[count++] = escapeChar;
            }

            if (count > 0)
            {
                return new string(array, 0, count);
            }
            return string.Empty;
        }

        private static bool IsWildcardChar(char ch)
        {
            if (ch != '*' && ch != '?' && ch != '[')
            {
                return ch == ']';
            }
            return true;
        }
    }
}

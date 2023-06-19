using System;
using System.Text;
using System.Text.RegularExpressions;

namespace BsaBrowser.Wildcard
{
    internal class WildcardPatternToRegexParser : WildcardPatternParser
    {
        private const string regexChars = "()[.?*{}^$+|\\";

        private StringBuilder regexPattern;

        private RegexOptions regexOptions;

        private static bool IsRegexChar(char ch)
        {
            for (int i = 0; i < regexChars.Length; i++)
            {
                if (ch == regexChars[i])
                {
                    return true;
                }
            }
            return false;
        }

        internal static RegexOptions TranslateWildcardOptionsIntoRegexOptions(WildcardOptions options)
        {
            RegexOptions regexOptions = RegexOptions.Singleline;
            if ((options & WildcardOptions.Compiled) != 0)
            {
                regexOptions |= RegexOptions.Compiled;
            }
            if ((options & WildcardOptions.IgnoreCase) != 0)
            {
                regexOptions |= RegexOptions.IgnoreCase;
            }
            if ((options & WildcardOptions.CultureInvariant) == WildcardOptions.CultureInvariant)
            {
                regexOptions |= RegexOptions.CultureInvariant;
            }
            return regexOptions;
        }

        protected override void BeginWildcardPattern(WildcardPattern pattern)
        {
            regexPattern = new StringBuilder(pattern.Pattern.Length * 2 + 2);
            regexPattern.Append('^');
            regexOptions = TranslateWildcardOptionsIntoRegexOptions(pattern.Options);
        }

        internal static void AppendLiteralCharacter(StringBuilder regexPattern, char c)
        {
            if (IsRegexChar(c))
            {
                regexPattern.Append('\\');
            }
            regexPattern.Append(c);
        }

        protected override void AppendLiteralCharacter(char c)
        {
            AppendLiteralCharacter(regexPattern, c);
        }

        protected override void AppendAsterix()
        {
            regexPattern.Append(".*");
        }

        protected override void AppendQuestionMark()
        {
            regexPattern.Append('.');
        }

        protected override void EndWildcardPattern()
        {
            regexPattern.Append('$');
            string text = regexPattern.ToString();
            if (text.Equals("^.*$", StringComparison.Ordinal))
            {
                regexPattern.Remove(0, 4);
                return;
            }
            if (text.StartsWith("^.*", StringComparison.Ordinal))
            {
                regexPattern.Remove(0, 3);
            }
            if (text.EndsWith(".*$", StringComparison.Ordinal))
            {
                regexPattern.Remove(regexPattern.Length - 3, 3);
            }
        }

        protected override void BeginBracketExpression()
        {
            regexPattern.Append('[');
        }

        internal static void AppendLiteralCharacterToBracketExpression(StringBuilder regexPattern, char c)
        {
            switch (c)
            {
                case '[':
                    regexPattern.Append('[');
                    break;
                case ']':
                    regexPattern.Append("\\]");
                    break;
                case '-':
                    regexPattern.Append("\\x2d");
                    break;
                default:
                    AppendLiteralCharacter(regexPattern, c);
                    break;
            }
        }

        protected override void AppendLiteralCharacterToBracketExpression(char c)
        {
            AppendLiteralCharacterToBracketExpression(regexPattern, c);
        }

        internal static void AppendCharacterRangeToBracketExpression(StringBuilder regexPattern, char startOfCharacterRange, char endOfCharacterRange)
        {
            AppendLiteralCharacterToBracketExpression(regexPattern, startOfCharacterRange);
            regexPattern.Append('-');
            AppendLiteralCharacterToBracketExpression(regexPattern, endOfCharacterRange);
        }

        protected override void AppendCharacterRangeToBracketExpression(char startOfCharacterRange, char endOfCharacterRange)
        {
            AppendCharacterRangeToBracketExpression(regexPattern, startOfCharacterRange, endOfCharacterRange);
        }

        protected override void EndBracketExpression()
        {
            regexPattern.Append(']');
        }

        public static Regex Parse(WildcardPattern wildcardPattern)
        {
            WildcardPatternToRegexParser wildcardPatternToRegexParser = new WildcardPatternToRegexParser();
            WildcardPatternParser.Parse(wildcardPattern, wildcardPatternToRegexParser);
            try
            {
                return new Regex(wildcardPatternToRegexParser.regexPattern.ToString(), wildcardPatternToRegexParser.regexOptions);
            }
            catch (ArgumentException)
            {
                throw WildcardPatternParser.NewWildcardPatternException(wildcardPattern.Pattern);
            }
        }
    }
}

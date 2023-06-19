using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BsaBrowser.Wildcard
{
    internal abstract class WildcardPatternParser
    {
        private const string InvalidPattern = "The specified wildcard character pattern is not valid: {0}";

        protected virtual void BeginWildcardPattern(WildcardPattern pattern)
        {
        }

        protected abstract void AppendLiteralCharacter(char c);

        protected abstract void AppendAsterix();

        protected abstract void AppendQuestionMark();

        protected virtual void EndWildcardPattern()
        {
        }

        protected abstract void BeginBracketExpression();

        protected abstract void AppendLiteralCharacterToBracketExpression(char c);

        protected abstract void AppendCharacterRangeToBracketExpression(char startOfCharacterRange, char endOfCharacterRange);

        protected abstract void EndBracketExpression();

        internal void AppendBracketExpression(string brackedExpressionContents, string bracketExpressionOperators, string pattern)
        {
            BeginBracketExpression();
            int num = 0;
            while (num < brackedExpressionContents.Length)
            {
                if (num + 2 < brackedExpressionContents.Length && bracketExpressionOperators[num + 1] == '-')
                {
                    char c = brackedExpressionContents[num];
                    char c2 = brackedExpressionContents[num + 2];
                    num += 3;
                    if (c > c2)
                    {
                        throw NewWildcardPatternException(pattern);
                    }
                    AppendCharacterRangeToBracketExpression(c, c2);
                }
                else
                {
                    AppendLiteralCharacterToBracketExpression(brackedExpressionContents[num]);
                    num++;
                }
            }
            EndBracketExpression();
        }

        public static void Parse(WildcardPattern pattern, WildcardPatternParser parser)
        {
            parser.BeginWildcardPattern(pattern);
            bool flag = false;
            bool flag2 = false;
            bool flag3 = false;
            StringBuilder stringBuilder = null;
            StringBuilder stringBuilder2 = null;
            string pattern2 = pattern.Pattern;
            foreach (char c in pattern2)
            {
                if (flag3)
                {
                    if (c == ']' && !flag2 && !flag)
                    {
                        flag3 = false;
                        parser.AppendBracketExpression(stringBuilder.ToString(), stringBuilder2.ToString(), pattern.Pattern);
                        stringBuilder = null;
                        stringBuilder2 = null;
                    }
                    else if (c != '`' || flag)
                    {
                        stringBuilder.Append(c);
                        stringBuilder2.Append((c == '-' && !flag) ? '-' : ' ');
                    }
                    flag2 = false;
                }
                else if (c == '*' && !flag)
                {
                    parser.AppendAsterix();
                }
                else if (c == '?' && !flag)
                {
                    parser.AppendQuestionMark();
                }
                else if (c == '[' && !flag)
                {
                    flag3 = true;
                    stringBuilder = new StringBuilder();
                    stringBuilder2 = new StringBuilder();
                    flag2 = true;
                }
                else if (c != '`' || flag)
                {
                    parser.AppendLiteralCharacter(c);
                }
                flag = c == '`' && !flag;
            }
            if (flag3)
            {
                throw NewWildcardPatternException(pattern.Pattern);
            }
            if (flag && !pattern.Pattern.Equals("`", StringComparison.Ordinal))
            {
                parser.AppendLiteralCharacter(pattern.Pattern[pattern.Pattern.Length - 1]);
            }
            parser.EndWildcardPattern();
        }

        internal static WildcardPatternException NewWildcardPatternException(string invalidPattern)
        {
            string message = string.Format(InvalidPattern, invalidPattern);
            return new WildcardPatternException(message);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BsaBrowser.Wildcard
{
    internal class WildcardPatternMatcher
    {
        private class PatternPositionsVisitor
        {
            private readonly int _lengthOfPattern;

            private readonly int[] _isPatternPositionVisitedMarker;

            private readonly int[] _patternPositionsForFurtherProcessing;

            private int _patternPositionsForFurtherProcessingCount;

            public int StringPosition { private get; set; }

            public bool ReachedEndOfPattern => _isPatternPositionVisitedMarker[_lengthOfPattern] >= StringPosition;

            public PatternPositionsVisitor(int lengthOfPattern)
            {
                _lengthOfPattern = lengthOfPattern;
                _isPatternPositionVisitedMarker = new int[lengthOfPattern + 1];
                for (int i = 0; i < _isPatternPositionVisitedMarker.Length; i++)
                {
                    _isPatternPositionVisitedMarker[i] = -1;
                }
                _patternPositionsForFurtherProcessing = new int[lengthOfPattern];
                _patternPositionsForFurtherProcessingCount = 0;
            }

            public void Add(int patternPosition)
            {
                if (_isPatternPositionVisitedMarker[patternPosition] != StringPosition)
                {
                    _isPatternPositionVisitedMarker[patternPosition] = StringPosition;
                    if (patternPosition < _lengthOfPattern)
                    {
                        _patternPositionsForFurtherProcessing[_patternPositionsForFurtherProcessingCount] = patternPosition;
                        _patternPositionsForFurtherProcessingCount++;
                    }
                }
            }

            public bool MoveNext(out int patternPosition)
            {
                if (_patternPositionsForFurtherProcessingCount == 0)
                {
                    patternPosition = -1;
                    return false;
                }
                _patternPositionsForFurtherProcessingCount--;
                patternPosition = _patternPositionsForFurtherProcessing[_patternPositionsForFurtherProcessingCount];
                return true;
            }
        }

        private abstract class PatternElement
        {
            public abstract void ProcessStringCharacter(char currentStringCharacter, int currentPatternPosition, PatternPositionsVisitor patternPositionsForCurrentStringPosition, PatternPositionsVisitor patternPositionsForNextStringPosition);

            public abstract void ProcessEndOfString(int currentPatternPosition, PatternPositionsVisitor patternPositionsForEndOfStringPosition);
        }

        private class QuestionMarkElement : PatternElement
        {
            public override void ProcessStringCharacter(char currentStringCharacter, int currentPatternPosition, PatternPositionsVisitor patternPositionsForCurrentStringPosition, PatternPositionsVisitor patternPositionsForNextStringPosition)
            {
                patternPositionsForNextStringPosition.Add(currentPatternPosition + 1);
            }

            public override void ProcessEndOfString(int currentPatternPosition, PatternPositionsVisitor patternPositionsForEndOfStringPosition)
            {
            }
        }

        private class LiteralCharacterElement : QuestionMarkElement
        {
            private readonly char _literalCharacter;

            public LiteralCharacterElement(char literalCharacter)
            {
                _literalCharacter = literalCharacter;
            }

            public override void ProcessStringCharacter(char currentStringCharacter, int currentPatternPosition, PatternPositionsVisitor patternPositionsForCurrentStringPosition, PatternPositionsVisitor patternPositionsForNextStringPosition)
            {
                if (_literalCharacter == currentStringCharacter)
                {
                    base.ProcessStringCharacter(currentStringCharacter, currentPatternPosition, patternPositionsForCurrentStringPosition, patternPositionsForNextStringPosition);
                }
            }
        }

        private class BracketExpressionElement : QuestionMarkElement
        {
            private readonly Regex _regex;

            public BracketExpressionElement(Regex regex)
            {
                _regex = regex;
            }

            public override void ProcessStringCharacter(char currentStringCharacter, int currentPatternPosition, PatternPositionsVisitor patternPositionsForCurrentStringPosition, PatternPositionsVisitor patternPositionsForNextStringPosition)
            {
                if (_regex.IsMatch(new string(currentStringCharacter, 1)))
                {
                    base.ProcessStringCharacter(currentStringCharacter, currentPatternPosition, patternPositionsForCurrentStringPosition, patternPositionsForNextStringPosition);
                }
            }
        }

        private class AsterixElement : PatternElement
        {
            public override void ProcessStringCharacter(char currentStringCharacter, int currentPatternPosition, PatternPositionsVisitor patternPositionsForCurrentStringPosition, PatternPositionsVisitor patternPositionsForNextStringPosition)
            {
                patternPositionsForCurrentStringPosition.Add(currentPatternPosition + 1);
                patternPositionsForNextStringPosition.Add(currentPatternPosition);
            }

            public override void ProcessEndOfString(int currentPatternPosition, PatternPositionsVisitor patternPositionsForEndOfStringPosition)
            {
                patternPositionsForEndOfStringPosition.Add(currentPatternPosition + 1);
            }
        }

        private class MyWildcardPatternParser : WildcardPatternParser
        {
            private readonly List<PatternElement> _patternElements = new List<PatternElement>();

            private CharacterNormalizer _characterNormalizer;

            private RegexOptions _regexOptions;

            private StringBuilder _bracketExpressionBuilder;

            public static PatternElement[] Parse(WildcardPattern pattern, CharacterNormalizer characterNormalizer)
            {
                MyWildcardPatternParser myWildcardPatternParser = new MyWildcardPatternParser();
                myWildcardPatternParser._characterNormalizer = characterNormalizer;
                myWildcardPatternParser._regexOptions = WildcardPatternToRegexParser.TranslateWildcardOptionsIntoRegexOptions(pattern.Options);
                MyWildcardPatternParser myWildcardPatternParser2 = myWildcardPatternParser;
                WildcardPatternParser.Parse(pattern, myWildcardPatternParser2);
                return myWildcardPatternParser2._patternElements.ToArray();
            }

            protected override void AppendLiteralCharacter(char c)
            {
                c = _characterNormalizer.Normalize(c);
                _patternElements.Add(new LiteralCharacterElement(c));
            }

            protected override void AppendAsterix()
            {
                _patternElements.Add(new AsterixElement());
            }

            protected override void AppendQuestionMark()
            {
                _patternElements.Add(new QuestionMarkElement());
            }

            protected override void BeginBracketExpression()
            {
                _bracketExpressionBuilder = new StringBuilder();
                _bracketExpressionBuilder.Append('[');
            }

            protected override void AppendLiteralCharacterToBracketExpression(char c)
            {
                WildcardPatternToRegexParser.AppendLiteralCharacterToBracketExpression(_bracketExpressionBuilder, c);
            }

            protected override void AppendCharacterRangeToBracketExpression(char startOfCharacterRange, char endOfCharacterRange)
            {
                WildcardPatternToRegexParser.AppendCharacterRangeToBracketExpression(_bracketExpressionBuilder, startOfCharacterRange, endOfCharacterRange);
            }

            protected override void EndBracketExpression()
            {
                _bracketExpressionBuilder.Append(']');
                Regex regex = new Regex(_bracketExpressionBuilder.ToString(), _regexOptions);
                _patternElements.Add(new BracketExpressionElement(regex));
            }
        }

        private class CharacterNormalizer
        {
            private readonly CultureInfo _cultureInfo;

            private readonly bool _caseInsensitive;

            public CharacterNormalizer(WildcardOptions options)
            {
                if (WildcardOptions.CultureInvariant == (options & WildcardOptions.CultureInvariant))
                {
                    _cultureInfo = CultureInfo.InvariantCulture;
                }
                else
                {
                    _cultureInfo = CultureInfo.CurrentCulture;
                }
                _caseInsensitive = WildcardOptions.IgnoreCase == (options & WildcardOptions.IgnoreCase);
            }

            public char Normalize(char x)
            {
                if (_caseInsensitive)
                {
                    return _cultureInfo.TextInfo.ToLower(x);
                }
                return x;
            }
        }

        private readonly PatternElement[] _patternElements;

        private readonly CharacterNormalizer _characterNormalizer;

        internal WildcardPatternMatcher(WildcardPattern wildcardPattern)
        {
            _characterNormalizer = new CharacterNormalizer(wildcardPattern.Options);
            _patternElements = MyWildcardPatternParser.Parse(wildcardPattern, _characterNormalizer);
        }

        internal bool IsMatch(string str)
        {
            StringBuilder stringBuilder = new StringBuilder(str.Length);
            string text = str;
            foreach (char x in text)
            {
                stringBuilder.Append(_characterNormalizer.Normalize(x));
            }
            str = stringBuilder.ToString();
            PatternPositionsVisitor patternPositionsVisitor = new PatternPositionsVisitor(_patternElements.Length);
            patternPositionsVisitor.Add(0);
            PatternPositionsVisitor patternPositionsVisitor2 = new PatternPositionsVisitor(_patternElements.Length);
            for (int j = 0; j < str.Length; j++)
            {
                char currentStringCharacter = str[j];
                patternPositionsVisitor.StringPosition = j;
                patternPositionsVisitor2.StringPosition = j + 1;
                int patternPosition;
                while (patternPositionsVisitor.MoveNext(out patternPosition))
                {
                    _patternElements[patternPosition].ProcessStringCharacter(currentStringCharacter, patternPosition, patternPositionsVisitor, patternPositionsVisitor2);
                }
                PatternPositionsVisitor patternPositionsVisitor3 = patternPositionsVisitor;
                patternPositionsVisitor = patternPositionsVisitor2;
                patternPositionsVisitor2 = patternPositionsVisitor3;
            }
            int patternPosition2;
            while (patternPositionsVisitor.MoveNext(out patternPosition2))
            {
                _patternElements[patternPosition2].ProcessEndOfString(patternPosition2, patternPositionsVisitor);
            }
            return patternPositionsVisitor.ReachedEndOfPattern;
        }
    }
}

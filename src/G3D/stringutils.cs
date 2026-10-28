/**
 @file stringutils.h
 
 @maintainer Morgan McGuire, matrix@graphics3d.com
 
 @author  2000-09-09
 @edited  2002-11-30
 */

using System;
using global::System.Globalization;

namespace G3D
{
    public static class StringUtils
    {
        public const string NEWLINE = "\r\n";

        public static bool beginsWith(string test, string pattern)
        {
            if (test == null)
            {
                throw new ArgumentNullException("test");
            }
            if (pattern == null)
            {
                throw new ArgumentNullException("pattern");
            }

            if (test.Length >= pattern.Length)
            {
                for (int i = 0; i < pattern.Length; ++i)
                {
                    if (pattern[i] != test[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool endsWith(string test, string pattern)
        {
            if (test == null)
            {
                throw new ArgumentNullException("test");
            }
            if (pattern == null)
            {
                throw new ArgumentNullException("pattern");
            }

            if (test.Length >= pattern.Length)
            {
                int te = test.Length - 1;
                int pe = pattern.Length - 1;
                for (int i = pattern.Length - 1; i >= 0; --i)
                {
                    if (pattern[pe - i] != test[te - i])
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public static string wordWrap(string input, int numCols)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            string output = "";
            int c = 0;
            int len;

            int minLength = numCols / 4;
            int inLen = input.Length;

            bool first = true;
            while (c < inLen)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    output += NEWLINE;
                }

                if (inLen - c - 1 < numCols)
                {
                    output += input.Substring(c, inLen - c);
                    break;
                }

                len = numCols;

                while ((len > minLength) &&
                       (input[c + len] != ' '))
                {
                    len--;
                }

                if (len == minLength)
                {
                    len = numCols;
                }

                output += input.Substring(c, len);
                c += len;
                if (c < input.Length)
                {
                    while ((input[c] == ' ') && (c < input.Length))
                    {
                        c++;
                    }
                }
            }

            return output;
        }

        public static int stringCompare(string s1, string s2)
        {
            return string.CompareOrdinal(s1, s2);
        }

        public static string toUpper(string x)
        {
            if (x == null)
            {
                throw new ArgumentNullException("x");
            }

            return x.ToUpperInvariant();
        }

        public static string toLower(string x)
        {
            if (x == null)
            {
                throw new ArgumentNullException("x");
            }

            return x.ToLowerInvariant();
        }

        public static Array<string> stringSplit(string x, char splitChar)
        {
            if (x == null)
            {
                throw new ArgumentNullException("x");
            }

            int prev;
            int cur = 0;
            Array<string> outArray = new Array<string>();

            while (cur < x.Length)
            {
                prev = cur;
                cur = x.IndexOf(splitChar, prev + 1);

                if (cur == -1)
                {
                    cur = x.Length;
                }

                outArray.append(x.Substring(prev, cur - prev));
                ++cur;
            }

            return outArray;
        }

        public static string stringJoin(Array<string> a, char joinChar)
        {
            return stringJoin(a, joinChar.ToString(CultureInfo.InvariantCulture));
        }

        public static string stringJoin(Array<string> a, string joinStr)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }
            if (joinStr == null)
            {
                throw new ArgumentNullException("joinStr");
            }

            string outString = "";

            for (int i = 0; i < a.size() - 1; ++i)
            {
                outString += a[i] + joinStr;
            }

            if (a.size() > 0)
            {
                return outString + a.last();
            }
            else
            {
                return outString;
            }
        }

        public static string trimWhitespace(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            int left = 0;

            while ((left < s.Length) && isWhiteSpace(s[left]))
            {
                ++left;
            }

            int right = s.Length - 1;
            while ((right > left) && isWhiteSpace(s[right]))
            {
                --right;
            }

            return s.Substring(left, right - left + 1);
        }

        public static bool isWhiteSpace(char c)
        {
            return char.IsWhiteSpace(c);
        }

        public static bool isNewline(char c)
        {
            return (c == '\n') || (c == '\r');
        }

        public static bool isDigit(char c)
        {
            return char.IsDigit(c);
        }

        public static bool isLetter(char c)
        {
            return char.IsLetter(c);
        }

        public static bool isSlash(char c)
        {
            return (c == '\\') || (c == '/');
        }

        public static bool isQuote(char c)
        {
            return (c == '\'') || (c == '\"');
        }
    }
}

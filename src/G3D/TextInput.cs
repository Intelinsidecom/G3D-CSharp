/**
 @file TextInput.h

 Simple text lexer/tokenizer.

 @maintainer Morgan McGuire, morgan@graphics3d.com

 @cite Based on a lexer written by Aaron Orenstein.

 @created 2002-11-27
 @edited  2005-09-24

 Copyright 2000-2005, Morgan McGuire.
 All rights reserved.
 */

using System;
using global::System.IO;
using global::System.Text;
using global::System.Globalization;

namespace G3D
{
    public class Token
    {
        public enum Type { STRING, SYMBOL, NUMBER, END }
        public enum ExtendedType { SINGLE_QUOTED_TYPE, DOUBLE_QUOTED_TYPE, SYMBOL_TYPE, INTEGER_TYPE, FLOATING_POINT_TYPE, END_TYPE }

        public string _string;
        public int _line;
        public int _character;
        public Type _type;
        public ExtendedType _extendedType;

        public Token()
        {
            _string = "";
            _line = 0;
            _character = 0;
            _type = Type.END;
            _extendedType = ExtendedType.END_TYPE;
        }

        public Token(Type t, ExtendedType e, string s, int line, int character)
        {
            _string = s;
            _line = line;
            _character = character;
            _type = t;
            _extendedType = e;
        }

        public Type type
        {
            get { return _type; }
        }

        public ExtendedType extendedType
        {
            get { return _extendedType; }
        }

        public string str
        {
            get { return _string; }
        }

        public int line
        {
            get { return _line; }
        }

        public int character
        {
            get { return _character; }
        }

        public double number()
        {
            if (_type == Type.NUMBER)
            {
                if (_string == "-1.#IND00")
                {
                    return g3dmath.nan();
                }

                if (_string == "1.#INF00")
                {
                    return g3dmath.inf();
                }

                if (_string == "-1.#INF00")
                {
                    return -g3dmath.inf();
                }

                double n = 0.0;
                if (_string.Length > 2 && _string[0] == '0' && _string[1] == 'x')
                {
                    uint i;
                    if (uint.TryParse(_string.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out i))
                    {
                        n = i;
                    }
                }
                else
                {
                    double.TryParse(_string, NumberStyles.Float, CultureInfo.InvariantCulture, out n);
                }
                return n;
            }

            return 0.0;
        }
    }

    public class TextInput
    {
        public class Settings
        {
            public bool cComments;
            public bool cppComments;
            public bool escapeSequencesInStrings;
            public char otherCommentCharacter;
            public char otherCommentCharacter2;
            public bool signedNumbers;
            public bool singleQuotedStrings;
            public string sourceFileName;
            public int startingLineNumberOffset;
            public bool msvcSpecials;

            public Settings()
            {
                cComments = true;
                cppComments = true;
                escapeSequencesInStrings = true;
                otherCommentCharacter = '\0';
                otherCommentCharacter2 = '\0';
                signedNumbers = true;
                singleQuotedStrings = true;
                sourceFileName = "";
                startingLineNumberOffset = 0;
                msvcSpecials = false;
            }
        }

        public class TokenException : Exception
        {
            public string sourceFile;
            public int line;
            public int character;
            public string message;

            public TokenException(string src, int ln, int ch)
            {
                sourceFile = src;
                line = ln;
                character = ch;
                message = Format.format("%s(%d) : ", sourceFile, line);
            }
        }

        public class BadMSVCSpecial : TokenException
        {
            public BadMSVCSpecial(string src, int ln, int ch) : base(src, ln, ch)
            {
            }
        }

        public class WrongTokenType : TokenException
        {
            public Token.Type expected;
            public Token.Type actual;

            public WrongTokenType(string src, int ln, int ch, Token.Type e, Token.Type a)
                : base(src, ln, ch)
            {
                expected = e;
                actual = a;
                message += Format.format("Expected token of type {0}, found type {1}.", TokenTypeToString(e), TokenTypeToString(a));
            }
        }

        public class WrongSymbol : TokenException
        {
            public string expected;
            public string actual;

            public WrongSymbol(string src, int ln, int ch, string e, string a)
                : base(src, ln, ch)
            {
                expected = e;
                actual = a;
                message += Format.format("Expected symbol '{0}', found symbol '{1}'.", e, a);
            }
        }

        public class WrongString : TokenException
        {
            public string expected;
            public string actual;

            public WrongString(string src, int ln, int ch, string e, string a)
                : base(src, ln, ch)
            {
                expected = e;
                actual = a;
                message += Format.format("Expected string '{0}', found string '{1}'.", e, a);
            }
        }

        private Queue<Token> stack;
        private Array<char> buffer;
        private uint currentCharOffset;
        private uint lineNumber;
        private uint charNumber;
        private Settings options;

        private void init()
        {
            currentCharOffset = 0;
            charNumber = 1;
            lineNumber = (uint)(1 + options.startingLineNumberOffset);
        }

        private int eatInputChar()
        {
            if (currentCharOffset >= (uint)buffer.size())
            {
                return -1;
            }

            char c = buffer[(int)currentCharOffset];
            ++currentCharOffset;

            if (c == '\n')
            {
                ++lineNumber;
                charNumber = 1;
            }
            else
            {
                ++charNumber;
            }

            return c;
        }

        private int peekInputChar(uint distance)
        {
            if (currentCharOffset + distance >= (uint)buffer.size())
            {
                return -1;
            }

            return buffer[(int)(currentCharOffset + distance)];
        }

        private int eatAndPeekInputChar()
        {
            eatInputChar();
            return peekInputChar(0);
        }

        public TextInput(string filename) : this(filename, new Settings())
        {
        }

        public TextInput(string filename, Settings settings)
        {
            options = settings;
            stack = new Queue<Token>();
            init();

            if (options.sourceFileName == "")
            {
                options.sourceFileName = filename;
            }

            byte[] bytes = File.ReadAllBytes(filename);
            char[] chars = Encoding.Default.GetChars(bytes);
            buffer = new Array<char>(chars.Length);
            for (int i = 0; i < chars.Length; ++i)
            {
                buffer[i] = chars[i];
            }
        }

        public enum FS { FROM_STRING }

        public TextInput(FS fs, string str) : this(fs, str, new Settings())
        {
        }

        public TextInput(FS fs, string str, Settings settings)
        {
            options = settings;
            stack = new Queue<Token>();
            init();

            if (options.sourceFileName == "")
            {
                if (str.Length < 14)
                {
                    options.sourceFileName = "\"" + str + "\"";
                }
                else
                {
                    options.sourceFileName = "\"" + str.Substring(0, 10) + "...\"";
                }
            }

            buffer = new Array<char>(str.Length);
            for (int i = 0; i < str.Length; ++i)
            {
                buffer[i] = str[i];
            }
        }

        private Token nextToken()
        {
            Token t = new Token();
            t = new Token(Token.Type.END, Token.ExtendedType.END_TYPE, "", (int)lineNumber, (int)charNumber);

            int c = peekInputChar(0);
            if (c == -1)
            {
                return t;
            }

            bool whitespaceDone = false;
            while (!whitespaceDone)
            {
                whitespaceDone = true;

                while (StringUtils.isWhiteSpace((char)c))
                {
                    c = eatAndPeekInputChar();
                }

                int c2 = peekInputChar(1);
                if ((options.cppComments && c == '/' && c2 == '/') ||
                    (options.otherCommentCharacter != '\0' && c == options.otherCommentCharacter) ||
                    (options.otherCommentCharacter2 != '\0' && c == options.otherCommentCharacter2))
                {
                    do
                    {
                        c = eatAndPeekInputChar();
                    } while (!StringUtils.isNewline((char)c) && c != -1);

                    whitespaceDone = false;
                }
                else if (options.cComments && c == '/' && c2 == '*')
                {
                    eatInputChar();
                    eatInputChar();

                    c = peekInputChar(0);
                    c2 = peekInputChar(1);
                    while (!(c == '*' && c2 == '/') && c != -1)
                    {
                        eatInputChar();
                        c = c2;
                        c2 = peekInputChar(1);
                    }
                    eatInputChar();
                    eatInputChar();

                    c = peekInputChar(0);
                    whitespaceDone = false;
                }
            }

            t = new Token(Token.Type.END, Token.ExtendedType.END_TYPE, "", (int)lineNumber, (int)charNumber);

            if (c == -1)
            {
                return t;
            }

            switch (c)
            {
                case '@':
                case '(':
                case ')':
                case ',':
                case ';':
                case '{':
                case '}':
                case '[':
                case ']':
                case '#':
                case '$':
                case '?':
                    SetupSymbol(t, ref c);
                    return t;

                case '-':
                    SetupSymbol(t, ref c);
                    switch (c)
                    {
                        case '>':
                        case '-':
                        case '=':
                            t._string += (char)c;
                            eatInputChar();
                            return t;
                    }

                    if (options.signedNumbers && (StringUtils.isDigit((char)c) || (c == '.' && StringUtils.isDigit((char)peekInputChar(1)))))
                    {
                        ParseNumber(t, ref c);
                        return t;
                    }
                    return t;

                case '+':
                    SetupSymbol(t, ref c);
                    switch (c)
                    {
                        case '+':
                        case '=':
                            t._string += (char)c;
                            eatInputChar();
                            return t;
                    }

                    if (options.signedNumbers && (StringUtils.isDigit((char)c) || (c == '.' && StringUtils.isDigit((char)peekInputChar(1)))))
                    {
                        ParseNumber(t, ref c);
                        return t;
                    }
                    return t;

                case ':':
                    SetupSymbol(t, ref c);
                    if (c == ':')
                    {
                        t._string += (char)c;
                        eatInputChar();
                    }
                    return t;

                case '*':
                case '/':
                case '!':
                case '~':
                case '=':
                case '^':
                    SetupSymbol(t, ref c);
                    if (c == '=')
                    {
                        t._string += (char)c;
                        eatInputChar();
                    }
                    return t;

                case '>':
                case '<':
                case '|':
                case '&':
                    {
                        int origC = c;
                        SetupSymbol(t, ref c);
                        if (c == '=' || origC == c)
                        {
                            t._string += (char)c;
                            eatInputChar();
                        }
                    }
                    return t;

                case '\\':
                    SetupSymbol(t, ref c);
                    if ((options.otherCommentCharacter != '\0' && c == options.otherCommentCharacter) ||
                        (options.otherCommentCharacter2 != '\0' && c == options.otherCommentCharacter2))
                    {
                        t._string = ((char)c).ToString();
                        eatInputChar();
                    }
                    return t;

                case '.':
                    if (StringUtils.isDigit((char)peekInputChar(1)))
                    {
                        ParseNumber(t, ref c);
                        return t;
                    }

                    SetupSymbol(t, ref c);
                    if (c == '.')
                    {
                        t._string += (char)c;
                        c = eatAndPeekInputChar();
                        if (c == '.')
                        {
                            t._string += (char)c;
                            eatInputChar();
                        }
                    }
                    return t;
            }

            if (StringUtils.isDigit((char)c) || c == '.')
            {
                ParseNumber(t, ref c);
                return t;
            }
            else if (StringUtils.isLetter((char)c) || c == '_')
            {
                t._type = Token.Type.SYMBOL;
                t._extendedType = Token.ExtendedType.SYMBOL_TYPE;
                t._string = "";
                do
                {
                    t._string += (char)c;
                    c = eatAndPeekInputChar();
                } while (StringUtils.isLetter((char)c) || StringUtils.isDigit((char)c) || c == '_');
                return t;
            }
            else if (c == '"')
            {
                eatInputChar();
                ParseQuotedString('"', t);
                return t;
            }
            else if (c == '\'')
            {
                eatInputChar();
                if (options.singleQuotedStrings)
                {
                    ParseQuotedString('\'', t);
                }
                else
                {
                    t._string = ((char)c).ToString();
                    t._type = Token.Type.SYMBOL;
                    t._extendedType = Token.ExtendedType.SYMBOL_TYPE;
                }
                return t;
            }

            if (c == -1)
            {
                t._type = Token.Type.END;
                t._extendedType = Token.ExtendedType.END_TYPE;
                t._string = "";
                return t;
            }

            Debug.debugAssert(false);
            return t;
        }

        private void SetupSymbol(Token t, ref int c)
        {
            t._type = Token.Type.SYMBOL;
            t._extendedType = Token.ExtendedType.SYMBOL_TYPE;
            t._string = ((char)c).ToString();
            c = eatAndPeekInputChar();
        }

        private void ParseNumber(Token t, ref int c)
        {
            if (t._string != "-")
            {
                t._string = "";
            }
            t._type = Token.Type.NUMBER;
            if (c == '.')
            {
                t._extendedType = Token.ExtendedType.FLOATING_POINT_TYPE;
            }
            else
            {
                t._extendedType = Token.ExtendedType.INTEGER_TYPE;
            }

            if (c == '0' && peekInputChar(1) == 'x')
            {
                t._string += "0x";
                eatInputChar();
                eatInputChar();
                c = peekInputChar(0);
                while (StringUtils.isDigit((char)c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'))
                {
                    t._string += (char)c;
                    c = eatAndPeekInputChar();
                }
            }
            else
            {
                while (StringUtils.isDigit((char)c))
                {
                    t._string += (char)c;
                    c = eatAndPeekInputChar();
                }

                bool isSpecial = false;
                if (c == '.')
                {
                    t._extendedType = Token.ExtendedType.FLOATING_POINT_TYPE;
                    t._string += (char)c;
                    c = eatAndPeekInputChar();

                    if (options.msvcSpecials && c == '#')
                    {
                        isSpecial = true;
                        c = eatAndPeekInputChar();
                        if (c != 'I')
                        {
                            throw new BadMSVCSpecial("Incorrect floating-point special (inf or nan) format.", (int)lineNumber, (int)charNumber);
                        }
                        c = eatAndPeekInputChar();
                        if (c != 'N')
                        {
                            throw new BadMSVCSpecial("Incorrect floating-point special (inf or nan) format.", (int)lineNumber, (int)charNumber);
                        }
                        t._string += "#IN";
                        c = eatAndPeekInputChar();
                        if (c != 'F' && c != 'D')
                        {
                            throw new BadMSVCSpecial("Incorrect floating-point special (inf or nan) format.", (int)lineNumber, (int)charNumber);
                        }
                        t._string += (char)c;
                        for (int j = 0; j < 2; ++j)
                        {
                            c = eatAndPeekInputChar();
                            if (c != '0')
                            {
                                throw new BadMSVCSpecial("Incorrect floating-point special (inf or nan) format.", (int)lineNumber, (int)charNumber);
                            }
                            t._string += (char)c;
                        }
                    }
                    else
                    {
                        while (StringUtils.isDigit((char)c))
                        {
                            t._string += (char)c;
                            c = eatAndPeekInputChar();
                        }
                    }
                }

                if (!isSpecial && (c == 'e' || c == 'E'))
                {
                    t._extendedType = Token.ExtendedType.FLOATING_POINT_TYPE;
                    t._string += (char)c;
                    c = eatAndPeekInputChar();
                    if (c == '-' || c == '+')
                    {
                        t._string += (char)c;
                        c = eatAndPeekInputChar();
                    }
                    while (StringUtils.isDigit((char)c))
                    {
                        t._string += (char)c;
                        c = eatAndPeekInputChar();
                    }
                }
            }
        }

        private void ParseQuotedString(char delimiter, Token t)
        {
            if (delimiter == '\'')
            {
                t._extendedType = Token.ExtendedType.SINGLE_QUOTED_TYPE;
            }
            else
            {
                t._extendedType = Token.ExtendedType.DOUBLE_QUOTED_TYPE;
            }
            t._type = Token.Type.STRING;

            while (true)
            {
                int c = eatInputChar();
                if (c == -1)
                {
                    break;
                }

                if (options.escapeSequencesInStrings && c == '\\')
                {
                    c = eatInputChar();
                    switch (c)
                    {
                        case 'r':
                            t._string += '\r';
                            break;
                        case 'n':
                            t._string += '\n';
                            break;
                        case 't':
                            t._string += '\t';
                            break;
                        case '0':
                            t._string += '\0';
                            break;
                        case '\\':
                        case '"':
                        case '\'':
                            t._string += (char)c;
                            break;
                        default:
                            if ((c == options.otherCommentCharacter && options.otherCommentCharacter != '\0') ||
                                (c == options.otherCommentCharacter2 && options.otherCommentCharacter2 != '\0'))
                            {
                                t._string += (char)c;
                            }
                            break;
                    }
                }
                else if (c == delimiter)
                {
                    break;
                }
                else
                {
                    t._string += (char)c;
                }
            }
        }

        public Token read()
        {
            if (stack.size() > 0)
            {
                Token t = stack[0];
                stack.popFront();
                return t;
            }
            return nextToken();
        }

        public void push(Token t)
        {
            stack.pushFront(t);
        }

        public bool hasMore()
        {
            return peek().type != Token.Type.END;
        }

        public Token peek()
        {
            if (stack.size() == 0)
            {
                Token t = nextToken();
                push(t);
            }
            return stack[0];
        }

        public int peekLineNumber()
        {
            return peek().line;
        }

        public int peekCharacterNumber()
        {
            return peek().character;
        }

        public double readNumber()
        {
            Token t = read();
            if (t.type == Token.Type.NUMBER)
            {
                return t.number();
            }

            if (!options.signedNumbers && t.type == Token.Type.SYMBOL && (t.str == "-" || t.str == "+"))
            {
                Token t2 = read();
                if (t2.type == Token.Type.NUMBER && t2.character == t.character + 1)
                {
                    if (t.str == "-")
                    {
                        return -t2.number();
                    }
                    return t2.number();
                }
                push(t2);
            }

            push(t);
            throw new WrongTokenType(options.sourceFileName, t.line, t.character, Token.Type.NUMBER, t.type);
        }

        public Token readStringToken()
        {
            Token t = read();
            if (t.type == Token.Type.STRING)
            {
                return t;
            }
            push(t);
            throw new WrongTokenType(options.sourceFileName, t.line, t.character, Token.Type.STRING, t.type);
        }

        public string readString()
        {
            return readStringToken().str;
        }

        public void readString(string s)
        {
            Token t = readStringToken();
            if (t.str == s)
            {
                return;
            }
            push(t);
            throw new WrongString(options.sourceFileName, t.line, t.character, s, t.str);
        }

        public Token readSymbolToken()
        {
            Token t = read();
            if (t.type == Token.Type.SYMBOL)
            {
                return t;
            }
            push(t);
            throw new WrongTokenType(options.sourceFileName, t.line, t.character, Token.Type.SYMBOL, t.type);
        }

        public string readSymbol()
        {
            return readSymbolToken().str;
        }

        public void readSymbol(string symbol)
        {
            Token t = readSymbolToken();
            if (t.str == symbol)
            {
                return;
            }
            push(t);
            throw new WrongSymbol(options.sourceFileName, t.line, t.character, symbol, t.str);
        }

        public void readSymbols(string s1, string s2)
        {
            readSymbol(s1);
            readSymbol(s2);
        }

        public void readSymbols(string s1, string s2, string s3)
        {
            readSymbol(s1);
            readSymbol(s2);
            readSymbol(s3);
        }

        public void readSymbols(string s1, string s2, string s3, string s4)
        {
            readSymbol(s1);
            readSymbol(s2);
            readSymbol(s3);
            readSymbol(s4);
        }

        public string filename()
        {
            return options.sourceFileName;
        }

        private static string TokenTypeToString(Token.Type t)
        {
            switch (t)
            {
                case Token.Type.SYMBOL:
                    return "Token::SYMBOL";
                case Token.Type.STRING:
                    return "Token::STRING";
                case Token.Type.NUMBER:
                    return "Token::NUMBER";
                case Token.Type.END:
                    return "Token::END";
                default:
                    Debug.debugAssertM(false, "Fell through switch");
                    return "?";
            }
        }

        public static void deserialize(ref bool b, TextInput ti)
        {
            b = ti.readSymbol() == "true";
        }

        public static void deserialize(ref int b, TextInput ti)
        {
            b = g3dmath.iRound(ti.readNumber());
        }

        public static void deserialize(ref byte b, TextInput ti)
        {
            b = (byte)g3dmath.iRound(ti.readNumber());
        }

        public static void deserialize(ref double b, TextInput ti)
        {
            b = ti.readNumber();
        }

        public static void deserialize(ref float b, TextInput ti)
        {
            b = (float)ti.readNumber();
        }

        public static void deserialize(ref string b, TextInput ti)
        {
            b = ti.readString();
        }
    }
}

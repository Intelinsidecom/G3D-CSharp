/**
  @file TextOutput.h

  @maintainer Morgan McGuire, morgan@graphics3d.com
  @created 2004-06-21
  @edited  2005-11-05

  Copyright 2000-2006, Morgan McGuire.
  All rights reserved.
 */

using System;
using global::System.IO;
using global::System.Text;

namespace G3D
{
    public class TextOutput
    {
        public class Options
        {
            public enum WordWrapMode { WRAP_NONE, WRAP_WITHOUT_BREAKING, WRAP_ALWAYS }
            public enum NewlineStyle { NEWLINE_WINDOWS, NEWLINE_UNIX }

            public WordWrapMode wordWrap;
            public bool allowWordWrapInsideDoubleQuotes;
            public int numColumns;
            public int spacesPerIndent;
            public NewlineStyle newlineStyle;
            public bool convertNewlines;

            public Options()
            {
                wordWrap = WordWrapMode.WRAP_WITHOUT_BREAKING;
                allowWordWrapInsideDoubleQuotes = false;
                numColumns = 80;
                spacesPerIndent = 4;
                convertNewlines = true;
#if DEBUG
                newlineStyle = NewlineStyle.NEWLINE_WINDOWS;
#else
                newlineStyle = NewlineStyle.NEWLINE_WINDOWS;
#endif
            }
        }

        private bool startingNewLine;
        private int currentColumn;
        private bool inDQuote;
        private string filename;
        private Array<char> data;
        private Options option;
        private int indentLevel;
        private int indentSpaces;
        private string newline;

        private void setIndentLevel(int i)
        {
            indentLevel = i;

            if (indentLevel < 0)
            {
                indentLevel = 0;
            }

            indentSpaces = g3dmath.iClamp(option.spacesPerIndent * indentLevel, 0, option.numColumns - 1);
        }

        private void setOptions(Options opt)
        {
            option = opt;
            Debug.debugAssert(option.numColumns > 1);

            setIndentLevel(indentLevel);

            newline = (option.newlineStyle == Options.NewlineStyle.NEWLINE_WINDOWS) ? "\r\n" : "\n";
        }

        public TextOutput(Options options)
        {
            setOptions(options);
            filename = "";
            setIndentLevel(0);
            startingNewLine = true;
            currentColumn = 0;
            inDQuote = false;
            data = new Array<char>();
        }

        public TextOutput(string filename, Options options)
        {
            setOptions(options);
            this.filename = filename;
            setIndentLevel(0);
            startingNewLine = true;
            currentColumn = 0;
            inDQuote = false;
            data = new Array<char>();
        }

        public void pushIndent()
        {
            setIndentLevel(indentLevel + 1);
        }

        public void popIndent()
        {
            setIndentLevel(indentLevel - 1);
        }

        private static string escape(string text)
        {
            string result = "";

            for (int i = 0; i < text.Length; ++i)
            {
                char c = text[i];
                switch (c)
                {
                    case '\0':
                        result += "\\0";
                        break;
                    case '\r':
                        result += "\\r";
                        break;
                    case '\n':
                        result += "\\n";
                        break;
                    case '\t':
                        result += "\\t";
                        break;
                    case '\\':
                        result += "\\\\";
                        break;
                    default:
                        result += c;
                        break;
                }
            }

            return result;
        }

        public void writeString(string text)
        {
            printf("\"%s\"", escape(text));
        }

        public void writeNumber(double n)
        {
            printf("%f ", n);
        }

        public void writeNumber(int n)
        {
            printf("%d ", n);
        }

        public void writeSymbol(string text)
        {
            if (text.Length > 0)
            {
                printf("%s ", text);
            }
        }

        public void writeSymbols(string a, string b, string c, string d, string e, string f)
        {
            writeSymbol(a);
            writeSymbol(b);
            writeSymbol(c);
            writeSymbol(d);
            writeSymbol(e);
            writeSymbol(f);
        }

        public void printf(string format, params object[] args)
        {
            vprintf(format, args);
        }

        public void vprintf(string format, object[] argPtr)
        {
            string text = Format.vformat(format, argPtr);
            string clean;
            convertNewlines(text, out clean);
            wordWrapIndentAppend(clean);
        }

        private void convertNewlines(string input, out string output)
        {
            if (option.convertNewlines)
            {
                output = "";
                for (int i = 0; i < input.Length; ++i)
                {
                    if (input[i] == '\n')
                    {
                        output += newline;
                    }
                    else if (input[i] == '\r' && i + 1 < input.Length && input[i + 1] == '\n')
                    {
                        output += newline;
                        ++i;
                    }
                    else
                    {
                        output += input[i];
                    }
                }
            }
            else
            {
                output = input;
            }
        }

        public void writeNewline()
        {
            for (int i = 0; i < newline.Length; ++i)
            {
                indentAppend(newline[i]);
            }
        }

        public void writeNewlines(int numLines)
        {
            for (int i = 0; i < numLines; ++i)
            {
                writeNewline();
            }
        }

        private void wordWrapIndentAppend(string text)
        {
            if (option.wordWrap == Options.WordWrapMode.WRAP_NONE ||
                currentColumn + text.Length <= option.numColumns)
            {
                for (int i = 0; i < text.Length; ++i)
                {
                    indentAppend(text[i]);
                }
                return;
            }

            int cols = option.numColumns - indentSpaces;

            for (int i = 0; i < text.Length; ++i)
            {
                indentAppend(text[i]);

                if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    ++i;
                    indentAppend(text[i]);
                }

                if (currentColumn >= cols)
                {
                    Debug.debugAssertM(text[i] != '\n' && text[i] != '\r', "Should never enter word-wrapping on a newline character");

                    bool unquotedSpace = option.allowWordWrapInsideDoubleQuotes || !inDQuote;

                    int lastSpace = data.size() - 1;
                    int k = 0;
                    int maxLookBackward = currentColumn - indentSpaces;

                    while (k < maxLookBackward && lastSpace > 0 && !(((data[lastSpace] == ' ') && unquotedSpace)))
                    {
                        --lastSpace;
                        ++k;

                        if (data[lastSpace] == '"' && !option.allowWordWrapInsideDoubleQuotes)
                        {
                            unquotedSpace = !unquotedSpace;
                        }
                    }

                    if (k == maxLookBackward)
                    {
                        if (option.wordWrap == Options.WordWrapMode.WRAP_ALWAYS)
                        {
                            data.pop();
                            writeNewline();
                            indentAppend(text[i]);
                        }
                    }
                    else
                    {
                        int firstSpace = lastSpace;
                        while (k < maxLookBackward && firstSpace > 0 && data[firstSpace] == ' ')
                        {
                            --firstSpace;
                            ++k;
                        }

                        if (k == maxLookBackward)
                        {
                            ++firstSpace;
                        }

                        if (lastSpace == data.size() - 1)
                        {
                            data.resize(firstSpace + 1);
                            writeNewline();

                            while (i < text.Length - 1 && text[i + 1] == ' ')
                            {
                                ++i;
                            }
                        }
                        else
                        {
                            Array<char> temp = new Array<char>();
                            for (int j = lastSpace + 1; j < data.size(); ++j)
                            {
                                char c = data[j];

                                if (c == '"')
                                {
                                    inDQuote = !inDQuote;
                                }
                                temp.append(c);
                            }

                            data.resize(firstSpace + 1);
                            writeNewline();

                            for (int j = 0; j < temp.size(); ++j)
                            {
                                indentAppend(temp[j]);
                            }
                        }
                    }
                }
            }
        }

        private void indentAppend(char c)
        {
            if (startingNewLine)
            {
                for (int j = 0; j < indentSpaces; ++j)
                {
                    data.push(' ');
                }
                startingNewLine = false;
                currentColumn = indentSpaces;
            }

            data.push(c);

            if (c != '\r')
            {
                ++currentColumn;
            }

            if (c == '"')
            {
                inDQuote = !inDQuote;
            }

            startingNewLine = (c == '\n');
            if (startingNewLine)
            {
                currentColumn = 0;
            }
        }

        public void commit(bool flush)
        {
            if (filename == "")
            {
                throw new InvalidOperationException("TextOutput filename is empty.");
            }

            string directory = FileUtils.filenamePath(filename);
            if (directory != "")
            {
                FileUtils.createDirectory(directory);
            }

            File.WriteAllBytes(filename, Encoding.ASCII.GetBytes(data.ToArray()));
        }

        public void commitString(ref string output)
        {
            char[] chars = data.ToArray();
            output = new string(chars, 0, data.size());
        }

        public string commitString()
        {
            char[] chars = data.ToArray();
            return new string(chars, 0, data.size());
        }

        public static void serialize(bool b, TextOutput to)
        {
            to.writeSymbol(b ? "true" : "false");
        }

        public static void serialize(int b, TextOutput to)
        {
            to.writeNumber(b);
        }

        public static void serialize(uint b, TextOutput to)
        {
            to.writeNumber((int)b);
        }

        public static void serialize(double b, TextOutput to)
        {
            to.writeNumber(b);
        }

        public static void serialize(float b, TextOutput to)
        {
            to.writeNumber(b);
        }

        public static void serialize(string b, TextOutput to)
        {
            to.writeString(b);
        }
    }
}

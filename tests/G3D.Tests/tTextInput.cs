using System;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private static void checkExcPos(TextInput.TokenException e, int lnum, int chnum)
        {
            Debug.alwaysAssertM(e.line == lnum && e.character == chnum, "");
        }

        private static void checkTokenPos(Token t, int lnum, int chnum)
        {
            Debug.alwaysAssertM(t.line == lnum
                          && t.character == chnum, "");
        }

        private static void checkTokenType(Token t, Token.Type typ, Token.ExtendedType etyp)
        {
            Debug.alwaysAssertM(t.type == typ, "");
            Debug.alwaysAssertM(t.extendedType == etyp, "");
        }

        private static void checkSymToken(TextInput ti, string str, int lnum, int chnum)
        {
            Token _t;
            _t = ti.read();
            checkTokenType(_t, Token.Type.SYMBOL, Token.ExtendedType.SYMBOL_TYPE);

            checkTokenPos(_t, lnum, chnum);
            Debug.alwaysAssertM(_t.str == str, "");
        }

        private static void checkEndToken(TextInput ti, int lnum, int chnum)
        {
            Token _t;
            _t = ti.read();
            checkTokenType(_t, Token.Type.END, Token.ExtendedType.END_TYPE);
            checkTokenPos(_t, lnum, chnum);
        }

        private static void checkOneSpecialSym(string s)
        {
            TextInput ti = new TextInput(TextInput.FS.FROM_STRING, "\n a" + s + "b\n ");
            checkSymToken(ti, "a", 2, 2);
            checkSymToken(ti, s, 2, 3);
            checkSymToken(ti, "b", 2, 3 + s.Length);
            checkEndToken(ti, 3, 2);
        }

        private static void testTextInput()
        {
            printf("TextInput\n");

            {
                // Parse floats
                TextInput ti = new TextInput(TextInput.FS.FROM_STRING, ".1");
                Debug.debugAssert(ti.readNumber() == 0.1);
            }
            {
                TextInput ti = new TextInput(TextInput.FS.FROM_STRING, "..1");
                Debug.debugAssert(ti.readSymbol() == "..");
                Debug.debugAssert(ti.readNumber() == 1);
            }

            {
                // Quoted string with escapes.  The actual expression we are parsing looks like:
                // "\\"
                TextInput ti = new TextInput(TextInput.FS.FROM_STRING, "\"\\\\\"");
                Token t;

                ti.readString("\\");

                t = ti.read();
                Debug.debugAssert(t.type == Token.Type.END);
                Debug.debugAssert(!ti.hasMore());
            }

            {
                // Quoted string without escapes: read as two backslashes
                // (the test itself has to escape the backslashes, just to write them).
                // The actual expression we are parsing is:
                // "\"
                TextInput.Settings opt = new TextInput.Settings();
                Token t;

                opt.escapeSequencesInStrings = false;
                TextInput ti = new TextInput(TextInput.FS.FROM_STRING, "\"\\\"", opt);
                ti.readString("\\");

                t = ti.read();
                Debug.debugAssert(t.type == Token.Type.END);
                Debug.debugAssert(!ti.hasMore());
            }

            {
                TextInput ti = new TextInput(TextInput.FS.FROM_STRING, "a \'foo\' bar");
                Token t;

                ti.readSymbol("a");

                t = ti.read();
                Debug.debugAssert(t.extendedType == Token.ExtendedType.SINGLE_QUOTED_TYPE);
                Debug.debugAssert(t.str == "foo");
                ti.readSymbol("bar");

                t = ti.read();
                Debug.debugAssert(t.type == Token.Type.END);
                Debug.debugAssert(!ti.hasMore());
            }

            {
                TextInput ti = new TextInput(TextInput.FS.FROM_STRING, "2.x");
                Token t;

                Debug.debugAssert(ti.readNumber() == 2);
                ti.readSymbol("x");

                t = ti.read();
                Debug.debugAssert(t.type == Token.Type.END);
                Debug.debugAssert(!ti.hasMore());
            }
            {
                TextInput ti = new TextInput(TextInput.FS.FROM_STRING, "1.E7");
                Token t;

                Debug.debugAssert(ti.readNumber() == 1e7);

                t = ti.read();
                Debug.debugAssert(t.type == Token.Type.END);
                Debug.debugAssert(!ti.hasMore());
            }

            {
                TextInput ti = new TextInput(TextInput.FS.FROM_STRING, "\\123");
                Token t;
                t = ti.read();
                Debug.debugAssert(t.type == Token.Type.SYMBOL);
                Debug.debugAssert(t.str == "\\");
                t = ti.read();
                Debug.debugAssert(t.type == Token.Type.NUMBER);
                Debug.debugAssert(t.number() == 123);

                t = ti.read();
                Debug.debugAssert(t.type == Token.Type.END);
                Debug.debugAssert(!ti.hasMore());
            }

            {
                TextInput.Settings options = new TextInput.Settings();
                options.otherCommentCharacter = '#';

                TextInput ti = new TextInput(TextInput.FS.FROM_STRING, "1#23\nA\\#2", options);
                Token t;
                t = ti.read();
                Debug.debugAssert(t.type == Token.Type.NUMBER);
                Debug.debugAssert(t.number() == 1);

                // skip the comment
                t = ti.read();
                Debug.debugAssert(t.type == Token.Type.SYMBOL);
                Debug.debugAssert(t.str == "A");

                // Read escaped comment character
                t = ti.read();
                Debug.debugAssert(t.type == Token.Type.SYMBOL);
                Debug.debugAssert(t.str == "#");

                t = ti.read();
                Debug.debugAssert(t.type == Token.Type.NUMBER);
                Debug.debugAssert(t.number() == 2);

                t = ti.read();
                Debug.debugAssert(t.type == Token.Type.END);
                Debug.debugAssert(!ti.hasMore());
            }

            {
                TextInput ti = new TextInput(TextInput.FS.FROM_STRING, "0xFEED");

                Token t;

                t = ti.peek();
                Debug.debugAssert(t.type == Token.Type.NUMBER);
                double n = ti.readNumber();
                Debug.debugAssert((int)n == 0xFEED);

                t = ti.read();
                Debug.debugAssert(t.type == Token.Type.END);
                Debug.debugAssert(!ti.hasMore());
            }

            {

                TextInput.Settings opt = new TextInput.Settings();
                opt.cppComments = false;
                TextInput ti = new TextInput(TextInput.FS.FROM_STRING,
                             "if/*comment*/(x->y==-1e6){cout<<\"hello world\"}; // foo\nbar",
                             opt);

                Token a = ti.read();
                Token b = ti.read();
                Token c = ti.read();
                Token d = ti.read();
                Token e = ti.read();
                Token f = ti.read();
                double g = ti.readNumber();
                Token h = ti.read();
                Token i = ti.read();
                Token j = ti.read();
                Token k = ti.read();
                Token L = ti.read();
                Token m = ti.read();
                Token n = ti.read();
                Token p = ti.read();
                Token q = ti.read();
                Token r = ti.read();
                Token s = ti.read();
                Token t = ti.read();

                Debug.debugAssert(a.type == Token.Type.SYMBOL);
                Debug.debugAssert(a.str == "if");

                Debug.debugAssert(b.type == Token.Type.SYMBOL);
                Debug.debugAssert(b.str == "(");

                Debug.debugAssert(c.type == Token.Type.SYMBOL);
                Debug.debugAssert(c.str == "x");

                Debug.debugAssert(d.type == Token.Type.SYMBOL);
                Debug.debugAssert(d.str == "->");

                Debug.debugAssert(e.type == Token.Type.SYMBOL);
                Debug.debugAssert(e.str == "y");

                Debug.debugAssert(f.type == Token.Type.SYMBOL);
                Debug.debugAssert(f.str == "==");

                Debug.debugAssert(g == -1e6);

                Debug.debugAssert(h.type == Token.Type.SYMBOL);
                Debug.debugAssert(h.str == ")");

                Debug.debugAssert(i.type == Token.Type.SYMBOL);
                Debug.debugAssert(i.str == "{");

                Debug.debugAssert(j.type == Token.Type.SYMBOL);
                Debug.debugAssert(j.str == "cout");

                Debug.debugAssert(k.type == Token.Type.SYMBOL);
                Debug.debugAssert(k.str == "<<");

                Debug.debugAssert(L.type == Token.Type.STRING);
                Debug.debugAssert(L.str == "hello world");

                Debug.debugAssert(m.type == Token.Type.SYMBOL);
                Debug.debugAssert(m.str == "}");

                Debug.debugAssert(n.type == Token.Type.SYMBOL);
                Debug.debugAssert(n.str == ";");

                Debug.debugAssert(p.type == Token.Type.SYMBOL);
                Debug.debugAssert(p.str == "/");

                Debug.debugAssert(q.type == Token.Type.SYMBOL);
                Debug.debugAssert(q.str == "/");

                Debug.debugAssert(r.type == Token.Type.SYMBOL);
                Debug.debugAssert(r.str == "foo");

                Debug.debugAssert(s.type == Token.Type.SYMBOL);
                Debug.debugAssert(s.str == "bar");

                Debug.debugAssert(t.type == Token.Type.END);
            }

            {
                TextInput ti = new TextInput(TextInput.FS.FROM_STRING, "-1 +1 2.6");

                Token t;

                t = ti.peek();
                Debug.debugAssert(t.type == Token.Type.NUMBER);
                double n = ti.readNumber();
                Debug.debugAssert(n == -1);

                t = ti.peek();
                Debug.debugAssert(t.type == Token.Type.NUMBER);
                n = ti.readNumber();
                Debug.debugAssert(n == 1);

                t = ti.peek();
                Debug.debugAssert(t.type == Token.Type.NUMBER);
                n = ti.readNumber();
                Debug.debugAssert(n == 2.6);
            }

            {
                TextInput ti = new TextInput(TextInput.FS.FROM_STRING, "- 1 ---.51");

                Token t;

                t = ti.peek();
                Debug.debugAssert(t.type == Token.Type.SYMBOL);
                ti.readSymbol("-");

                t = ti.peek();
                Debug.debugAssert(t.type == Token.Type.NUMBER);
                double n = ti.readNumber();
                Debug.debugAssert(n == 1);

                t = ti.peek();
                Debug.debugAssert(t.type == Token.Type.SYMBOL);
                ti.readSymbol("--");

                t = ti.peek();
                Debug.debugAssert(t.type == Token.Type.NUMBER);
                n = ti.readNumber();
                Debug.debugAssert(n == -.51);
            }

            {
                TextInput.Settings ti_opts = new TextInput.Settings();
                string str = "'";
                ti_opts.singleQuotedStrings = false;

                TextInput ti = new TextInput(TextInput.FS.FROM_STRING, str, ti_opts);

                Token t = ti.read();

                Debug.alwaysAssertM(t.type == Token.Type.SYMBOL, "");
                Debug.alwaysAssertM(t.extendedType == Token.ExtendedType.SYMBOL_TYPE, "");
            }

            // Basic line number checking test.  Formerly would skip over line
            // numbers (i.e., report 1, 3, 5, 7 as the lines for the tokens), because
            // the newline would be consumed, pushed back to the input stream, then
            // consumed again (reincrementing the line number.)
            {
                TextInput ti = new TextInput(TextInput.FS.FROM_STRING, "foo\nbar\nbaz\n");
                checkSymToken(ti, "foo", 1, 1);
                checkSymToken(ti, "bar", 2, 1);
                checkSymToken(ti, "baz", 3, 1);
                checkEndToken(ti, 4, 1);
            }

            checkOneSpecialSym("@");
            checkOneSpecialSym("(");
            checkOneSpecialSym(")");
            checkOneSpecialSym(",");
            checkOneSpecialSym(";");
            checkOneSpecialSym("{");
            checkOneSpecialSym("}");
            checkOneSpecialSym("[");
            checkOneSpecialSym("]");
            checkOneSpecialSym("#");
            checkOneSpecialSym("$");
            checkOneSpecialSym("?");

            checkOneSpecialSym("-");
            checkOneSpecialSym("--");
            checkOneSpecialSym("-=");
            checkOneSpecialSym("->");

            checkOneSpecialSym("+");
            checkOneSpecialSym("++");
            checkOneSpecialSym("+=");

            checkOneSpecialSym(":");
            checkOneSpecialSym("::");

            checkOneSpecialSym("*");
            checkOneSpecialSym("*=");
            checkOneSpecialSym("/");
            checkOneSpecialSym("/=");
            checkOneSpecialSym("!");
            checkOneSpecialSym("!=");
            checkOneSpecialSym("~");
            checkOneSpecialSym("~=");
            checkOneSpecialSym("=");
            checkOneSpecialSym("==");
            checkOneSpecialSym("^");
            // Formerly (mistakenly) tokenized as symbol "^"
            checkOneSpecialSym("^=");

            checkOneSpecialSym(">");
            checkOneSpecialSym(">>");
            checkOneSpecialSym(">=");
            checkOneSpecialSym("<");
            checkOneSpecialSym("<<");
            checkOneSpecialSym("<=");
            checkOneSpecialSym("|");
            checkOneSpecialSym("||");
            checkOneSpecialSym("|=");
            checkOneSpecialSym("&");
            checkOneSpecialSym("&&");
            checkOneSpecialSym("&=");

            checkOneSpecialSym("\\");

            checkOneSpecialSym(".");
            checkOneSpecialSym("..");
            checkOneSpecialSym("...");

            // Formerly would loop infinitely if EOF seen in multi-line comment.
            {
                TextInput ti = new TextInput(TextInput.FS.FROM_STRING, "/* ... comment to end");
                checkEndToken(ti, 1, 22);
            }

            // Formerly would terminate quoted string after "foobar", having
            // mistaken \377 for EOF.
            {
                // This is a quoted string "foobarybaz", but with the 'y' replaced by
                // character 0xff (Latin-1 'y' with diaeresis a.k.a. HTML &yuml;).
                // It should parse into a quoted string with exactly those chars.
                // Documented adaptation: C++ \377 escape -> C# \u00FF.
                TextInput ti = new TextInput(TextInput.FS.FROM_STRING, "\"foobar\u00FFbaz\"");
                ti.readString("foobar\u00FFbaz");
                checkEndToken(ti, 1, 13);
            }

            {
                TextInput ti = new TextInput(TextInput.FS.FROM_STRING, "[ foo \n  bar\n");
                bool got_exc = false;
                try
                {
                    ti.readSymbols("[", "foo", "]");
                }
                catch (TextInput.WrongSymbol e)
                {
                    got_exc = true;
                    Debug.alwaysAssertM(e.expected == "]", "");
                    Debug.alwaysAssertM(e.actual == "bar", "");
                    checkExcPos(e, 2, 3);
                }
                Debug.alwaysAssertM(got_exc, "");
            }

            // Test file pseudonym creation.
            {
                TextInput ti = new TextInput(TextInput.FS.FROM_STRING, "foo");
                Token t;
                t = ti.read();
                checkTokenType(t, Token.Type.SYMBOL, Token.ExtendedType.SYMBOL_TYPE);
                checkTokenPos(t, 1, 1);
                Debug.alwaysAssertM(t.str == "foo", "");
            }

            // Test filename override.
            {
                TextInput.Settings tio = new TextInput.Settings();
                tio.sourceFileName = "<stdin>";
                TextInput ti = new TextInput(TextInput.FS.FROM_STRING, "foo", tio);
                Token t;
                t = ti.read();
                checkTokenType(t, Token.Type.SYMBOL, Token.ExtendedType.SYMBOL_TYPE);
                checkTokenPos(t, 1, 1);
                Debug.alwaysAssertM(t.str == "foo", "");
            }

            // Signed numbers, parsed two different ways
            {
                TextInput t = new TextInput(TextInput.FS.FROM_STRING, "- 5");
                Token x = t.read();
                checkTokenType(x, Token.Type.SYMBOL, Token.ExtendedType.SYMBOL_TYPE);
                Debug.alwaysAssertM(x.str == "-", "");

                x = t.read();
                checkTokenType(x, Token.Type.NUMBER, Token.ExtendedType.INTEGER_TYPE);
                Debug.alwaysAssertM(x.number() == 5, "");
            }

            {
                TextInput.Settings opt = new TextInput.Settings();
                opt.signedNumbers = false;
                TextInput t = new TextInput(TextInput.FS.FROM_STRING, "-5", opt);
                Debug.alwaysAssertM(t.readNumber() == -5, "");
            }

            {
                TextInput.Settings opt = new TextInput.Settings();
                opt.signedNumbers = false;
                TextInput t = new TextInput(TextInput.FS.FROM_STRING, "- 5", opt);
                try
                {
                    t.readNumber();
                    Debug.alwaysAssertM(false, "Incorrect parse");
                }
                catch (Exception)
                {
                }
            }

            // Test Nan and inf
            {
                TextInput.Settings opt = new TextInput.Settings();
                opt.msvcSpecials = true;
                TextInput t = new TextInput(TextInput.FS.FROM_STRING, "-1.#INF00", opt);
                double n = t.readNumber();
                Debug.alwaysAssertM(n == -g3dmath.inf(), "");
            }
            {
                TextInput.Settings opt = new TextInput.Settings();
                opt.msvcSpecials = true;
                TextInput t = new TextInput(TextInput.FS.FROM_STRING, "1.#INF00", opt);
                Debug.alwaysAssertM(t.readNumber() == g3dmath.inf(), "");
            }
            {
                TextInput.Settings opt = new TextInput.Settings();
                opt.msvcSpecials = true;
                TextInput t = new TextInput(TextInput.FS.FROM_STRING, "-1.#IND00", opt);
                Debug.alwaysAssertM(g3dmath.isNaN(t.readNumber()), "");
            }
            {
                TextInput t = new TextInput(TextInput.FS.FROM_STRING, "fafaosadoas");
                Debug.alwaysAssertM(t.hasMore(), "");
                t.readSymbol();
                Debug.alwaysAssertM(!t.hasMore(), "");
            }
        }
    }
}

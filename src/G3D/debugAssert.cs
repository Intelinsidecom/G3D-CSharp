/**
  @file debugAssert.h

  debugAssert(expression);
  debugAssertM(expression, message);

  @cite
     John Robbins, Microsoft Systems Journal Bugslayer Column, Feb 1999.
     <A HREF="http://msdn.microsoft.com/library/periodic/period99/feb99_BUGSLAYE_BUGSLAYE.htm">
     http://msdn.microsoft.com/library/periodic/period99/feb99_BUGSLAYE_BUGSLAYE.htm</A>

  @cite
     Douglas Cox, An assert() Replacement, Code of The Day, flipcode, Sept 19, 2000
     <A HREF="http://www.flipcode.com/cgi-bin/msg.cgi?showThread=COTD-AssertReplace&forum=cotd&id=-1">
     http://www.flipcode.com/cgi-bin/msg.cgi?showThread=COTD-AssertReplace&forum=cotd&id=-1</A>

  @maintainer Morgan McGuire, matrix@graphics3d.com

  @created 2001-08-26
  @edited  2006-01-12

 Copyright 2000-2006, Morgan McGuire.
 All rights reserved.
 */

using System;
using global::System.Diagnostics;

namespace G3D
{
    public delegate bool AssertionHook(
        string expression,
        string message,
        string filename,
        int lineNumber,
        ref bool ignoreAlways,
        bool useGuiPrompt);

    public static partial class Debug
    {
        private static AssertionHook _debugHook = _handleDebugAssert_;
        private static AssertionHook _failureHook = _handleErrorCheck_;

        public static void setAssertionHook(AssertionHook hook)
        {
            _debugHook = hook;
        }

        public static AssertionHook assertionHook()
        {
            return _debugHook;
        }

        /**
          Called by alwaysAssertM in case of failure in release mode.  If returns
          true then the program exits with -1 (you can replace this with your own
          version that throws an exception or has other failure modes).
         */
        public static void setFailureHook(AssertionHook hook)
        {
            _failureHook = hook;
        }

        public static AssertionHook failureHook()
        {
            return _failureHook;
        }

        /**
         @def debugBreak()

         Break at the current location (i.e. don't push a procedure stack frame
         before breaking).
         */
        public static void debugBreak()
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
        }

        public static void debugAssertGLOk()
        {
            // OpenGL debug check - no-op in C# port
        }

        /**
          @def debugAssert(exp)
          Breaks if the expression is false. If G3D_DEBUG_NOGUI is defined, prompts at
          the console, otherwise pops up a dialog.  The user may then break (debug),
          ignore, ignore always, or halt the program.

          The assertion is also posted to the clipboard under Win32.
         */
        public static void debugAssert(bool exp)
        {
#if DEBUG
            debugAssertM(exp, "Debug assertion failure");
#endif
        }

        /**
          @def debugAssertM(exp, msg)
          Breaks if the expression is false and displays a message. If G3D_DEBUG_NOGUI
          is defined, prompts at the console, otherwise pops up a dialog.  The user may
          then break (debug), ignore, ignore always, or halt the program.

          The assertion is also posted to the clipboard under Win32.
         */
        public static void debugAssertM(bool exp, string message)
        {
#if DEBUG
            bool ignoreAlways = false;
            if (!ignoreAlways && !exp)
            {
                string filename = "";
                int lineNumber = 0;
                try
                {
                    global::System.Diagnostics.StackTrace trace = new global::System.Diagnostics.StackTrace(1, true);
                    global::System.Diagnostics.StackFrame frame = trace.GetFrame(0);
                    if (frame != null)
                    {
                        filename = frame.GetFileName() ?? "";
                        lineNumber = frame.GetFileLineNumber();
                    }
                }
                catch
                {
                }

                if (_debugHook != null &&
                    _debugHook("false", message, filename, lineNumber, ref ignoreAlways, false))
                {
                    debugBreak();
                }
            }
#endif
        }

        public static void alwaysAssertM(bool exp, string message)
        {
#if DEBUG
            debugAssertM(exp, message);
#else
            bool ignoreAlways = false;
            if (!ignoreAlways && !exp)
            {
                if (_failureHook != null &&
                    _failureHook("false", message, "", 0, ref ignoreAlways, false))
                {
                    Environment.Exit(-1);
                }
            }
#endif
        }

        /**
         Pops up an assertion dialog or prints an assertion

         ignoreAlways      - return result of pressing the ignore button.
         useGuiPrompt      - if true, shows a dialog
         */
        private static bool _handleDebugAssert_(
            string expression,
            string message,
            string filename,
            int lineNumber,
            ref bool ignoreAlways,
            bool useGuiPrompt)
        {
            string text = message + Environment.NewLine + "Expression: " + expression + Environment.NewLine + filename + ":" + lineNumber;
            try
            {
                Log.common().print(Environment.NewLine + "**************************" + Environment.NewLine + "Assertion Failure" + Environment.NewLine + text + Environment.NewLine);
            }
            catch
            {
            }
            Console.Error.WriteLine("Assertion Failure: " + text);
            return true;
        }

        private static bool _handleErrorCheck_(
            string expression,
            string message,
            string filename,
            int lineNumber,
            ref bool ignoreAlways,
            bool useGuiPrompt)
        {
            string text = message + Environment.NewLine + "Expression: " + expression + Environment.NewLine + filename + ":" + lineNumber;
            try
            {
                Log.common().print(Environment.NewLine + "**************************" + Environment.NewLine + "Critical Error" + Environment.NewLine + text + Environment.NewLine);
            }
            catch
            {
            }
            Console.Error.WriteLine("Critical Error: " + text);
            Console.Error.WriteLine(Environment.StackTrace);
            return true;
        }

        /** Attempts to give the user back their mouse and keyboard if they
            were locked to the current window.
            @internal*/
        public static void _releaseInputGrab_()
        {
        }

        /** Attempts to restore the state before _releaseInputGrab_.
            @internal*/
        public static void _restoreInputGrab_()
        {
        }
    }
}

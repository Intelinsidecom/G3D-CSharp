/**
 @file debugPrintf.h

 <CODE>debugPrintf(char* fmt ...);</CODE>
 Prints to the debug window (win32) or stderr.

 @maintainer Morgan McGuire, matrix@graphics3d.com

 @created 2001-08-26
 @edited  2002-08-07

 Copyright 2000-2006, Morgan McGuire.
 All rights reserved.
 */

using System;

namespace G3D
{
    public static partial class Debug
    {
        /**
         @fn void __cdecl debugPrintf(const char *fmt ...)

          Prints a string from arguments of the style of printf.
          If G3D_DEBUG is not defined, does nothing.  On Windows, the
          string is printed to the output window in MSVC.  On other
          platforms, the string is printed to stderr.
         */
        public static void debugPrintf(string fmt, params object[] args)
        {
#if DEBUG
            string text = Format.vformat(fmt, args);
            global::System.Diagnostics.Debug.Write(text);
            Console.Error.Write(text);
#endif
        }
    }
}

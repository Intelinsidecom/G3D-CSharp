/**
 @file debug.h

 @maintainer Morgan McGuire, matrix@graphics3d.com

 @created 2001-08-26
 @edited  2006-02-16

  Copyright 2000-2006, Morgan McGuire.
  All rights reserved.
  */

using System;

namespace G3D
{
    public static partial class Debug
    {
        /**
         Useful for debugging purposes.
         */
        public static bool isValidHeapPointer(object x)
        {
            return x != null;
        }

        /**
         Returns true if the pointer is likely to be
         a valid pointer (instead of an arbitrary number).
         Useful for debugging purposes.
         */
        public static bool isValidPointer(object x)
        {
            return x != null;
        }
    }
}

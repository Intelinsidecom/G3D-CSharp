/**
  @file g3derror.h
  
  @maintainer Morgan McGuire, matrix@graphics3d.com
  
  @created 2000-08-26
  @created 2002-08-04

  Copyright 2000-2003, Morgan McGuire.
  All rights reserved.
 */

using System;

namespace G3D
{
    /**
     Return values from error().
     */
    public enum ErrorConstant { IGNORE_ERROR, QUIT_ERROR }

    /**
      @def error(level, message, showPrompt)
      error(const char *level, const char *message, bool showPrompt);
      error(const char *level, const string &message, bool showPrompt);
      error(const string &level, const string &message, bool showPrompt);
     
      Logs an error to stderr with the time, date, file, line, and message.
      Displays an error message.  If prompt is true, with an interactive prompt.
      If errorLevel is "Critical Error" the option to continue is not shown.
     
       @return Returns QUIT_ERROR if the user wants to quit, IGNORE_ERROR
          if the user wants to continue.
     */
    public static class Error
    {
        public static ErrorConstant error(string level, string message, bool showPrompt)
        {
            return _internal.ErrorUtility._utility_error(level, message, showPrompt, "", 0);
        }
    }

    namespace _internal
    {
        /**
         @internal
         */
        public static class ErrorUtility
        {
            /**
             @internal
             */
            public static ErrorConstant _utility_error(
                string level,
                string message,
                bool showPrompt,
                string filename,
                int line)
            {
                if (level == null)
                {
                    throw new ArgumentNullException("level");
                }
                if (message == null)
                {
                    throw new ArgumentNullException("message");
                }

                if (showPrompt)
                {
                    if (level == "Critical Error")
                    {
                        string[] choice = new string[] { "Quit" };
                        Prompt.prompt(level, message, choice, 1);
                        return G3D.ErrorConstant.QUIT_ERROR;
                    }
                    else
                    {
                        string[] choice = new string[] { "Ignore", "Quit" };
                        if (Prompt.prompt(level, message, choice, 2) == 1)
                        {
                            return G3D.ErrorConstant.QUIT_ERROR;
                        }
                    }
                }

                return G3D.ErrorConstant.IGNORE_ERROR;
            }
        }
    }
}

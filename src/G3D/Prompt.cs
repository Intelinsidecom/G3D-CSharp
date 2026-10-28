/**
  @file prompt.h
  
  @maintainer Morgan McGuire, matrix@graphics3d.com
  @cite   Windows GUI code by Max McGuire

  @created 2001-08-26
  @edited  2003-09-13
*/

using System;

namespace G3D
{
    /**
      Prints a prompt to stdout and waits for user input.  The return value is
      the number of the user's choice (the first is 0, if there are no
      choices, returns 0). 
     
      @param useGui Under Win32, use a GUI, not stdout prompt.
      @param windowTitle The title for the prompt window
      @param promptx The text string to prompt the user with
      @param choice  An array of strings that are the choices the user may make
      @param numChoices The length of choice.

      @cite Windows dialog interface by Max McGuire, mmcguire@ironlore.com
      @cite Font setting code by Kurt Miller, kurt@flipcode.com
      @deprecated
     */
    public static class Prompt
    {
        internal static Func<int> consoleKeyReader;

        public static int prompt(
            string windowTitle,
            string promptx,
            string[] choice,
            int numChoices,
            bool useGui)
        {
            if (windowTitle == null)
            {
                throw new ArgumentNullException("windowTitle");
            }
            if (promptx == null)
            {
                throw new ArgumentNullException("promptx");
            }
            if (numChoices < 0)
            {
                throw new ArgumentOutOfRangeException("numChoices");
            }

            int effectiveChoiceCount = numChoices > 10 ? 10 : numChoices;
            if (effectiveChoiceCount > 0)
            {
                if (choice == null)
                {
                    throw new ArgumentNullException("choice");
                }
                if (choice.Length < effectiveChoiceCount)
                {
                    throw new ArgumentException("The choice array is shorter than numChoices.", "choice");
                }
            }

            return textPrompt(windowTitle, promptx, choice, effectiveChoiceCount);
        }

        /**
          Prints a prompt and waits for user input.  The return value is
          the number of the user's choice (the first is 0, if there are no
          choices, returns 0).
          <P>Uses GUI under Win32, stdout prompt otherwise.
          @deprecated
         */
        public static int prompt(
            string windowTitle,
            string promptx,
            string[] choice,
            int numChoices)
        {
            return prompt(windowTitle, promptx, choice, numChoices, true);
        }

        /**
         Displays a GUI prompt with "Ok" as the only choice.
         @deprecated 
         <B>Note:</B> SDL 1.2.6 interferes with this and causes it to return
         immediately.  If you use SDL, use SDL 1.2.5, which does not have that
         problem.
         */
        public static void msgBox(string message)
        {
            msgBox(message, "Message");
        }

        /**
         Displays a GUI prompt with "Ok" as the only choice.
         @deprecated 
         <B>Note:</B> SDL 1.2.6 interferes with this and causes it to return
         immediately.  If you use SDL, use SDL 1.2.5, which does not have that
         problem.
         */
        public static void msgBox(string message, string title)
        {
            string[] choice = new string[] { "Ok" };
            prompt(title, message, choice, 1, true);
        }

        /**
         * Show a prompt on stdout 
         */
        private static int textPrompt(
            string windowTitle,
            string prompt,
            string[] choice,
            int numChoices)
        {
            Console.Write("\n___________________________________________________\n");
            Console.Write(Format.format("%s\n", windowTitle));
            Console.Write(Format.format("%s", prompt));

            int c = -1;
            if (numChoices > 1)
            {
                Console.Write("\n");
                Console.Write("Choose an option by number:");

                while ((c < 0) || (c >= numChoices))
                {
                    Console.Write("\n");

                    for (int i = 0; i < numChoices; i++)
                    {
                        if (numChoices <= 3)
                        {
                            Console.Write(Format.format("  (%d) %s ", i, choice[i]));
                        }
                        else
                        {
                            Console.Write(Format.format("  (%d) %s\n", i, choice[i]));
                        }
                    }

                    Console.Write("\n> ");
                    c = readKey() - '0';

                    if ((c < 0) || (c >= numChoices))
                    {
                        Console.Write(Format.format("'%d' is not a valid choice.", c));
                    }
                    else
                    {
                        Console.Write(Format.format("%d", c));
                    }
                }
            }
            else if (numChoices == 1)
            {
                Console.Write("\nPress any key for '" + choice[0] + "'...");
                readKey();
                c = 0;
            }
            else
            {
                Console.Write("\nPress any key...");
                readKey();
                c = 0;
            }

            Console.Write("\n___________________________________________________\n");
            return c;
        }

        private static int readKey()
        {
            if (consoleKeyReader != null)
            {
                return consoleKeyReader();
            }

            try
            {
                return Console.ReadKey(true).KeyChar;
            }
            catch (InvalidOperationException)
            {
                int redirectedValue = Console.In.Read();
                if (redirectedValue < 0)
                {
                    throw new InvalidOperationException("No console input is available.");
                }
                return redirectedValue;
            }
        }
    }
}

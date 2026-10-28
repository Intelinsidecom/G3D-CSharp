using System;
using System.IO;
using G3D;
using Microsoft.Win32;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private static void testPrompt()
        {
            printf("Prompt ");

            global::System.IO.TextWriter originalOut = Console.Out;
            global::System.Func<int> originalReader = Prompt.consoleKeyReader;
            try
            {
                using (global::System.IO.StringWriter output = new global::System.IO.StringWriter())
                {
                    Console.SetOut(output);

                    int readCount = 0;
                    Prompt.consoleKeyReader = delegate
                    {
                        readCount++;
                        return readCount == 1 ? 'x' : '1';
                    };

                    int choice = Prompt.prompt(
                        "Console Redirect",
                        "Choose an option",
                        new string[] { "Stay", "Quit" },
                        2);
                    CheckEqual(1, choice, "Prompt choice index");
                    CheckEqual(2, readCount, "Prompt invalid choice retry");
                    Check(output.ToString().Contains("Console Redirect"), "Prompt title output");
                    Check(output.ToString().Contains("Choose an option"), "Prompt message output");
                    Check(output.ToString().Contains("(1) Quit"), "Prompt choice output");
                    Check(output.ToString().Contains("is not a valid choice"), "Prompt invalid choice output");

                    Prompt.consoleKeyReader = delegate { return 'z'; };
                    CheckEqual(0, Prompt.prompt("No Choices", "Continue", null, 0, false),
                        "Prompt with no choices");
                    Check(output.ToString().Contains("Press any key..."), "Prompt no-choice output");

                    Prompt.msgBox("Message body", "Notice");
                    Check(output.ToString().Contains("Message body"), "Prompt message box body");
                    Check(output.ToString().Contains("Notice"), "Prompt message box title");
                    Check(output.ToString().Contains("Press any key for 'Ok'"), "Prompt message box choice");
                }
            }
            finally
            {
                Prompt.consoleKeyReader = originalReader;
                Console.SetOut(originalOut);
            }

            printf("passed\n");
        }

        private static void testG3DError()
        {
            printf("g3derror ");

            global::System.IO.TextWriter originalOut = Console.Out;
            global::System.Func<int> originalReader = Prompt.consoleKeyReader;
            try
            {
                using (global::System.IO.StringWriter output = new global::System.IO.StringWriter())
                {
                    Console.SetOut(output);

                    CheckEqual(0, (int)ErrorConstant.IGNORE_ERROR, "IGNORE_ERROR value");
                    CheckEqual(1, (int)ErrorConstant.QUIT_ERROR, "QUIT_ERROR value");

                    int readCount = 0;
                    Prompt.consoleKeyReader = delegate
                    {
                        readCount++;
                        return '0';
                    };
                    Check(Error.error("Error", "No prompt", false) == ErrorConstant.IGNORE_ERROR,
                        "Error without prompt returns IGNORE_ERROR");
                    CheckEqual(0, readCount, "Error without prompt does not read input");

                    Check(Error.error("Error", "Ignore", true) == ErrorConstant.IGNORE_ERROR,
                        "Error Ignore choice returns IGNORE_ERROR");
                    CheckEqual(1, readCount, "Error Ignore choice read");

                    Prompt.consoleKeyReader = delegate { return '1'; };
                    Check(Error.error("Error", "Quit", true) == ErrorConstant.QUIT_ERROR,
                        "Error Quit choice returns QUIT_ERROR");

                    Prompt.consoleKeyReader = delegate { return 'x'; };
                    Check(Error.error("Critical Error", "Fatal", true) == ErrorConstant.QUIT_ERROR,
                        "Critical Error returns QUIT_ERROR");
                    Check(output.ToString().Contains("(1) Quit"), "Critical Error has no Ignore choice");
                }
            }
            finally
            {
                Prompt.consoleKeyReader = originalReader;
                Console.SetOut(originalOut);
            }

            printf("passed\n");
        }

        private static void testLicense()
        {
            printf("license ");

            string expected =
"This program uses the Graphics3D Library (\"G3D\", http://g3d-cpp.sf.net)\n" +
"This software is based in part on the PNG Reference Library which is\n" +
"Copyright (c) 2004 Glenn Randers-Pehrson\n" +
"This software is based in part on the work of the Independent JPEG Group.\n" +
"This software uses the Simple DirectMedia Layer library (\"SDL\",\n" +
"http://www.libsdl.org), which is included under the terms of the\n" +
"GNU Lesser General Public License, (http://www.gnu.org/copyleft/lesser.html).\n" +
"\n" +
"This program uses the Graphics3D Library (\"G3D\", http://g3d-cpp.sf.net), which\n" +
"is licensed under the \"BSD\" Open Source license.  The Graphics3D library\n" +
"source code is Copyright © 2000-2006, Morgan McGuire, All rights reserved.\n" +
"\n" +
"Redistribution and use in source and binary forms, with or without\n" +
"modification, are permitted provided that the following conditions\n" +
"are met:\n" +
"\n" +
"Redistributions of source code must retain the above copyright\n" +
"notice, this list of conditions and the following disclaimer.\n" +
"\n" +
"Redistributions in binary form must reproduce the above copyright\n" +
"notice, this list of conditions and the following disclaimer in the\n" +
"documentation and/or other materials provided with the distribution.\n" +
"\n" +
"Neither the name of Morgan McGuire, Brown University, nor the names\n" +
"of its contributors may be used to endorse or promote products derived\n" +
"from this software without specific prior written permission.\n" +
"\n" +
"THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS\n" +
"\"AS IS\" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT\n" +
"LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR\n" +
"A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT\n" +
"OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,\n" +
"SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT\n" +
"LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,\n" +
"DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY\n" +
"THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT\n" +
"(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE\n" +
"OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.\n" +
"\n\n" +
"G3D VERSION 60900\n";

            CheckEqual(expected, System.license(), "System.license exact original text");
            printf("passed\n");
        }

        private static bool registryPlatformIsWindows()
        {
            PlatformID platform = Environment.OSVersion.Platform;
            return platform == PlatformID.Win32NT ||
                   platform == PlatformID.Win32Windows ||
                   platform == PlatformID.Win32S;
        }

        private static void testRegistryUtil()
        {
            printf("RegistryUtil ");

            if (!registryPlatformIsWindows())
            {
                bool unsupportedExceptionThrown = false;
                try
                {
                    RegistryUtil.keyExists("HKEY_CURRENT_USER\\Software");
                }
                catch (PlatformNotSupportedException)
                {
                    unsupportedExceptionThrown = true;
                }
                Check(unsupportedExceptionThrown, "RegistryUtil unsupported-platform exception");
                printf("passed\n");
                return;
            }

            int processId = global::System.Diagnostics.Process.GetCurrentProcess().Id;
            string subKey = "Software\\G3D.Tests.RegistryUtil_" +
                processId.ToString() + "_" + DateTime.UtcNow.Ticks.ToString();
            string keyPrefix = "HKEY_CURRENT_USER\\" + subKey;

            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(subKey))
                {
                    Check(key != null, "RegistryUtil test key creation");
                }

                Check(RegistryUtil.keyExists(keyPrefix), "RegistryUtil keyExists");
                Check(!RegistryUtil.keyExists(keyPrefix + "\\Missing"), "RegistryUtil missing keyExists");
                Check(RegistryUtil.keyExists("HKEY_USERS\\"), "RegistryUtil HKEY_USERS base key");

                Check(RegistryUtil.writeInt32(keyPrefix + "\\IntValue", -1234567),
                    "RegistryUtil writeInt32");
                int intValue = 0;
                Check(RegistryUtil.readInt32(keyPrefix + "\\IntValue", ref intValue),
                    "RegistryUtil readInt32");
                CheckEqual(-1234567, intValue, "RegistryUtil Int32 value");

                Check(RegistryUtil.writeString(keyPrefix + "\\StringValue", "G3D registry value"),
                    "RegistryUtil writeString");
                string stringValue = "unchanged";
                Check(RegistryUtil.readString(keyPrefix + "\\StringValue", ref stringValue),
                    "RegistryUtil readString");
                CheckEqual("G3D registry value", stringValue, "RegistryUtil string value");

                byte[] sourceBytes = new byte[] { 1, 2, 3, 4 };
                Check(RegistryUtil.writeBytes(keyPrefix + "\\BinaryValue", sourceBytes, 4),
                    "RegistryUtil writeBytes");

                uint dataSize = 0;
                Check(RegistryUtil.readBytes(keyPrefix + "\\BinaryValue", null, ref dataSize),
                    "RegistryUtil readBytes size query");
                CheckEqual(4, (int)dataSize, "RegistryUtil byte size");

                byte[] readBytes = new byte[4];
                dataSize = 4;
                Check(RegistryUtil.readBytes(keyPrefix + "\\BinaryValue", readBytes, ref dataSize),
                    "RegistryUtil readBytes");
                CheckEqual(4, (int)dataSize, "RegistryUtil readBytes size");
                Check(readBytes[0] == 1 && readBytes[1] == 2 &&
                      readBytes[2] == 3 && readBytes[3] == 4,
                    "RegistryUtil byte values");

                byte[] shortBuffer = new byte[2];
                dataSize = 2;
                Check(!RegistryUtil.readBytes(keyPrefix + "\\BinaryValue", shortBuffer, ref dataSize),
                    "RegistryUtil undersized readBytes fails");
                CheckEqual(4, (int)dataSize, "RegistryUtil undersized readBytes required size");

                int missingInt = 99;
                Check(!RegistryUtil.readInt32(keyPrefix + "\\Missing\\Value", ref missingInt),
                    "RegistryUtil missing readInt32 fails");
                CheckEqual(99, missingInt, "RegistryUtil failed read preserves Int32");

                string missingString = "unchanged";
                Check(!RegistryUtil.readString(keyPrefix + "\\Missing\\Value", ref missingString),
                    "RegistryUtil missing readString fails");
                CheckEqual("unchanged", missingString, "RegistryUtil failed read preserves string");

                Check(!RegistryUtil.writeInt32(keyPrefix + "\\Missing\\Value", 1),
                    "RegistryUtil write to missing key fails");
                Check(!RegistryUtil.keyExists("not-a-registry-key"), "RegistryUtil malformed key");
                Check(!RegistryUtil.keyExists("HKEY_UNKNOWN\\Value"), "RegistryUtil unknown base key");

                bool nlsUnsupported = false;
                try
                {
                    RegistryUtil.keyExists("HKEY_PERFORMANCE_NLSTEXT\\Value");
                }
                catch (NotSupportedException)
                {
                    nlsUnsupported = true;
                }
                Check(nlsUnsupported, "RegistryUtil managed HKEY_PERFORMANCE_NLSTEXT limitation");

                bool performanceTextUnsupported = false;
                try
                {
                    RegistryUtil.keyExists("HKEY_PERFORMANCE_TEXT\\Value");
                }
                catch (NotSupportedException)
                {
                    performanceTextUnsupported = true;
                }
                Check(performanceTextUnsupported, "RegistryUtil managed HKEY_PERFORMANCE_TEXT limitation");
            }
            finally
            {
                try
                {
                    Registry.CurrentUser.DeleteSubKeyTree(subKey, false);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            printf("passed\n");
        }

        private static void testCoreUtilities()
        {
            testPrompt();
            testG3DError();
            testLicense();
            testRegistryUtil();
        }
    }
}

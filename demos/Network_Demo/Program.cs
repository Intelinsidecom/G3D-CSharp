/**
  @file Network_Demo/Program.cs

  Port of main() in Network_Demo/main.cpp.

  Optional command line argument is the name of the log file (useful
  when running two clients on the same machine).
 */

using System;
using System.Windows.Forms;
using G3D;

namespace Network_Demo
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i] == "--selftest")
                {
                    Environment.ExitCode = Network_Demo.SelfTest.Run(args);
                    return;
                }
            }

            string backend = "opengl";
            foreach (string arg in args)
            {
                if (arg != null && arg.StartsWith("--backend=", StringComparison.OrdinalIgnoreCase))
                {
                    backend = arg.Substring("--backend=".Length);
                }
            }

            GAppSettings settings = new GAppSettings();

            if (args.Length > 1)
            {
                settings.logFilename = args[1];
            }

            settings.window.fsaaSamples = 4;
            settings.window.width = 640;
            settings.window.height = 480;

            App app = new App(settings, backend);
            app.run();
        }
    }
}

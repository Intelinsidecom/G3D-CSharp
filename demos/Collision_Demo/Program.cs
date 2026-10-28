/**
  @file Collision_Demo/Program.cs

  @maintainer Morgan McGuire, matrix@graphics3d.com
  @created 2003-02-07
  @edited  2003-11-18
*/

using System;
using System.Windows.Forms;
using G3D;

namespace Collision_Demo
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string backend = "opengl";
            foreach (string arg in args)
            {
                if (arg != null && arg.StartsWith("--backend=", StringComparison.OrdinalIgnoreCase))
                {
                    backend = arg.Substring("--backend=".Length);
                }
            }

            App app = new App(new GAppSettings(), backend);
            app.run();
            Model.freeModels();
        }
    }
}
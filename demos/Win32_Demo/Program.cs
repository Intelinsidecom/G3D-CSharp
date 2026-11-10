/**
  @file Win32_Demo/Program.cs

  Port of WinMain() in demos/Win32_Demo/main.cpp.

  This demo is a Win32 only program that creates a window in which
  you can set pixels.  It also shows how to use the G3D::GImage class
  to load an image.  This should be sufficient setup to write
  non-realtime programs like ray tracers and image processing demos.

  To run this sample program, you must have installed the Graphics3D library
  on your machine according to the instructions in the doc/index.html
  file.


  Requires:
    Windows
    Graphics3D

  @maintainer Morgan McGuire, matrix@graphics3d.com
  @created    2002-01-01
  @edited     2003-12-07
*/

using System;
using System.Windows.Forms;
using G3D;

namespace Win32_Demo
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Window app = new Window();

            try
            {
                // The original relies on the GIF/JPEG codecs built into G3D.
                // In this port they are supplied by the System.Drawing image
                // assembly, so its codecs must be registered first.
                G3D.Image.SystemDrawing.SystemDrawingImageCodec.register();

                // The original loads "Win32_Demo/Cirrus.jpg" relative to its
                // working directory; this port runs from the demo's output
                // directory, where the asset is copied beside the executable.
                string filename = "Win32_Demo/Cirrus.jpg";
                if (!System.IO.File.Exists(filename))
                {
                    filename = System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(
                            System.Reflection.Assembly.GetExecutingAssembly().Location),
                        "Cirrus.jpg");
                }

                GImage im = new GImage(filename);

                app.create("Win32 Sample Program", im.width, im.height);
                G3D.GImage.RGBtoBGRA(im.getBytes(), app.buffer, im.width * im.height);

            }
            catch (G3D.GImage.Error e)
            {
                Error.error("Critical Error", e.reason, true);
                return;
            }

            app.run();
        }
    }
}

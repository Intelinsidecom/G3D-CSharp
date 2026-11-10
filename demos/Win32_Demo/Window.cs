/**
  @file Win32_Demo/Window.cs

  Support routines for tests.

  @maintainer Morgan McGuire, matrix@graphics3d.com
  @cite       Based on code by Max McGuire, mmcguire@ironlore.com
  @created 	  2002-09-09
  @edited  	  2003-01-25

  Copyright 2000-2003, Morgan McGuire.
  All rights reserved.
*/

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Win32_Demo
{
    public class Window : Form
    {
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_CLOSE   = 0x0010;
        private const int WM_DESTROY = 0x0002;

        private const int BLIT_BUFFER = 0xC001;

        private const int SM_CXFIXEDFRAME = 7;
        private const int SM_CYFIXEDFRAME = 8;
        private const int SM_CYCAPTION    = 4;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        public Graphics hDC;

        public int width;
        public int height;

        /**
          Size width x height x 4
        */
        public byte[] buffer;

        public Window()
        {
            buffer = null;
            width  = 0;
            height = 0;
        }

        public bool create(string name, int width, int height)
        {
            this.width  = width;
            this.height = height;

            // Allocate the image buffer used for rendering
            buffer = new byte[width * height * 4];

            Text = name;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = true;
            MaximizeBox = false;
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;

            // Add the non-client area
            int total_width  = width  + GetSystemMetrics(SM_CXFIXEDFRAME) * 2;
            int total_height = height + GetSystemMetrics(SM_CYFIXEDFRAME) * 2 + GetSystemMetrics(SM_CYCAPTION);

            // CreateWindow's cx and cy are the outer size and the original passes
            // them unchanged, so its client area comes out 10 pixels smaller than
            // the image in each direction and the bottom 10 rows and right 10
            // columns are never shown.  Reproducing that faithfully means sizing
            // the outer window rather than the client area: WinForms sizes the
            // client area when given a client size and the whole image would
            // show.  The sizable border style is the one whose non-client area
            // matches, giving the same 642x284 client for the 652x294 image that
            // the original produces on this platform.
            Size = new Size(total_width, total_height);

            StartPosition = FormStartPosition.Manual;
            Location = new Point(
                (SystemInformation.PrimaryMonitorSize.Width  - total_width)  / 2,
                (SystemInformation.PrimaryMonitorSize.Height - total_height) / 2);

            Show();

            return true;
        }


        public void close()
        {
            Close();
        }


        public void requestRedraw()
        {
            Message m = Message.Create(Handle, BLIT_BUFFER, IntPtr.Zero, IntPtr.Zero);
            WndProc(ref m);
        }


        public void run()
        {
            // main message loop

            hDC = CreateGraphics();

            // Equivalent of SetStretchBltMode(hDC, COLORONCOLOR)
            hDC.InterpolationMode = InterpolationMode.NearestNeighbor;

            Application.Run(this);

            hDC.Dispose();
            hDC = null;
        }


        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
            case WM_KEYDOWN:
                Application.Exit();
                return;

            /*    case WM_KEYDOWN:
            this_window->keyChange(wparam, true);
            return 0;

              case WM_KEYUP:
              this_window->keyChange(wparam, false);
              return 0;
                */
            case WM_CLOSE:
                Application.Exit();
                return;

            case BLIT_BUFFER:
                //this_window->redraw();
                break;
            }

            base.WndProc(ref m);
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // The original converts the buffer to a DIB through a BITMAPINFO
            // and hands it to SetDIBitsToDevice.  This port builds the same
            // top-down 32-bit BGRA surface as a Bitmap and draws it.
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;

            if (buffer == null)
            {
                return;
            }

            using (Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                BitmapData data = bitmap.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppArgb);

                System.Runtime.InteropServices.Marshal.Copy(
                    buffer, 0, data.Scan0, width * height * 4);

                bitmap.UnlockBits(data);

                e.Graphics.DrawImage(bitmap, 0, 0, width, height);
            }
        }
    }
}

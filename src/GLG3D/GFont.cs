/**
  @file GFont.h

  Font class for use with RenderDevice.  Renders variable size and color
  fonts from high-resolution bitmaps.

  Font rendering is (inherently) slow-- you can achieve better performance
  for static text by creating bitmap textures with whole words and
  sentences on them.
  <P>

  The following fonts are provided with G3D in the
  <CODE>data/font</CODE> directory.  See the <CODE>copyright.txt</CODE>
  file in that directory for information about the source of these
  files and rules for distribution.  <P> <IMG SRC="font.png">
*/

using System;
using System.IO;
using G3D;

namespace G3D
{
    public class GFont : ReferenceCountedObject
    {
        /** Constant for draw2D.  Specifies the horizontal alignment
            of an entire string relative to the supplied x,y position */
        public enum XAlign { XALIGN_RIGHT, XALIGN_LEFT, XALIGN_CENTER };

        /** Constant for draw2D.  Specifies the vertical alignment
            of the characters relative to the supplied x,y position.
         */
        public enum YAlign { YALIGN_TOP, YALIGN_BASELINE, YALIGN_CENTER, YALIGN_BOTTOM };

        /** Constant for draw2D.  Proportional width (default)
            spaces characters based on their size.  Fixed spacing gives
            uniform spacing regardless of character width. */
        public enum Spacing { PROPORTIONAL_SPACING, FIXED_SPACING };

        /** The actual width of the character. */
        private int[] subWidth = new int[128];

        /** The width of the box, in texels, around the character. */
        private int glyphWidth;
        private int glyphHeight;

        /** Y distance from top of the bounding box to the font baseline. */
        private int baseline;

        private ReferenceCountedPointer<Texture> fontTexture;

        /** The font texture is drawn with the immediate mode pipeline in
            this port instead of packed vertex arrays. */
        private RenderDevice renderDevice;

        protected GFont(RenderDevice renderDevice, string filename, BinaryInput b)
        {
            this.renderDevice = renderDevice;

            int ver = b.readInt32();
            Debug.debugAssertM(ver == 1, "Can't read font files other than version 1");

            // Read the widths
            for (int c = 0; c < 128; ++c)
            {
                subWidth[c] = b.readUInt16();
            }

            baseline = b.readUInt16();
            int texWidth = b.readUInt16();
            glyphWidth = texWidth / 16;
            glyphHeight = texWidth / 16;

            // The input may not be a power of 2
            int width = g3dmath.ceilPow2((uint)(glyphWidth * 16));
            int height = g3dmath.ceilPow2((uint)(glyphHeight * 8));

            byte[] source = b.getCArray();
            int position = b.getPosition();

            // The font texture stores the glyph coverage in a single channel.
            // The port uses the RGBA8 format so that the coverage can be used
            // as the alpha channel when blending.
            byte[] data = new byte[width * height * 4];
            for (int i = 0; i < width * height; ++i)
            {
                byte coverage = position + i < source.Length ? source[position + i] : (byte)0;
                data[i * 4] = 255;
                data[i * 4 + 1] = 255;
                data[i * 4 + 2] = 255;
                data[i * 4 + 3] = coverage;
            }

            fontTexture = new ReferenceCountedPointer<Texture>(Texture.fromMemory(filename, data, TextureFormat.RGBA8, width, height,
                TextureFormat.RGBA8, Texture.WrapMode.CLAMP, Texture.InterpolateMode.TRILINEAR_MIPMAP,
                Texture.Dimension.DIM_2D, Texture.DepthReadMode.DEPTH_NORMAL, 1.0f));
        }

        /** @deprecated */
        public static ReferenceCountedPointer<GFont> fromFile(RenderDevice renderDevice, string filename)
        {
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException("Could not load font", filename);
            }

            BinaryInput b = new BinaryInput(filename, G3DEndian.G3D_LITTLE_ENDIAN, true);
            return new ReferenceCountedPointer<GFont>(new GFont(renderDevice, filename, b));
        }

        /** The filename must be a FNT (proportional width font) file.
            <P> If a font file is not found, an assertion will fail, an
            exception will be thrown, and texelSize() will return (0, 0).
            <P> Several fonts in this format at varying resolutions are
            available in the data/font directory.  The large fonts require
            500k of memory when loaded and look good when rendering
            characters up to about 64 pixels high. The small fonts require
            130k and look good up to about 32 pixels.

            See GFont::convertTGAtoPWF for creating new fonts in the FNT format:

          <P>
           This file is compressed by BinaryOutput::compress().  The contents
           after decompression  have the following format (little endian):
            <pre>
             int32                       Version number (must be 1)
             128 * int16                 Character widths, in texels
             uint16                      Baseline from top of box, in texels
             uint16                      Texture width (texture height is always 1/2 texture width)
             (pow(width, 2) / 2) * int8  Texture data
            </pre>
            The width of a character's bounding box is always width / 16.  The height is always width / 8.
        */
        public static ReferenceCountedPointer<GFont> fromFile(string filename)
        {
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException("Could not load font", filename);
            }

            BinaryInput b = new BinaryInput(filename, G3DEndian.G3D_LITTLE_ENDIAN, true);
            return new ReferenceCountedPointer<GFont>(new GFont(null, filename, b));
        }

        /** @deprecated Converted to GFont::fromFile */
        public static ReferenceCountedPointer<GFont> create(RenderDevice renderDevice, string filename)
        {
            return fromFile(renderDevice, filename);
        }

        /** @deprecated Converted to GFont::fromFile */
        public static ReferenceCountedPointer<GFont> create(string filename)
        {
            return fromFile(filename);
        }

        /** see GFont::fromFile */
        public static ReferenceCountedPointer<GFont> fromMemory(string name, byte[] bytes, int size)
        {
            BinaryInput b = new BinaryInput(bytes, size, G3DEndian.G3D_LITTLE_ENDIAN, true, false);
            return new ReferenceCountedPointer<GFont>(new GFont(null, name, b));
        }

        /**
         Converts an 8-bit RAW font texture and INI file as produced by
         the Bitmap Font Builder program to a graphics3d PWF font.  infileBase
         should have no extension-- .raw and .ini will be appended to it.
         outfile should end with ".FNT" or be "" for the default.  <P>

          The Bitmap Font Builder program can be downloaded from http://www.lmnopc.com/bitmapfontbuilder/

          Use the full ASCII character set; the conversion will strip
          infrequently used characters automatically. Write out RAW files
          with characters CENTER aligned and right side up using this
          program.  Then, also write out an INI file; this contains the
          width of each character in the font.  Example:

          <PRE>
          GFont::convertRAWINItoPWF("c:/tmp/g3dfont/news",
                                    "d:/graphics3d/book/cpp/data/font/news.fnt");
          </PRE>

          @param infileBase The name of the raw/ini files @param outfile Defaults
          to infileBase + ".fnt"
         */
        public static void convertRAWINItoPWF(string infileBase, string outfile = "")
        {
            if (!File.Exists(infileBase + ".raw"))
            {
                throw new FileNotFoundException("Could not load font", infileBase + ".raw");
            }

            if (!File.Exists(infileBase + ".ini"))
            {
                throw new FileNotFoundException("Could not load font", infileBase + ".ini");
            }

            if (outfile == "")
            {
                outfile = infileBase + ".fnt";
            }

            BinaryInput pixel = new BinaryInput(infileBase + ".raw", G3DEndian.G3D_LITTLE_ENDIAN);
            TextInput ini = new TextInput(infileBase + ".ini");
            BinaryOutput outFile = new BinaryOutput(outfile, G3DEndian.G3D_LITTLE_ENDIAN);

            ini.readSymbol("[");
            ini.readSymbol("Char");
            ini.readSymbol("Widths");
            ini.readSymbol("]");

            // Version
            outFile.writeInt32(1);

            // Character widths
            for (int i = 0; i < 128; ++i)
            {
                int n = (int)ini.readNumber();
                Debug.debugAssertM(n == i, "Font INI character indices must be sequential");
                ini.readSymbol("=");
                int cw = (int)ini.readNumber();
                outFile.writeInt16((short)cw);
            }

            int width = (int)Math.Sqrt((double)pixel.size());

            byte[] raw = new byte[pixel.size()];
            pixel.readBytes(raw, raw.Length);

            // Autodetect baseline from capital E
            {
                // Size of a character, in texels
                int w = width / 16;

                int x0 = ('E' % 16) * w;
                int y0 = ('E' / 16) * w;

                byte[] p = raw;
                bool done = false;
                int detectedBaseline = w * 2 / 3;

                // Search up from the bottom for the first pixel
                for (int y = y0 + w - 1; (y >= y0) && !done; --y)
                {
                    for (int x = x0; (x < x0 + w) && !done; ++x)
                    {
                        int index = x + y * w * 16;
                        if (index >= 0 && index < p.Length && p[index] != 0)
                        {
                            detectedBaseline = y - y0 + 1;
                            done = true;
                        }
                    }
                }
                outFile.writeUInt16((ushort)detectedBaseline);
            }

            // Texture width
            outFile.writeUInt16((ushort)width);

            // The input may not be a power of 2, so size it up
            int width2 = g3dmath.ceilPow2((uint)width);
            int height2 = g3dmath.ceilPow2((uint)(width / 2));

            if ((width2 == width) && (height2 == (width / 2)))
            {
                // Texture
                int num = width * (width / 2);
                byte[] data = raw;
                int count = Math.Min(num, data.Length);
                outFile.writeBytes(data, count);
            }
            else
            {
                // Pad
                byte[] p = raw;

                for (int y = 0; y < height2; ++y)
                {
                    // Write the row
                    int offset = y * width;
                    int count = Math.Min(width, Math.Max(0, p.Length - offset));
                    if (count > 0)
                    {
                        outFile.writeBytes(p, offset, count);
                    }
                    if (count < width)
                    {
                        outFile.skip(width - count);
                    }

                    // Write the horizontal padding
                    outFile.skip(width2 - width);
                }

                // Write the vertical padding
                outFile.skip((height2 - (width / 2)) * width2);
            }

            outFile.compress();
            outFile.commit(false);
        }

        /** Returns the natural character width and height of this font. */
        public Vector2 texelSize()
        {
            return new Vector2(glyphWidth, glyphHeight);
        }

        /** The width of a single character cell, in texels. */
        public int charWidth
        {
            get { return glyphWidth; }
        }

        /** The height of a single character cell, in texels. */
        public int rowHeight
        {
            get { return glyphHeight; }
        }

        /** The texture that contains the rendered glyphs. */
        public Texture image
        {
            get { return fontTexture.pointer(); }
        }

        /**
         Draws a proportional width font string.  Assumes device->push2D()
         has been called.  Leaves all rendering state as it was, except for the
         texture coordinate on unit 0.

         @param size The distance between successive lines of text.  Specify
         texelSize().y / 1.5 to get 1:1 texel to pixel

         @param outline If this color has a non-zero alpha, a 1 pixel border of
         this color is drawn about the text.

         @param spacing Fixed width fonts are spaced based on the width of the 'M' character.

         @return Returns the x and y bounds (ala get2DStringBounds) of the printed string.
        */
        public Vector2 draw2D(
            RenderDevice rd,
            string s,
            Vector2 pos2D,
            double size = 12,
            Color4 color = default(Color4),
            Color4 outline = default(Color4),
            XAlign xalign = XAlign.XALIGN_LEFT,
            YAlign yalign = YAlign.YALIGN_TOP,
            Spacing spacing = Spacing.PROPORTIONAL_SPACING)
        {
            Debug.debugAssert(rd != null);

            if (rd == null)
            {
                throw new ArgumentNullException("rd");
            }

            if (color == null)
            {
                color = new Color4(Color3.black(), 1.0f);
            }

            if (outline == null)
            {
                outline = new Color4(0, 0, 0, 0);
            }

            float x = pos2D.x;
            float y = pos2D.y;

            float h = (float)(size * 1.5);
            float w = h * glyphWidth / glyphHeight;

            switch (xalign)
            {
                case XAlign.XALIGN_RIGHT:
                    x -= get2DStringBounds(s, size, spacing).x;
                    break;

                case XAlign.XALIGN_CENTER:
                    x -= get2DStringBounds(s, size, spacing).x / 2;
                    break;

                default:
                    break;
            }

            switch (yalign)
            {
                case YAlign.YALIGN_CENTER:
                    y -= h / 2.0f;
                    break;

                case YAlign.YALIGN_BASELINE:
                    y -= (float)(baseline * h / (double)glyphHeight);
                    break;

                case YAlign.YALIGN_BOTTOM:
                    y -= h;
                    break;

                default:
                    break;
            }

            float[] m = new float[]
            {
                1.0f / fontTexture.pointer().texelWidth(), 0, 0, 0,
                0, 1.0f / fontTexture.pointer().texelHeight(), 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            };

            rd.pushState();
            rd.disableLighting();
            rd.setTextureMatrix(0, m);
            rd.setTexture(0, fontTexture.pointer());
            rd.setTextureCombineMode(0, RenderDevice.CombineMode.TEX_MODULATE);

            // This is BLEND_SRC_ALPHA because the texture has no luminance, only alpha
            rd.setBlendFunc(RenderDevice.BlendFunc.BLEND_SRC_ALPHA,
                            RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_ALPHA);

            rd.setAlphaTest(RenderDevice.AlphaTest.ALPHA_GEQUAL, 1 / 255.0);

            float b = (float)rd.getBrightScale();

            int numChars = 0;
            for (int i = 0; i < s.Length; ++i)
            {
                numChars += ((s[i] % 128) != ' ') ? 1 : 0;
            }

            if (numChars == 0)
            {
                rd.popState();
                return new Vector2(0, h);
            }

            rd.beginPrimitive(RenderDevice.Primitive.QUADS);

            if (outline.a > 0.05f)
            {
                rd.setColor(new Color4(outline.r * b, outline.g * b, outline.b * b, outline.a));
                for (int dy = -1; dy <= 1; dy += 2)
                {
                    for (int dx = -1; dx <= 1; dx += 2)
                    {
                        drawString(rd, s, x + dx, y + dy, w, h, spacing);
                    }
                }
            }

            // Draw foreground
            rd.setColor(new Color4(color.r * b, color.g * b, color.b * b, color.a));
            Vector2 bounds = drawString(rd, s, x, y, w, h, spacing);

            rd.endPrimitive();

            rd.popState();

            return bounds;
        }

        /** @deprecated  Use the version that accepts a RenderDevice as the 1st argument.
         */
        public Vector2 draw2D(
            string s,
            Vector2 pos2D,
            double size = 12,
            Color4 color = default(Color4),
            Color4 outline = default(Color4),
            XAlign xalign = XAlign.XALIGN_LEFT,
            YAlign yalign = YAlign.YALIGN_TOP,
            Spacing spacing = Spacing.PROPORTIONAL_SPACING)
        {
            if (renderDevice == null)
            {
                throw new InvalidOperationException("This GFont was not created with a RenderDevice.");
            }
            return draw2D(renderDevice, s, pos2D, size, color, outline, xalign, yalign, spacing);
        }

        /**
         Text is visible from behind.  The text is oriented so that it
         reads "forward" when the pos3D z-axis points towards the viewer.

         Note that text, like all transparent objects, should be rendered
         in back to front sorted order to achieve proper alpha blending.

         @param size In meters.
        */
        public Vector2 draw3D(
            RenderDevice rd,
            string s,
            CoordinateFrame pos3D,
            double size = .1,
            Color4 color = default(Color4),
            Color4 outline = default(Color4),
            XAlign xalign = XAlign.XALIGN_LEFT,
            YAlign yalign = YAlign.YALIGN_TOP,
            Spacing spacing = Spacing.PROPORTIONAL_SPACING)
        {
            Debug.debugAssert(rd != null);

            if (rd == null)
            {
                throw new ArgumentNullException("rd");
            }

            if (color == null)
            {
                color = new Color4(Color3.black(), 1.0f);
            }

            if (outline == null)
            {
                outline = new Color4(0, 0, 0, 0);
            }

            double x = 0;
            double y = 0;

            double h = size * 1.5;
            double w = h * glyphWidth / glyphHeight;

            switch (xalign)
            {
                case XAlign.XALIGN_RIGHT:
                    x -= get2DStringBounds(s, size, spacing).x;
                    break;

                case XAlign.XALIGN_CENTER:
                    x -= get2DStringBounds(s, size, spacing).x / 2;
                    break;

                default:
                    break;
            }

            switch (yalign)
            {
                case YAlign.YALIGN_CENTER:
                    y -= h / 2.0;
                    break;

                case YAlign.YALIGN_BASELINE:
                    y -= baseline * h / (double)glyphHeight;
                    break;

                case YAlign.YALIGN_BOTTOM:
                    y -= h;
                    break;

                default:
                    break;
            }

            float[] m = new float[]
            {
                1.0f / fontTexture.pointer().texelWidth(), 0, 0, 0,
                0, 1.0f / fontTexture.pointer().texelHeight(), 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            };

            rd.pushState();

            // We need to get out of Y=up coordinates.
            CoordinateFrame flipY = CoordinateFrame.identity();
            flipY.rotation.setElt(1, 1, -1.0f);
            rd.setObjectToWorldMatrix(pos3D * flipY);

            rd.setCullFace(RenderDevice.CullFace.CULL_NONE);
            rd.setTextureMatrix(0, m);
            rd.setTexture(0, fontTexture.pointer());
            rd.setTextureCombineMode(0, RenderDevice.CombineMode.TEX_MODULATE);
            rd.setBlendFunc(RenderDevice.BlendFunc.BLEND_SRC_ALPHA,
                            RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_ALPHA);
            rd.setAlphaTest(RenderDevice.AlphaTest.ALPHA_GEQUAL, 0.05);

            rd.disableLighting();
            rd.beginPrimitive(RenderDevice.Primitive.QUADS);

            if (outline.a > 0.05f)
            {
                // Make the equivalent of a 3D "1 pixel" offset (the
                // default 2D text size is 12-pt with a 1pix border)

                double borderOffset = size / 12.0;
                rd.setColor(outline);
                for (int dy = -1; dy <= 1; dy += 2)
                {
                    for (int dx = -1; dx <= 1; dx += 2)
                    {
                        drawString(rd, s,
                                   x + dx * borderOffset,
                                   y + dy * borderOffset,
                                   w, h, spacing);
                    }
                }
            }

            float brightScale = (float)rd.getBrightScale();
            rd.setColor(
                new Color4(color.r * brightScale,
                           color.g * brightScale,
                           color.b * brightScale,
                           color.a));
            Vector2 bounds = drawString(rd, s, x, y, w, h, spacing);

            rd.endPrimitive();

            rd.popState();

            return bounds;
        }

        /** @deprecated Use the version that accepts a RenderDevice as the 1st argument.*/
        public Vector2 draw3D(
            string s,
            CoordinateFrame pos3D,
            double size = .1,
            Color4 color = default(Color4),
            Color4 outline = default(Color4),
            XAlign xalign = XAlign.XALIGN_LEFT,
            YAlign yalign = YAlign.YALIGN_TOP,
            Spacing spacing = Spacing.PROPORTIONAL_SPACING)
        {
            if (renderDevice == null)
            {
                throw new InvalidOperationException("This GFont was not created with a RenderDevice.");
            }
            return draw3D(renderDevice, s, pos3D, size, color, outline, xalign, yalign, spacing);
        }

        /**
         Useful for drawing centered text and boxes around text.
         */
        public Vector2 get2DStringBounds(
            string s,
            double size = 12,
            Spacing spacing = Spacing.PROPORTIONAL_SPACING)
        {
            int n = s.Length;

            double h = size * 1.5;
            double w = h * glyphWidth / glyphHeight;
            double propW = w / glyphWidth;
            double x = 0;
            double y = h;

            if (spacing == Spacing.PROPORTIONAL_SPACING)
            {
                for (int i = 0; i < n; ++i)
                {
                    char c = (char)(s[i] & 127);
                    x += propW * subWidth[(int)c];
                }
            }
            else
            {
                x = subWidth[(int)'M'] * n * 0.85 * propW;
            }

            return new Vector2((float)x, (float)y);
        }

        /** The width of a string when drawn at the given size. */
        public double stringWidth(
            string s,
            double size = 12,
            Spacing spacing = Spacing.PROPORTIONAL_SPACING)
        {
            return get2DStringBounds(s, size, spacing).x;
        }

        /** The bounds of a string drawn in 2D. */
        public Vector2 get2D(
            string s,
            double size = 12,
            Spacing spacing = Spacing.PROPORTIONAL_SPACING)
        {
            return get2DStringBounds(s, size, spacing);
        }

        /** The bounds of a string drawn in 3D, where the size is in meters. */
        public Vector2 get3D(
            string s,
            double size = .1,
            Spacing spacing = Spacing.PROPORTIONAL_SPACING)
        {
            return get2DStringBounds(s, size, spacing);
        }

        /** Assumes you are already inside of beginPrimitive(QUADS) */
        private Vector2 drawString(
            RenderDevice rd,
            string s,
            double x,
            double y,
            double w,
            double h,
            Spacing spacing)
        {
            double propW = w / glyphWidth;
            int n = s.Length;

            // Shrink the vertical texture coordinates by 1 texel to avoid
            // bilinear interpolation interactions with mipmapping.
            float sy = (float)(h / glyphHeight);

            float x0 = 0;

            for (int i = 0; i < n; ++i)
            {
                char c = (char)(s[i] & 127); // s[i] % 128; avoid using illegal chars

                if (c != ' ')
                {
                    int row = c / 16;
                    int col = c & 15; // c % 16

                    // Fixed width
                    float sx = 0;

                    if (spacing == Spacing.PROPORTIONAL_SPACING)
                    {
                        sx = (glyphWidth - subWidth[(int)c]) * (float)propW * 0.5f;
                    }

                    float xx = (float)(x - sx);

                    rd.setTexCoord(0, new Vector2(col * glyphWidth, row * glyphHeight + 1));
                    rd.sendVertex(new Vector2(xx, (float)y + sy));

                    rd.setTexCoord(0, new Vector2(col * glyphWidth, (row + 1) * glyphHeight - 2));
                    rd.sendVertex(new Vector2(xx, (float)(y + h - sy)));

                    xx += (float)w;

                    rd.setTexCoord(0, new Vector2((col + 1) * glyphWidth - 1, (row + 1) * glyphHeight - 2));
                    rd.sendVertex(new Vector2(xx, (float)(y + h - sy)));

                    rd.setTexCoord(0, new Vector2((col + 1) * glyphWidth - 1, row * glyphHeight + 1));
                    rd.sendVertex(new Vector2(xx, (float)y + sy));
                }

                if (spacing == Spacing.PROPORTIONAL_SPACING)
                {
                    x += propW * subWidth[(int)c];
                }
                else
                {
                    x += propW * subWidth[(int)'M'] * 0.85f;
                }
            }

            return new Vector2((float)(x - x0), (float)h);
        }

        /**
         Packs vertices for rendering the string
         into the array as tex/vertex, tex/vertex, ...
         */
        private Vector2 computePackedArray(
            string s,
            double x,
            double y,
            double w,
            double h,
            Spacing spacing,
            Vector2[] array)
        {
            double propW = w / glyphWidth;
            int n = s.Length;

            // Shrink the vertical texture coordinates by 1 texel to avoid
            // bilinear interpolation interactions with mipmapping.
            float sy = (float)(h / glyphHeight);

            float x0 = 0;

            float mwidth = subWidth[(int)'M'] * 0.85f * (float)propW;

            int count = -1;
            for (int i = 0; i < n; ++i)
            {
                char c = (char)(s[i] & 127); // s[i] % 128; avoid using illegal chars

                if (c != ' ')
                {
                    int row = c >> 4; // fast version of c / 16
                    int col = c & 15; // fast version of c % 16

                    // Fixed width
                    float sx = (spacing == Spacing.PROPORTIONAL_SPACING) ?
                        (glyphWidth - subWidth[(int)c]) * (float)propW * 0.5f : 0.0f;

                    float xx = (float)(x - sx);
                    // Tex, Vert
                    ++count;
                    array[count].x = col * glyphWidth;
                    array[count].y = row * glyphHeight + 1;

                    ++count;
                    array[count].x = xx;
                    array[count].y = (float)(y + sy);

                    ++count;
                    array[count].x = col * glyphWidth;
                    array[count].y = (row + 1) * glyphHeight - 2;

                    ++count;
                    array[count].x = xx;
                    array[count].y = (float)(y + h - sy);

                    xx += (float)w;
                    ++count;
                    array[count].x = (col + 1) * glyphWidth - 1;
                    array[count].y = (row + 1) * glyphHeight - 2;

                    ++count;
                    array[count].x = xx;
                    array[count].y = (float)(y + h - sy);

                    ++count;
                    array[count].x = (col + 1) * glyphWidth - 1;
                    array[count].y = row * glyphHeight + 1;

                    ++count;
                    array[count].x = xx;
                    array[count].y = (float)(y + sy);
                }

                x += (spacing == Spacing.PROPORTIONAL_SPACING) ?
                    propW * subWidth[(int)c] : mwidth;
            }

            return new Vector2((float)(x - x0), (float)h);
        }
    }

    /**
     @deprecated
     */
    public class CFont : GFont
    {
        internal CFont(RenderDevice renderDevice, string filename, BinaryInput b)
            : base(renderDevice, filename, b)
        {
        }
    }
}

/**
  @file GImage_jpeg.cpp

  Decodes and encodes JPEG and PNG images using System.Drawing.  The core
  G3D library does not depend on System.Drawing, so the codec lives in this
  separate assembly.

  @maintainer Morgan McGuire, morgan@graphics3d.com
  @created 2002-05-27
  @edited 2006-01-10

  Copyright 2000-2006, Morgan McGuire.
  All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using G3D;

namespace G3D.Image.SystemDrawing
{
    public sealed class SystemDrawingPngCodec : GImageCodec
    {
        public GImage.Format format
        {
            get { return GImage.Format.PNG; }
        }

        public GImage decode(string filename)
        {
            if (filename == null)
            {
                throw new ArgumentNullException("filename");
            }
            try
            {
                byte[] data = File.ReadAllBytes(filename);
                return decode(data, data.Length);
            }
            catch (GImage.Error)
            {
                throw;
            }
            catch (Exception)
            {
                throw new GImage.Error("Unable to read the PNG file.", filename);
            }
        }

        public GImage decode(byte[] data, int length)
        {
            validateBuffer(data, length);
            try
            {
                using (MemoryStream stream = new MemoryStream(data, 0, length))
                using (Bitmap bitmap = new Bitmap(stream))
                {
                    return fromBitmap(bitmap, data, length);
                }
            }
            catch (GImage.Error)
            {
                throw;
            }
            catch (Exception)
            {
                throw new GImage.Error("The PNG image could not be decoded.", "<memory>");
            }
        }

        public byte[] encode(GImage image)
        {
            return ManagedPngEncoder.encode(image);
        }

        private static GImage fromBitmap(Bitmap bitmap, byte[] data, int length)
        {
            int pngColorType = getPNGColorType(data, length);
            bool transparency = hasPNGTransparency(data, length);
            bool alpha;
            bool grayscale;
            if (pngColorType >= 0)
            {
                alpha = pngColorType == 4 || pngColorType == 6 || transparency;
                grayscale = pngColorType == 0 && !alpha;
                if (pngColorType == 3 && !transparency && isGrayscalePalette(bitmap))
                {
                    grayscale = true;
                }
            }
            else
            {
                alpha = global::System.Drawing.Image.IsAlphaPixelFormat(bitmap.PixelFormat);
                grayscale = false;
            }

            int channels = alpha ? 4 : (grayscale ? 1 : 3);
            byte[] pixels = new byte[bitmap.Width * bitmap.Height * channels];
            int index = 0;
            for (int y = 0; y < bitmap.Height; ++y)
            {
                for (int x = 0; x < bitmap.Width; ++x)
                {
                    Color color = bitmap.GetPixel(x, y);
                    if (channels == 1)
                    {
                        pixels[index++] = color.R;
                    }
                    else
                    {
                        pixels[index++] = color.R;
                        pixels[index++] = color.G;
                        pixels[index++] = color.B;
                        if (channels == 4)
                        {
                            pixels[index++] = color.A;
                        }
                    }
                }
            }
            return GImage.fromMemory(pixels, bitmap.Width, bitmap.Height, channels);
        }

        private static int getPNGColorType(byte[] data, int length)
        {
            if (data == null || length < 26 || data[12] != (byte)'I' || data[13] != (byte)'H' ||
                data[14] != (byte)'D' || data[15] != (byte)'R')
            {
                return -1;
            }
            return data[25];
        }

        private static bool hasPNGTransparency(byte[] data, int length)
        {
            if (data == null || length < 8)
            {
                return false;
            }
            int position = 8;
            while (position + 12 <= length)
            {
                int chunkLength = (data[position] << 24) | (data[position + 1] << 16) |
                                  (data[position + 2] << 8) | data[position + 3];
                if (chunkLength < 0 || chunkLength > length - position - 12)
                {
                    break;
                }
                if (data[position + 4] == (byte)'t' && data[position + 5] == (byte)'R' &&
                    data[position + 6] == (byte)'N' && data[position + 7] == (byte)'S')
                {
                    return true;
                }
                position += 12 + chunkLength;
            }
            return false;
        }

        private static bool isGrayscalePalette(Bitmap bitmap)
        {
            if (bitmap.PixelFormat != PixelFormat.Format8bppIndexed)
            {
                return false;
            }
            ColorPalette palette = bitmap.Palette;
            for (int i = 0; i < palette.Entries.Length; ++i)
            {
                Color color = palette.Entries[i];
                if (color.R != color.G || color.G != color.B)
                {
                    return false;
                }
            }
            return true;
        }

        private static void validateBuffer(byte[] data, int length)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            if (length < 0 || length > data.Length)
            {
                throw new ArgumentOutOfRangeException("length");
            }
            if (length < 8)
            {
                throw new GImage.Error("The PNG buffer is too short.", "<memory>");
            }
        }
    }

    public sealed class SystemDrawingJpegCodec : GImageCodec
    {
        public GImage.Format format
        {
            get { return GImage.Format.JPEG; }
        }

        public GImage decode(string filename)
        {
            if (filename == null)
            {
                throw new ArgumentNullException("filename");
            }
            try
            {
                byte[] data = File.ReadAllBytes(filename);
                return decode(data, data.Length);
            }
            catch (GImage.Error)
            {
                throw;
            }
            catch (Exception)
            {
                throw new GImage.Error("Unable to read the JPEG file.", filename);
            }
        }

        public GImage decode(byte[] data, int length)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            if (length < 0 || length > data.Length)
            {
                throw new ArgumentOutOfRangeException("length");
            }
            if (length < 2)
            {
                throw new GImage.Error("The JPEG buffer is too short.", "<memory>");
            }

            try
            {
                using (MemoryStream stream = new MemoryStream(data, 0, length))
                using (Bitmap bitmap = new Bitmap(stream))
                {
                    byte[] pixels = new byte[bitmap.Width * bitmap.Height * 3];
                    int index = 0;
                    for (int y = 0; y < bitmap.Height; ++y)
                    {
                        for (int x = 0; x < bitmap.Width; ++x)
                        {
                            Color color = bitmap.GetPixel(x, y);
                            pixels[index++] = color.R;
                            pixels[index++] = color.G;
                            pixels[index++] = color.B;
                        }
                    }
                    return GImage.fromMemory(pixels, bitmap.Width, bitmap.Height, 3);
                }
            }
            catch (GImage.Error)
            {
                throw;
            }
            catch (Exception)
            {
                throw new GImage.Error("The JPEG image could not be decoded.", "<memory>");
            }
        }

        public byte[] encode(GImage image)
        {
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }
            if (image.channels != 1 && image.channels != 3 && image.channels != 4)
            {
                throw new GImage.Error("JPEG encoding requires one, three or four channels.");
            }
            if (image.width <= 0 || image.height <= 0)
            {
                throw new GImage.Error("JPEG encoding requires positive dimensions.");
            }
            byte[] source = image.getBytes();
            if (source == null)
            {
                throw new GImage.Error("The image has no pixel data.");
            }

            try
            {
                using (Bitmap bitmap = new Bitmap(image.width, image.height, PixelFormat.Format24bppRgb))
                {
                    int count = image.width * image.height;
                    for (int i = 0; i < count; ++i)
                    {
                        int x = i % image.width;
                        int y = i / image.width;
                        byte r;
                        byte g;
                        byte b;
                        if (image.channels == 1)
                        {
                            r = source[i];
                            g = r;
                            b = r;
                        }
                        else
                        {
                            r = source[i * image.channels];
                            g = source[i * image.channels + 1];
                            b = source[i * image.channels + 2];
                        }
                        bitmap.SetPixel(x, y, Color.FromArgb(r, g, b));
                    }

                    ImageCodecInfo encoder = findJPEGEncoder();
                    if (encoder == null)
                    {
                        throw new GImage.Error("A JPEG encoder is not available.");
                    }
                    using (EncoderParameters parameters = new EncoderParameters(1))
                    {
                        parameters.Param[0] = new EncoderParameter(Encoder.Quality, 96L);
                        using (MemoryStream stream = new MemoryStream())
                        {
                            bitmap.Save(stream, encoder, parameters);
                            return stream.ToArray();
                        }
                    }
                }
            }
            catch (GImage.Error)
            {
                throw;
            }
            catch (Exception)
            {
                throw new GImage.Error("The JPEG image could not be encoded.", "<memory>");
            }
        }

        private static ImageCodecInfo findJPEGEncoder()
        {
            ImageCodecInfo[] encoders = ImageCodecInfo.GetImageEncoders();
            for (int i = 0; i < encoders.Length; ++i)
            {
                if (encoders[i].MimeType == "image/jpeg")
                {
                    return encoders[i];
                }
            }
            return null;
        }
    }

    public sealed class SystemDrawingImageDecoder : GImageDecoder
    {
        private static readonly string[] extensions = new string[] { ".jpg", ".jpeg", ".jpe", ".png", ".bmp", ".gif", ".tif", ".tiff" };
        private static readonly SystemDrawingPngCodec pngCodec = new SystemDrawingPngCodec();
        private static readonly SystemDrawingJpegCodec jpegCodec = new SystemDrawingJpegCodec();
        private static bool registered;

        public static void register()
        {
            if (registered)
            {
                return;
            }
            registered = true;
            GImage.registerDecoder(new SystemDrawingImageDecoder());
            GImage.registerCodec(pngCodec);
            GImage.registerCodec(jpegCodec);
        }

        public bool canDecode(string filename)
        {
            if (filename == null)
            {
                return false;
            }
            string extension = Path.GetExtension(filename).ToLowerInvariant();
            foreach (string candidate in extensions)
            {
                if (candidate == extension)
                {
                    return true;
                }
            }
            return false;
        }

        public bool canDecode(byte[] data, int length)
        {
            if (data == null || length < 2 || length > data.Length)
            {
                return false;
            }
            if (data[0] == 0xFF && data[1] == 0xD8)
            {
                return true;
            }
            if (length >= 4 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
            {
                return true;
            }
            if (length >= 2 && data[0] == 0x42 && data[1] == 0x4D)
            {
                return true;
            }
            if (length >= 3 && data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46)
            {
                return true;
            }
            return false;
        }

        public GImage decode(string filename)
        {
            if (filename == null)
            {
                throw new ArgumentNullException("filename");
            }
            byte[] data = File.ReadAllBytes(filename);
            return decode(data, data.Length);
        }

        public GImage decode(byte[] data, int length)
        {
            if (data == null || length < 0 || length > data.Length)
            {
                throw new ArgumentException("The image buffer is invalid.");
            }
            if (length >= 2 && data[0] == 0xFF && data[1] == 0xD8)
            {
                return jpegCodec.decode(data, length);
            }
            if (length >= 4 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
            {
                return pngCodec.decode(data, length);
            }
            if (length >= 2 && data[0] == 0x42 && data[1] == 0x4D)
            {
                return new GImage(data, length, GImage.Format.BMP);
            }
            return decodeOther(data, length);
        }

        private static GImage decodeOther(byte[] data, int length)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream(data, 0, length))
                using (Bitmap bitmap = new Bitmap(stream))
                {
                    bool alpha = global::System.Drawing.Image.IsAlphaPixelFormat(bitmap.PixelFormat);
                    int channels = alpha ? 4 : 3;
                    byte[] pixels = new byte[bitmap.Width * bitmap.Height * channels];
                    int index = 0;
                    for (int y = 0; y < bitmap.Height; ++y)
                    {
                        for (int x = 0; x < bitmap.Width; ++x)
                        {
                            Color color = bitmap.GetPixel(x, y);
                            pixels[index++] = color.R;
                            pixels[index++] = color.G;
                            pixels[index++] = color.B;
                            if (alpha)
                            {
                                pixels[index++] = color.A;
                            }
                        }
                    }
                    return GImage.fromMemory(pixels, bitmap.Width, bitmap.Height, channels);
                }
            }
            catch (Exception)
            {
                throw new GImage.Error("The image could not be decoded.", "<memory>");
            }
        }
    }

    internal static class ManagedPngEncoder
    {
        private static readonly uint[] crcTable = createCrcTable();

        internal static byte[] encode(GImage image)
        {
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }
            if (image.width <= 0 || image.height <= 0)
            {
                throw new GImage.Error("PNG encoding requires positive dimensions.");
            }
            if (image.channels != 1 && image.channels != 3 && image.channels != 4)
            {
                throw new GImage.Error("PNG encoding requires one, three or four channels.");
            }

            byte[] source = image.getBytes();
            if (source == null)
            {
                throw new GImage.Error("The image has no pixel data.");
            }

            int rowBytes;
            int rawLength;
            try
            {
                rowBytes = checked(image.width * image.channels);
                rawLength = checked((rowBytes + 1) * image.height);
            }
            catch (OverflowException ex)
            {
                throw new GImage.Error("The PNG dimensions are too large.", ex.Message);
            }
            if (source.Length < rowBytes * image.height)
            {
                throw new GImage.Error("The image has no pixel data.");
            }

            byte[] raw = new byte[rawLength];
            for (int y = 0; y < image.height; ++y)
            {
                int destination = y * (rowBytes + 1);
                raw[destination] = 0;
                Array.Copy(source, y * rowBytes, raw, destination + 1, rowBytes);
            }

            byte[] compressed = createZlibStream(raw);
            List<byte> output = new List<byte>(compressed.Length + 64);
            output.AddRange(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

            int colorType = image.channels == 1 ? 0 : (image.channels == 3 ? 2 : 6);
            byte[] header = new byte[13];
            addU32(header, 0, (uint)image.width);
            addU32(header, 4, (uint)image.height);
            header[8] = 8;
            header[9] = (byte)colorType;
            header[10] = 0;
            header[11] = 0;
            header[12] = 0;
            addChunk(output, "IHDR", header);
            addChunk(output, "IDAT", compressed);
            addChunk(output, "IEND", new byte[0]);
            return output.ToArray();
        }

        private static byte[] createZlibStream(byte[] data)
        {
            List<byte> output = new List<byte>(data.Length + data.Length / 65535 * 5 + 16);
            output.Add(0x78);
            output.Add(0x01);

            int offset = 0;
            while (offset < data.Length)
            {
                int count = data.Length - offset;
                if (count > 65535)
                {
                    count = 65535;
                }
                output.Add((byte)(offset + count == data.Length ? 1 : 0));
                ushort inverse = (ushort)(count ^ 0xFFFF);
                output.Add((byte)(count & 0xFF));
                output.Add((byte)((count >> 8) & 0xFF));
                output.Add((byte)(inverse & 0xFF));
                output.Add((byte)((inverse >> 8) & 0xFF));
                for (int i = 0; i < count; ++i)
                {
                    output.Add(data[offset + i]);
                }
                offset += count;
            }

            uint adler = computeAdler32(data);
            addU32(output, adler);
            return output.ToArray();
        }

        private static void addChunk(List<byte> output, string type, byte[] data)
        {
            byte[] typeBytes = global::System.Text.Encoding.ASCII.GetBytes(type);
            addU32(output, (uint)data.Length);
            output.AddRange(typeBytes);
            output.AddRange(data);

            byte[] crcInput = new byte[typeBytes.Length + data.Length];
            Array.Copy(typeBytes, crcInput, typeBytes.Length);
            Array.Copy(data, 0, crcInput, typeBytes.Length, data.Length);
            addU32(output, computeCRC32(crcInput));
        }

        private static uint computeCRC32(byte[] data)
        {
            uint crc = 0xFFFFFFFF;
            for (int i = 0; i < data.Length; ++i)
            {
                crc = crcTable[(crc ^ data[i]) & 0xFF] ^ (crc >> 8);
            }
            return ~crc;
        }

        private static uint[] createCrcTable()
        {
            uint[] table = new uint[256];
            for (int i = 0; i < table.Length; ++i)
            {
                uint value = (uint)i;
                for (int bit = 0; bit < 8; ++bit)
                {
                    value = (value & 1) != 0 ? 0xEDB88320 ^ (value >> 1) : value >> 1;
                }
                table[i] = value;
            }
            return table;
        }

        private static uint computeAdler32(byte[] data)
        {
            uint a = 1;
            uint b = 0;
            for (int i = 0; i < data.Length; ++i)
            {
                a = (a + data[i]) % 65521;
                b = (b + a) % 65521;
            }
            return (b << 16) | a;
        }

        private static void addU32(byte[] data, int offset, uint value)
        {
            data[offset] = (byte)(value >> 24);
            data[offset + 1] = (byte)(value >> 16);
            data[offset + 2] = (byte)(value >> 8);
            data[offset + 3] = (byte)value;
        }

        private static void addU32(List<byte> data, uint value)
        {
            data.Add((byte)(value >> 24));
            data.Add((byte)(value >> 16));
            data.Add((byte)(value >> 8));
            data.Add((byte)value);
        }
    }

    public static class SystemDrawingImageCodec
    {
        public static void register()
        {
            SystemDrawingImageDecoder.register();
        }
    }
}

/**
  @file GImage.h

  See G3D::GImage for details.

  @cite JPEG compress/decompressor is the <A HREF="http://www.ijg.org/files/">IJG library</A>, used in accordance with their license.
  @cite JPG code by John Chisholm, using the IJG Library
  @cite TGA code by Morgan McGuire
  @cite BMP code by John Chisholm, based on code by Edward "CGameProgrammer" Resnick <A HREF="mailto:cgp@gdnmail.net">mailto:cgp@gdnmail.net</A> at <A HREF="ftp://ftp.flipcode.com/cotd/LoadPicture.txt">ftp://ftp.flipcode.com/cotd/LoadPicture.txt</A>
  @cite PCX format described in the ZSOFT PCX manual http://www.nist.fss.ru/hr/doc/spec/pcx.htm#2
  @cite PNG compress/decompressor is the <A HREF="http://www.libpng.org/pub/png/libpng.html">libpng library</A>, used in accordance with their license.
  @cite PPM code by Morgan McGuire based on http://netpbm.sourceforge.net/doc/ppm.html

  @maintainer Morgan McGuire, morgan@graphics3d.com
  @created 2002-05-27
  @edited  2006-01-10

  Copyright 2000-2006, Morgan McGuire.
  All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.IO;

namespace G3D
{
    /**
      Interface to image compression and file formats.

      Pixel data is stored in a managed byte array.  The core implementation
      contains the BMP, TGA, PPM, PCX and ICO decoders and the BMP, TGA and PPM
      encoders.  PNG and JPEG codecs are supplied by a desktop assembly and
      are selected through the registered format codecs.
     */
    public sealed class GImage : IDisposable
    {
        /**
          Thrown when an image cannot be loaded or decoded.
         */
        public class Error : Exception
        {
            public string reason;
            public string filename;

            public Error(string reason, string filename = "")
                : base(filename == "" ? reason : reason + ": " + filename)
            {
                this.reason = reason;
                this.filename = filename;
            }
        }

        public enum Format {JPEG, BMP, TGA, PCX, ICO, PNG, PPM_ASCII, PPM, AUTODETECT, UNKNOWN};

        public int width;
        public int height;

        /**
          The number of channels; either 1 (L8), 3 (RGB) or 4 (RGBA).
         */
        public int channels;

        private byte[] _byte;

        private static readonly List<GImageDecoder> decoders = new List<GImageDecoder>();
        private static readonly List<GImageFormatDecoder> formatDecoders = new List<GImageFormatDecoder>();
        private static readonly List<GImageEncoder> encoders = new List<GImageEncoder>();

        /**
          Registers a decoder that is able to decode files that the core
          library cannot decode itself.
         */
        public static void registerDecoder(GImageDecoder decoder)
        {
            if (decoder == null)
            {
                throw new ArgumentNullException("decoder");
            }

            if (!decoders.Contains(decoder))
            {
                decoders.Add(decoder);
            }

            GImageFormatDecoder formatDecoder = decoder as GImageFormatDecoder;
            if (formatDecoder != null)
            {
                registerFormatDecoder(formatDecoder);
            }
        }

        /**
          Registers a decoder for one explicit image format.
         */
        public static void registerFormatDecoder(GImageFormatDecoder decoder)
        {
            if (decoder == null)
            {
                throw new ArgumentNullException("decoder");
            }

            if (!formatDecoders.Contains(decoder))
            {
                formatDecoders.Add(decoder);
            }
        }

        /**
          Registers an encoder for one explicit image format.
         */
        public static void registerEncoder(GImageEncoder encoder)
        {
            if (encoder == null)
            {
                throw new ArgumentNullException("encoder");
            }

            if (!encoders.Contains(encoder))
            {
                encoders.Add(encoder);
            }
        }

        /**
          Registers a codec that can decode and encode one explicit format.
         */
        public static void registerCodec(GImageCodec codec)
        {
            if (codec == null)
            {
                throw new ArgumentNullException("codec");
            }

            registerFormatDecoder(codec);
            registerEncoder(codec);
        }

        /**
          Returns the registered legacy decoders.
         */
        public static GImageDecoder[] getDecoders()
        {
            return decoders.ToArray();
        }

        /**
          Returns the registered format-specific decoders.
         */
        public static GImageFormatDecoder[] getFormatDecoders()
        {
            return formatDecoders.ToArray();
        }

        /**
          Returns the registered format-specific encoders.
         */
        public static GImageEncoder[] getEncoders()
        {
            return encoders.ToArray();
        }

        /**
          Returns a copy of the raw pixel bytes.  There are
          width * height * channels bytes.
         */
        public byte[] getBytes()
        {
            return _byte;
        }

        /**
          Sets the raw pixel bytes.  There must be width * height * channels
          bytes.
         */
        public void setBytes(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            int expected = checked(width * height * channels);
            if (data.Length != expected)
            {
                throw new ArgumentException("The byte array does not match the image dimensions.", "data");
            }

            _byte = data;
        }

        /**
          Returns the pixel at (x, y), where (0,0) is the upper left.
         */
        public byte pixel1(int x, int y)
        {
            checkPixel(x, y);
            return _byte[x + y * width];
        }

        /**
          Returns the pixel at (x, y), where (0,0) is the upper left.
         */
        public Color3uint8 pixel3(int x, int y)
        {
            checkPixel(x, y);
            if (channels != 3)
            {
                throw new InvalidOperationException("The image does not have three channels.");
            }

            int i = (x + y * width) * 3;
            return new Color3uint8(_byte[i], _byte[i + 1], _byte[i + 2]);
        }

        /**
          Returns the pixel at (x, y), where (0,0) is the upper left.
         */
        public Color4uint8 pixel4(int x, int y)
        {
            checkPixel(x, y);
            if (channels != 4)
            {
                throw new InvalidOperationException("The image does not have four channels.");
            }

            int i = (x + y * width) * 4;
            return new Color4uint8(_byte[i], _byte[i + 1], _byte[i + 2], _byte[i + 3]);
        }

        /**
          Sets the pixel at (x, y), where (0,0) is the upper left.
         */
        public void setPixel1(int x, int y, byte value)
        {
            checkPixel(x, y);
            _byte[x + y * width] = value;
        }

        /**
          Sets the pixel at (x, y), where (0,0) is the upper left.
         */
        public void setPixel3(int x, int y, Color3uint8 value)
        {
            checkPixel(x, y);
            if (channels != 3)
            {
                throw new InvalidOperationException("The image does not have three channels.");
            }

            int i = (x + y * width) * 3;
            _byte[i] = value.r;
            _byte[i + 1] = value.g;
            _byte[i + 2] = value.b;
        }

        /**
          Sets the pixel at (x, y), where (0,0) is the upper left.
         */
        public void setPixel4(int x, int y, Color4uint8 value)
        {
            checkPixel(x, y);
            if (channels != 4)
            {
                throw new InvalidOperationException("The image does not have four channels.");
            }

            int i = (x + y * width) * 4;
            _byte[i] = value.r;
            _byte[i + 1] = value.g;
            _byte[i + 2] = value.b;
            _byte[i + 3] = value.a;
        }

        /**
          Returns a copy of the upper-left pixels as Color3uint8 values.
         */
        public Color3uint8[] pixel3()
        {
            if (channels != 3)
            {
                throw new InvalidOperationException("The image does not have three channels.");
            }

            int count = checked(width * height);
            Color3uint8[] result = new Color3uint8[count];
            for (int i = 0; i < count; ++i)
            {
                result[i] = new Color3uint8(_byte[i * 3], _byte[i * 3 + 1], _byte[i * 3 + 2]);
            }
            return result;
        }

        /**
          Returns a copy of the upper-left pixels as Color4uint8 values.
         */
        public Color4uint8[] pixel4()
        {
            if (channels != 4)
            {
                throw new InvalidOperationException("The image does not have four channels.");
            }

            int count = checked(width * height);
            Color4uint8[] result = new Color4uint8[count];
            for (int i = 0; i < count; ++i)
            {
                result[i] = new Color4uint8(_byte[i * 4], _byte[i * 4 + 1], _byte[i * 4 + 2], _byte[i * 4 + 3]);
            }
            return result;
        }

        private void checkPixel(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                throw new ArgumentOutOfRangeException("x", "The pixel is outside the image.");
            }
        }

        /**
          Given a filename, returns the most likely format of the file.  If the
          file exists, its header is inspected; otherwise the extension is used.
         */
        public static Format resolveFormat(string filename)
        {
            if (filename == null)
            {
                throw new ArgumentNullException("filename");
            }

            if (!File.Exists(filename))
            {
                throw new Error("File not found.", filename);
            }

            byte[] data = File.ReadAllBytes(filename);
            return resolveFormat(filename, data, data.Length, Format.AUTODETECT);
        }

        /**
          Given a filename, a buffer and an optional format, returns the
          selected format.  A non-AUTODETECT format is always returned
          unchanged, even when the filename or buffer suggests another format.
         */
        public static Format resolveFormat(string filename, byte[] data, int length, Format maybeFormat)
        {
            if (maybeFormat != Format.AUTODETECT)
            {
                return maybeFormat;
            }

            if (filename == null)
            {
                filename = "";
            }

            if (data == null)
            {
                length = 0;
            }
            else if (length < 0 || length > data.Length)
            {
                throw new ArgumentOutOfRangeException("length");
            }

            string extension = "";
            try
            {
                extension = Path.GetExtension(filename);
            }
            catch (ArgumentException)
            {
                extension = "";
            }

            if (extension.StartsWith(".", StringComparison.Ordinal))
            {
                extension = extension.Substring(1);
            }
            extension = StringUtils.toUpper(extension);

            if (extension == "PPM" && data != null && length > 2)
            {
                if (data[0] == (byte)'P' && data[1] == (byte)'6')
                {
                    return Format.PPM;
                }
                if (data[0] == (byte)'P' &&
                    (data[1] == (byte)'1' || data[1] == (byte)'2' || data[1] == (byte)'3'))
                {
                    return Format.PPM_ASCII;
                }
            }

            Format byExtension = stringToFormat(extension);
            if (byExtension != Format.UNKNOWN)
            {
                return byExtension;
            }

            if (data == null || length == 0)
            {
                return Format.UNKNOWN;
            }

            if (length >= 2 && data[0] == (byte)'P' && data[1] == (byte)'6')
            {
                return Format.PPM;
            }
            if (length >= 2 && data[0] == (byte)'P' &&
                (data[1] == (byte)'1' || data[1] == (byte)'2' || data[1] == (byte)'3'))
            {
                return Format.PPM_ASCII;
            }
            if (length >= 8 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E &&
                data[3] == 0x47 && data[4] == 0x0D && data[5] == 0x0A &&
                data[6] == 0x1A && data[7] == 0x0A)
            {
                return Format.PNG;
            }
            if (length >= 2 && data[0] == (byte)'B' && data[1] == (byte)'M')
            {
                return Format.BMP;
            }
            if (length >= 2 && data[0] == 0xFF && data[1] == 0xD8)
            {
                return Format.JPEG;
            }
            if (length >= 40 && data[length - 18] == (byte)'T')
            {
                string footer = global::System.Text.Encoding.ASCII.GetString(data, length - 18, 16);
                if (footer == "TRUEVISION-XFILE")
                {
                    return Format.TGA;
                }
            }
            if (length >= 4 && data[0] == 0 && data[1] == 0 && data[2] == 0 && data[3] == 1)
            {
                return Format.ICO;
            }
            if (data[0] == 10)
            {
                return Format.PCX;
            }

            return Format.UNKNOWN;
        }

        /**
          Given a buffer, returns the detected format.
         */
        public static Format resolveFormat(byte[] data, int length)
        {
            return resolveFormat("", data, length, Format.AUTODETECT);
        }

        /**
          Returns true if the format name is recognized.
         */
        public static bool supportedFormat(string format)
        {
            return stringToFormat(format) != Format.UNKNOWN;
        }

        /**
          Converts a string to an enum, returning UNKNOWN if not recognized.
         */
        public static Format stringToFormat(string format)
        {
            if (format == null)
            {
                return Format.UNKNOWN;
            }

            string extension = StringUtils.toUpper(format);
            if (extension.StartsWith(".", StringComparison.Ordinal))
            {
                extension = extension.Substring(1);
            }

            if (extension == "JPG" || extension == "JPEG")
            {
                return Format.JPEG;
            }
            else if (extension == "TGA")
            {
                return Format.TGA;
            }
            else if (extension == "BMP")
            {
                return Format.BMP;
            }
            else if (extension == "PCX")
            {
                return Format.PCX;
            }
            else if (extension == "ICO")
            {
                return Format.ICO;
            }
            else if (extension == "PNG")
            {
                return Format.PNG;
            }
            else if (extension == "PPM")
            {
                return Format.PPM;
            }
            else
            {
                return Format.UNKNOWN;
            }
        }

        public GImage()
        {
            clear();
        }

        /**
          Load an encoded image from disk and decode it.
          Throws GImage.Error if something goes wrong.
         */
        public GImage(string filename, Format format = Format.AUTODETECT)
        {
            load(filename, format);
        }

        /**
          Decodes an image stored in a buffer.
         */
        public GImage(byte[] data, int length, Format format = Format.AUTODETECT)
        {
            load(data, length, format);
        }

        /**
          Decodes an image stored in a buffer.
         */
        public GImage(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            load(data, data.Length, Format.AUTODETECT);
        }

        /**
          Create an empty image of the given size.
         */
        public GImage(int width, int height, int channels = 3)
        {
            resize(width, height, channels);
        }

        /**
          Creates an independent copy of an image.
         */
        public GImage(GImage other)
        {
            clear();
            copyFrom(other);
        }

        /**
          Loads an image from disk (clearing the old one first).
         */
        public void load(string filename, Format format = Format.AUTODETECT)
        {
            clear();
            if (filename == null)
            {
                throw new ArgumentNullException("filename");
            }

            byte[] data;
            try
            {
                data = File.ReadAllBytes(filename);
            }
            catch (Exception)
            {
                throw new Error("Unable to read the image file.", filename);
            }

            if (data.Length == 0)
            {
                throw new Error("The image file is empty.", filename);
            }

            loadData(data, data.Length, format, filename);
        }

        /**
          Decodes an image stored in a buffer (clearing the old one first).
         */
        public void load(byte[] data, int length, Format format = Format.AUTODETECT)
        {
            clear();
            loadData(data, length, format, "<memory>");
        }

        /**
          Decodes an image stored in a buffer (clearing the old one first).
         */
        public void load(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            load(data, data.Length, Format.AUTODETECT);
        }

        private void loadData(byte[] data, int length, Format format, string filename)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            if (length < 0 || length > data.Length)
            {
                throw new ArgumentOutOfRangeException("length");
            }
            if (length == 0)
            {
                throw new Error("The image buffer is empty.", filename);
            }

            Format selected = resolveFormat(filename, data, length, format);
            if (selected == Format.UNKNOWN && format != Format.AUTODETECT)
            {
                throw new Error("The requested image format is unknown.", filename);
            }
            GImage decoded;
            try
            {
                decoded = decodeData(data, length, selected, filename);
            }
            catch (Error)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Error(ex.Message, filename);
            }

            copyFrom(decoded);
        }

        private static GImage decodeData(byte[] data, int length, Format format, string filename)
        {
            switch (format)
            {
                case Format.BMP:
                    return GImageCoreCodec.decodeBMP(data, length, filename);
                case Format.TGA:
                    return GImageCoreCodec.decodeTGA(data, length, filename);
                case Format.PPM:
                    return GImageCoreCodec.decodePPM(data, length, filename);
                case Format.PPM_ASCII:
                    return GImageCoreCodec.decodePPMASCII(data, length, filename);
                case Format.PCX:
                    return GImageCoreCodec.decodePCX(data, length, filename);
                case Format.ICO:
                    return GImageCoreCodec.decodeICO(data, length, filename);
                case Format.PNG:
                case Format.JPEG:
                    return decodeExternal(data, length, format, filename);
                case Format.UNKNOWN:
                    return decodeLegacy(data, length, filename);
                default:
                    throw new Error("The requested image format cannot be decoded.", filename);
            }
        }

        private static GImage decodeExternal(byte[] data, int length, Format format, string filename)
        {
            foreach (GImageFormatDecoder decoder in formatDecoders)
            {
                if (decoder.format == format)
                {
                    return decoder.decode(data, length);
                }
            }

            foreach (GImageDecoder decoder in decoders)
            {
                if (decoder.canDecode(data, length))
                {
                    return decoder.decode(data, length);
                }
            }

            throw new Error("No decoder is registered for the requested image format.", filename);
        }

        private static GImage decodeLegacy(byte[] data, int length, string filename)
        {
            foreach (GImageDecoder decoder in decoders)
            {
                if (decoder.canDecode(data, length))
                {
                    return decoder.decode(data, length);
                }
            }

            throw new Error("The image format could not be determined.", filename);
        }

        /**
          Frees memory and resets to a 0x0 image.
         */
        public void clear()
        {
            width = 0;
            height = 0;
            channels = 0;
            _byte = null;
        }

        /**
          Deallocates the pixels.
         */
        public void Dispose()
        {
            clear();
        }

        /**
          Resizes the internal buffer to (width x height) with the number of
          channels specified.  All data is set to 0 (black).
         */
        public void resize(int width, int height, int channels)
        {
            if (width < 0)
            {
                throw new ArgumentOutOfRangeException("width");
            }
            if (height < 0)
            {
                throw new ArgumentOutOfRangeException("height");
            }
            if (channels < 1 || channels > 4)
            {
                throw new ArgumentOutOfRangeException("channels");
            }

            int size;
            try
            {
                size = checked(width * height * channels);
            }
            catch (OverflowException ex)
            {
                throw new ArgumentOutOfRangeException("width", ex.Message);
            }

            clear();
            this.width = width;
            this.height = height;
            this.channels = channels;
            _byte = new byte[size];
        }

        /**
          Copies another image into this image.
         */
        public void copyFrom(GImage other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            clear();
            width = other.width;
            height = other.height;
            channels = other.channels;
            _byte = other._byte == null ? null : (byte[])other._byte.Clone();
        }

        /**
          Creates an image from raw, tightly packed pixel bytes.
         */
        public static GImage fromMemory(byte[] data, int width, int height, int channels)
        {
            GImage image = new GImage(width, height, channels);
            image.setBytes(data);
            return image;
        }

        /**
          Returns a new GImage that has 4 channels.  RGB is taken from this
          GImage and the alpha from the red channel of the second image.
         */
        public GImage insertRedAsAlpha(GImage alpha)
        {
            if (alpha == null)
            {
                throw new ArgumentNullException("alpha");
            }
            if (width != alpha.width || height != alpha.height)
            {
                throw new Error("Image sizes do not match");
            }

            GImage result = new GImage(width, height, 4);
            int count = checked(width * height);
            for (int i = 0; i < count; ++i)
            {
                byte r;
                byte g;
                byte b;
                if (channels == 1)
                {
                    r = _byte[i];
                    g = _byte[i];
                    b = _byte[i];
                }
                else
                {
                    r = _byte[i * channels];
                    g = _byte[i * channels + 1];
                    b = _byte[i * channels + 2];
                }
                result._byte[i * 4] = r;
                result._byte[i * 4 + 1] = g;
                result._byte[i * 4 + 2] = b;
                result._byte[i * 4 + 3] = alpha._byte[i * alpha.channels];
            }
            return result;
        }

        /**
          Returns a new GImage with 3 channels, removing the alpha channel if
          there is one.
         */
        public GImage stripAlpha()
        {
            GImage result = new GImage(width, height, 3);
            int count = checked(width * height);
            for (int i = 0; i < count; ++i)
            {
                if (channels == 1)
                {
                    result._byte[i * 3] = _byte[i];
                    result._byte[i * 3 + 1] = _byte[i];
                    result._byte[i * 3 + 2] = _byte[i];
                }
                else
                {
                    result._byte[i * 3] = _byte[i * channels];
                    result._byte[i * 3 + 1] = _byte[i * channels + 1];
                    result._byte[i * 3 + 2] = _byte[i * channels + 2];
                }
            }
            return result;
        }

        /**
          Copies src sub-image data into dest at a certain offset.
         */
        public static bool pasteSubImage(GImage dest, GImage src,
            int destX, int destY, int srcX, int srcY, int srcWidth, int srcHeight)
        {
            if (dest == null || src == null)
            {
                return false;
            }
            if (srcWidth < 0 || srcHeight < 0 || destX < 0 || destY < 0 || srcX < 0 || srcY < 0)
            {
                return false;
            }
            if (src.channels != dest.channels)
            {
                return false;
            }
            if (srcX > src.width - srcWidth || srcY > src.height - srcHeight)
            {
                return false;
            }
            if (destX > dest.width - srcWidth || destY > dest.height - srcHeight)
            {
                return false;
            }

            for (int i = 0; i < srcHeight; ++i)
            {
                int source = ((i + srcY) * src.width + srcX) * src.channels;
                int destination = ((i + destY) * dest.width + destX) * dest.channels;
                Array.Copy(src._byte, source, dest._byte, destination, srcWidth * src.channels);
            }
            return true;
        }

        /**
          Creates dest from src sub-image data.
         */
        public static bool copySubImage(GImage dest, GImage src,
            int srcX, int srcY, int srcWidth, int srcHeight)
        {
            if (dest == null || src == null)
            {
                return false;
            }
            if (srcWidth < 0 || srcHeight < 0 || srcX < 0 || srcY < 0)
            {
                return false;
            }
            if (srcX > src.width - srcWidth || srcY > src.height - srcHeight)
            {
                return false;
            }

            dest.resize(srcWidth, srcHeight, src.channels);
            bool result = pasteSubImage(dest, src, 0, 0, srcX, srcY, srcWidth, srcHeight);
            if (!result)
            {
                throw new InvalidOperationException("The sub-image could not be copied.");
            }
            return true;
        }

        /**
          Converts this image into a 4 channel RGBA image.
         */
        public void convertToRGBA()
        {
            if (channels == 4)
            {
                return;
            }

            int count = checked(width * height);
            byte[] result = new byte[count * 4];
            for (int i = 0; i < count; ++i)
            {
                byte r;
                byte g;
                byte b;
                if (channels == 1)
                {
                    r = _byte[i];
                    g = r;
                    b = r;
                }
                else
                {
                    r = _byte[i * channels];
                    g = _byte[i * channels + 1];
                    b = _byte[i * channels + 2];
                }
                result[i * 4] = r;
                result[i * 4 + 1] = g;
                result[i * 4 + 2] = b;
                result[i * 4 + 3] = 255;
            }
            channels = 4;
            _byte = result;
        }

        /**
          Converts this image into a 3 channel RGB image.
         */
        public void convertToRGB()
        {
            if (channels == 3)
            {
                return;
            }

            int count = checked(width * height);
            byte[] result = new byte[count * 3];
            for (int i = 0; i < count; ++i)
            {
                if (channels == 1)
                {
                    result[i * 3] = _byte[i];
                    result[i * 3 + 1] = _byte[i];
                    result[i * 3 + 2] = _byte[i];
                }
                else
                {
                    result[i * 3] = _byte[i * channels];
                    result[i * 3 + 1] = _byte[i * channels + 1];
                    result[i * 3 + 2] = _byte[i * channels + 2];
                }
            }
            channels = 3;
            _byte = result;
        }

        /**
          Averages color channels if they exist.
         */
        public void convertToL8()
        {
            if (channels == 1)
            {
                return;
            }

            int count = checked(width * height);
            byte[] result = new byte[count];
            for (int i = 0; i < count; ++i)
            {
                result[i] = (byte)((_byte[i * channels] + _byte[i * channels + 1] + _byte[i * channels + 2]) / 3);
            }
            channels = 1;
            _byte = result;
        }

        /**
          Encode and save to disk.
         */
        public void save(string filename, Format format = Format.AUTODETECT)
        {
            if (filename == null)
            {
                throw new ArgumentNullException("filename");
            }

            Format selected = resolveFormat(filename, null, 0, format);
            if (selected == Format.AUTODETECT || selected == Format.UNKNOWN)
            {
                throw new Error("The output image format could not be determined.", filename);
            }

            byte[] data = encode(selected);
            try
            {
                string directory = Path.GetDirectoryName(filename);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllBytes(filename, data);
            }
            catch (Exception)
            {
                throw new Error("Unable to write the image file.", filename);
            }
        }

        /**
          Encodes the image and returns the encoded bytes.
         */
        public byte[] encode(Format format)
        {
            BinaryOutput output = new BinaryOutput();
            encode(format, output);
            int length = output.size();
            byte[] result = new byte[length];
            if (length > 0)
            {
                Array.Copy(output.getCArray(), result, length);
            }
            return result;
        }

        /**
          Encodes the image into the supplied output buffer.
         */
        public void encode(Format format, out byte[] outData, out int outLength)
        {
            outData = encode(format);
            outLength = outData.Length;
        }

        /**
          Does not commit the BinaryOutput when done.
         */
        public void encode(Format format, BinaryOutput output)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }

            byte[] encoded = null;
            switch (format)
            {
                case Format.BMP:
                    encoded = GImageCoreCodec.encodeBMP(this);
                    break;
                case Format.TGA:
                    encoded = GImageCoreCodec.encodeTGA(this);
                    break;
                case Format.PPM:
                    encoded = GImageCoreCodec.encodePPM(this);
                    break;
                case Format.PPM_ASCII:
                    encoded = GImageCoreCodec.encodePPMASCII(this);
                    break;
                case Format.PNG:
                case Format.JPEG:
                {
                    bool encoderFound = false;
                    foreach (GImageEncoder encoder in encoders)
                    {
                        if (encoder.format == format)
                        {
                            encoderFound = true;
                            encoded = encoder.encode(this);
                            break;
                        }
                    }
                    if (!encoderFound)
                    {
                        throw new Error("No encoder is registered for the requested image format.");
                    }
                    break;
                }
                default:
                    throw new Error("The requested image format cannot be encoded.");
            }

            if (encoded == null)
            {
                throw new Error("The registered encoder returned no data.");
            }
            output.writeBytes(encoded, 0, encoded.Length);
        }

        /**
          Returns the size of this object in bytes.
         */
        public int sizeInMemory()
        {
            return checked(32 + width * height * channels);
        }

        /**
          Converts an RGB buffer to RGBA.
         */
        public static void RGBtoRGBA(byte[] input, byte[] output, int numPixels)
        {
            if (input == null || output == null)
            {
                throw new ArgumentNullException(input == null ? "input" : "output");
            }
            if (numPixels < 0)
            {
                throw new ArgumentOutOfRangeException("numPixels");
            }
            if (input.Length < numPixels * 3 || output.Length < numPixels * 4)
            {
                throw new ArgumentException("The image buffer is too small.");
            }

            byte[] source = ReferenceEquals(input, output) ? (byte[])input.Clone() : input;
            for (int i = 0; i < numPixels; ++i)
            {
                output[i * 4] = source[i * 3];
                output[i * 4 + 1] = source[i * 3 + 1];
                output[i * 4 + 2] = source[i * 3 + 2];
                output[i * 4 + 3] = 255;
            }
        }

        /**
          Converts an RGB buffer to BGR.
         */
        public static void RGBtoBGR(byte[] input, byte[] output, int numPixels)
        {
            if (input == null || output == null)
            {
                throw new ArgumentNullException(input == null ? "input" : "output");
            }
            if (numPixels < 0)
            {
                throw new ArgumentOutOfRangeException("numPixels");
            }
            if (input.Length < numPixels * 3 || output.Length < numPixels * 3)
            {
                throw new ArgumentException("The image buffer is too small.");
            }

            for (int i = 0; i < numPixels; ++i)
            {
                byte r = input[i * 3];
                byte g = input[i * 3 + 1];
                byte b = input[i * 3 + 2];
                output[i * 3] = b;
                output[i * 3 + 1] = g;
                output[i * 3 + 2] = r;
            }
        }

        /**
          Converts an RGB buffer to the Win32 32-bit HDC BGRA format.
         */
        public static void RGBtoBGRA(byte[] input, byte[] output, int numPixels)
        {
            if (input == null || output == null)
            {
                throw new ArgumentNullException(input == null ? "input" : "output");
            }
            if (numPixels < 0)
            {
                throw new ArgumentOutOfRangeException("numPixels");
            }
            if (input.Length < numPixels * 3 || output.Length < numPixels * 4)
            {
                throw new ArgumentException("The image buffer is too small.");
            }

            byte[] source = ReferenceEquals(input, output) ? (byte[])input.Clone() : input;
            for (int i = 0; i < numPixels; ++i)
            {
                output[i * 4] = source[i * 3 + 2];
                output[i * 4 + 1] = source[i * 3 + 1];
                output[i * 4 + 2] = source[i * 3];
                output[i * 4 + 3] = 255;
            }
        }

        /**
          Converts an RGB buffer to the Win32 32-bit HDC ARGB format.
         */
        public static void RGBtoARGB(byte[] input, byte[] output, int numPixels)
        {
            if (input == null || output == null)
            {
                throw new ArgumentNullException(input == null ? "input" : "output");
            }
            if (numPixels < 0)
            {
                throw new ArgumentOutOfRangeException("numPixels");
            }
            if (input.Length < numPixels * 3 || output.Length < numPixels * 4)
            {
                throw new ArgumentException("The image buffer is too small.");
            }

            byte[] source = ReferenceEquals(input, output) ? (byte[])input.Clone() : input;
            for (int i = 0; i < numPixels; ++i)
            {
                output[i * 4] = 255;
                output[i * 4 + 1] = source[i * 3];
                output[i * 4 + 2] = source[i * 3 + 1];
                output[i * 4 + 3] = source[i * 3 + 2];
            }
        }

        /**
          Uses the red channel of the second image as an alpha channel.
         */
        public static void RGBxRGBtoRGBA(byte[] colorRGB, byte[] alphaRGB, byte[] output, int numPixels)
        {
            if (colorRGB == null || alphaRGB == null || output == null)
            {
                throw new ArgumentNullException(colorRGB == null ? "colorRGB" : (alphaRGB == null ? "alphaRGB" : "output"));
            }
            if (numPixels < 0)
            {
                throw new ArgumentOutOfRangeException("numPixels");
            }
            if (colorRGB.Length < numPixels * 3 || alphaRGB.Length < numPixels * 3 || output.Length < numPixels * 4)
            {
                throw new ArgumentException("The image buffer is too small.");
            }

            byte[] colors = ReferenceEquals(colorRGB, output) ? (byte[])colorRGB.Clone() : colorRGB;
            byte[] alphas = ReferenceEquals(alphaRGB, output) ? (byte[])alphaRGB.Clone() : alphaRGB;
            for (int i = 0; i < numPixels; ++i)
            {
                output[i * 4] = colors[i * 3];
                output[i * 4 + 1] = colors[i * 3 + 1];
                output[i * 4 + 2] = colors[i * 3 + 2];
                output[i * 4 + 3] = alphas[i * 3];
            }
        }

        /**
          Flips an RGB image along the vertical axis.
         */
        public static void flipRGBVertical(byte[] input, byte[] output, int width, int height)
        {
            flipVertical(input, output, width, height, 3);
        }

        /**
          Flips an RGBA image along the vertical axis.
         */
        public static void flipRGBAVertical(byte[] input, byte[] output, int width, int height)
        {
            flipVertical(input, output, width, height, 4);
        }

        private static void flipVertical(byte[] input, byte[] output, int width, int height, int bytesPerPixel)
        {
            if (input == null || output == null)
            {
                throw new ArgumentNullException(input == null ? "input" : "output");
            }
            if (width < 0 || height < 0)
            {
                throw new ArgumentOutOfRangeException(width < 0 ? "width" : "height");
            }
            int rowBytes = checked(width * bytesPerPixel);
            if (input.Length < rowBytes * height || output.Length < rowBytes * height)
            {
                throw new ArgumentException("The image buffer is too small.");
            }

            byte[] row = new byte[rowBytes];
            for (int i = 0; i < height / 2; ++i)
            {
                int top = i * rowBytes;
                int bottom = (height - i - 1) * rowBytes;
                Array.Copy(input, top, row, 0, rowBytes);
                Array.Copy(input, bottom, output, top, rowBytes);
                Array.Copy(row, 0, output, bottom, rowBytes);
            }
        }

        /**
          Converts RGB pixels to YUV.
         */
        public static void R8G8B8_to_Y8U8V8(int width, int height, byte[] input, byte[] output)
        {
            if (input == null || output == null)
            {
                throw new ArgumentNullException(input == null ? "input" : "output");
            }
            int count = checked(width * height);
            if (input.Length < count * 3 || output.Length < count * 3)
            {
                throw new ArgumentException("The image buffer is too small.");
            }

            for (int i = 0; i < count; ++i)
            {
                int r = input[i * 3];
                int g = input[i * 3 + 1];
                int b = input[i * 3 + 2];
                output[i * 3] = (byte)g3dmath.iClamp(g3dmath.iRound(r * 0.229 + g * 0.587 + b * 0.114), 0, 255);
                output[i * 3 + 1] = (byte)g3dmath.iClamp(g3dmath.iRound(r * -0.147 + g * -0.289 + b * 0.436) + 127, 0, 255);
                output[i * 3 + 2] = (byte)g3dmath.iClamp(g3dmath.iRound(r * 0.615 + g * -0.515 + b * -0.100) + 127, 0, 255);
            }
        }

        /**
          Converts YUV pixels to RGB.
         */
        public static void Y8U8V8_to_R8G8B8(int width, int height, byte[] input, byte[] output)
        {
            if (input == null || output == null)
            {
                throw new ArgumentNullException(input == null ? "input" : "output");
            }
            int count = checked(width * height);
            if (input.Length < count * 3 || output.Length < count * 3)
            {
                throw new ArgumentException("The image buffer is too small.");
            }

            for (int i = 0; i < count; ++i)
            {
                int y = input[i * 3];
                int u = input[i * 3 + 1] - 127;
                int v = input[i * 3 + 2] - 127;
                output[i * 3] = (byte)g3dmath.iClamp(g3dmath.iRound(y * 1.0753 + v * 1.2256), 0, 255);
                output[i * 3 + 1] = (byte)g3dmath.iClamp(g3dmath.iRound(y * 1.0753 + u * -0.3946 + v * -0.4947), 0, 255);
                output[i * 3 + 2] = (byte)g3dmath.iClamp(g3dmath.iRound(y * 1.0753 + u * 2.0320 + v * 0.0853), 0, 255);
            }
        }

        private static double normalHeight(byte[] bytes, int stride, int width, int height, int x, int y)
        {
            int xx = (x + width) % width;
            int yy = (y + height) % height;
            return bytes[(xx + yy * width) * stride] / 255.0;
        }

        /**
          Computes a tangent-space normal map from a bump map.
         */
        public static void computeNormalMap(GImage bump, GImage normal)
        {
            computeNormalMap(bump, normal, false, false);
        }

        /**
          Computes a tangent-space normal map from a bump map.
         */
        public static void computeNormalMap(GImage bump, GImage normal, bool lowPassBump, bool scaleHeightByNz)
        {
            if (bump == null || normal == null)
            {
                throw new ArgumentNullException(bump == null ? "bump" : "normal");
            }
            if (bump.width <= 0 || bump.height <= 0)
            {
                throw new ArgumentException("The bump map must not be empty.", "bump");
            }

            byte[] source = bump.getBytes();
            if (source == null)
            {
                throw new InvalidOperationException("The bump map has no pixel data.");
            }
            if (ReferenceEquals(bump, normal))
            {
                source = (byte[])source.Clone();
            }

            int w = bump.width;
            int h = bump.height;
            int stride = bump.channels;
            normal.resize(w, h, 4);
            byte[] destination = normal.getBytes();

            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    int i = x + y * w;
                    double dx = -(normalHeight(source, stride, w, h, x - 1, y - 1) * -1 +
                                  normalHeight(source, stride, w, h, x + 1, y - 1) * 1 +
                                  normalHeight(source, stride, w, h, x - 1, y) * -2 +
                                  normalHeight(source, stride, w, h, x + 1, y) * 2 +
                                  normalHeight(source, stride, w, h, x - 1, y + 1) * -1 +
                                  normalHeight(source, stride, w, h, x + 1, y + 1) * 1);
                    double dy = -(normalHeight(source, stride, w, h, x - 1, y - 1) * 1 +
                                  normalHeight(source, stride, w, h, x, y - 1) * 2 +
                                  normalHeight(source, stride, w, h, x + 1, y - 1) * 1 +
                                  normalHeight(source, stride, w, h, x - 1, y + 1) * -1 +
                                  normalHeight(source, stride, w, h, x, y + 1) * -2 +
                                  normalHeight(source, stride, w, h, x + 1, y + 1) * -1);
                    Vector3 delta = new Vector3((float)dx, (float)dy, 1.0f).direction();
                    double heightValue = source[i * stride] / 255.0;

                    if (lowPassBump)
                    {
                        heightValue = (normalHeight(source, stride, w, h, x - 1, y - 1) +
                                       normalHeight(source, stride, w, h, x, y - 1) +
                                       normalHeight(source, stride, w, h, x + 1, y - 1) +
                                       normalHeight(source, stride, w, h, x - 1, y) +
                                       normalHeight(source, stride, w, h, x, y) +
                                       normalHeight(source, stride, w, h, x + 1, y) +
                                       normalHeight(source, stride, w, h, x - 1, y + 1) +
                                       normalHeight(source, stride, w, h, x, y + 1) +
                                       normalHeight(source, stride, w, h, x + 1, y + 1)) / 9.0;
                    }
                    if (scaleHeightByNz)
                    {
                        heightValue *= delta.z;
                    }

                    destination[i * 4] = (byte)g3dmath.iClamp(g3dmath.iRound(delta.x * 127.5 + 127.5), 0, 255);
                    destination[i * 4 + 1] = (byte)g3dmath.iClamp(g3dmath.iRound(delta.y * 127.5 + 127.5), 0, 255);
                    destination[i * 4 + 2] = (byte)g3dmath.iClamp(g3dmath.iRound(delta.z * 127.5 + 127.5), 0, 255);
                    destination[i * 4 + 3] = (byte)g3dmath.iClamp(g3dmath.iRound(heightValue * 255.0), 0, 255);
                }
            }
        }

        /**
          Overwrites every pixel with one of two checkerboard colors.
         */
        public static void makeCheckerboard(GImage im)
        {
            makeCheckerboard(im, 1, new Color4uint8(255, 255, 255, 255), new Color4uint8(0, 0, 0, 255));
        }

        /**
          Overwrites every pixel with one of two checkerboard colors.
         */
        public static void makeCheckerboard(GImage im, int checkerSize)
        {
            makeCheckerboard(im, checkerSize, new Color4uint8(255, 255, 255, 255), new Color4uint8(0, 0, 0, 255));
        }

        /**
          Overwrites every pixel with one of two checkerboard colors.
         */
        public static void makeCheckerboard(GImage im, int checkerSize, Color4uint8 color1, Color4uint8 color2)
        {
            if (im == null)
            {
                throw new ArgumentNullException("im");
            }
            if (checkerSize <= 0)
            {
                throw new ArgumentOutOfRangeException("checkerSize");
            }
            if (im.channels < 1 || im.channels > 4)
            {
                throw new InvalidOperationException("The image has an invalid number of channels.");
            }

            for (int y = 0; y < im.height; ++y)
            {
                for (int x = 0; x < im.width; ++x)
                {
                    bool checker = g3dmath.isOdd((x / checkerSize) + (y / checkerSize));
                    Color4uint8 color = checker ? color1 : color2;
                    int offset = (x + y * im.width) * im.channels;
                    for (int c = 0; c < im.channels; ++c)
                    {
                        im._byte[offset + c] = color[c];
                    }
                }
            }
        }

        /**
          Bayer demosaicing using the Malvar-He-Cutler filter.
         */
        public static void BAYER_G8B8_R8G8_to_R8G8B8_MHC(int w, int h, byte[] input, byte[] output)
        {
            BayerMHC.bayerG8B8R8G8(w, h, input, output);
        }

        /**
          Bayer demosaicing using the Malvar-He-Cutler filter.
         */
        public static void BAYER_G8R8_B8G8_to_R8G8B8_MHC(int w, int h, byte[] input, byte[] output)
        {
            BayerMHC.bayerG8R8B8G8(w, h, input, output);
        }

        /**
          Bayer demosaicing using the Malvar-He-Cutler filter.
         */
        public static void BAYER_R8G8_G8B8_to_R8G8B8_MHC(int w, int h, byte[] input, byte[] output)
        {
            BayerMHC.bayerR8G8G8B8(w, h, input, output);
        }

        /**
          Bayer demosaicing using the Malvar-He-Cutler filter.
         */
        public static void BAYER_B8G8_G8R8_to_R8G8B8_MHC(int w, int h, byte[] input, byte[] output)
        {
            BayerMHC.bayerB8G8G8R8(w, h, input, output);
        }

        /**
          Fast conversion to quarter-size RGB.
         */
        public static void BAYER_G8B8_R8G8_to_Quarter_R8G8B8(int inWidth, int inHeight, byte[] input, byte[] output)
        {
            BayerMHC.bayerToQuarter(inWidth, inHeight, input, output);
        }

        /**
          Undoes the fast quarter-size Bayer conversion.
         */
        public static void Quarter_R8G8B8_to_BAYER_G8B8_R8G8(int inWidth, int inHeight, byte[] input, byte[] output)
        {
            BayerMHC.quarterToBayer(inWidth, inHeight, input, output);
        }
    }

    /**
      Decodes an image file into a GImage.
     */
    public interface GImageDecoder
    {
        bool canDecode(string filename);
        bool canDecode(byte[] data, int length);
        GImage decode(string filename);
        GImage decode(byte[] data, int length);
    }

    /**
      Decodes one explicit image format.
     */
    public interface GImageFormatDecoder
    {
        GImage.Format format { get; }
        GImage decode(string filename);
        GImage decode(byte[] data, int length);
    }

    /**
      Encodes one explicit image format.
     */
    public interface GImageEncoder
    {
        GImage.Format format { get; }
        byte[] encode(GImage image);
    }

    /**
      A format-specific decoder and encoder.
     */
    public interface GImageCodec : GImageFormatDecoder, GImageEncoder
    {
    }
}

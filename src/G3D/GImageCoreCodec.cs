using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace G3D
{
    internal static class GImageCoreCodec
    {
        private const int MAX_DIMENSION = 100000;

        internal static byte[] encodeBMP(GImage image)
        {
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }
            if (image.channels != 1 && image.channels != 3)
            {
                throw new GImage.Error("BMP encoding requires one or three channels.");
            }
            if (image.width < 0 || image.height < 0)
            {
                throw new GImage.Error("BMP encoding requires non-negative dimensions.");
            }

            byte[] source = image.getBytes();
            if (source == null)
            {
                throw new GImage.Error("The image has no pixel data.");
            }

            int width = image.width;
            int height = image.height;
            int rowBytes;
            int pixelBytes;
            try
            {
                int rawRowBytes = checked(width * 3);
                rowBytes = checked((rawRowBytes + 3) / 4 * 4);
                pixelBytes = checked(rowBytes * height);
            }
            catch (OverflowException ex)
            {
                throw new GImage.Error("The BMP dimensions are too large.", ex.Message);
            }

            List<byte> output = new List<byte>(checked(54 + pixelBytes));
            output.Add((byte)'B');
            output.Add((byte)'M');
            addU32(output, checked(54 + pixelBytes));
            addU16(output, 0);
            addU16(output, 0);
            addU32(output, 54);

            addU32(output, 40);
            addU32(output, width);
            addU32(output, height);
            addU16(output, 1);
            addU16(output, 24);
            addU32(output, 0);
            addU32(output, pixelBytes);
            addU32(output, 0);
            addU32(output, 0);
            addU32(output, 0);
            addU32(output, 0);

            for (int y = height - 1; y >= 0; --y)
            {
                for (int x = 0; x < width; ++x)
                {
                    int sourceIndex = (x + y * width) * image.channels;
                    byte r = source[sourceIndex];
                    byte g = image.channels == 1 ? r : source[sourceIndex + 1];
                    byte b = image.channels == 1 ? r : source[sourceIndex + 2];
                    output.Add(b);
                    output.Add(g);
                    output.Add(r);
                }
                int written = width * 3;
                for (int i = written; i < rowBytes; ++i)
                {
                    output.Add(0);
                }
            }

            return output.ToArray();
        }

        internal static GImage decodeBMP(byte[] data, int length, string filename)
        {
            require(data, length, 14, filename, "BMP");
            if (data[0] != (byte)'B' || data[1] != (byte)'M')
            {
                throw new GImage.Error("Not a BMP file", filename);
            }

            uint pixelOffset = readU32(data, 10);
            uint dibSize = readU32(data, 14);
            int width;
            int height;
            int bitCount;
            uint compression = 0;
            uint colorsUsed = 0;
            int paletteOffset;

            if (dibSize == 12)
            {
                require(data, length, 26, filename, "BMP");
                width = readU16(data, 18);
                height = readU16(data, 20);
                bitCount = readU16(data, 24);
                paletteOffset = 26;
            }
            else
            {
                require(data, length, checked(14 + (int)Math.Min(dibSize, 40u)), filename, "BMP");
                if (dibSize < 40 || dibSize > (uint)(length - 14))
                {
                    throw new GImage.Error("The BMP information header is invalid.", filename);
                }
                width = readI32(data, 18);
                height = readI32(data, 22);
                bitCount = readU16(data, 28);
                compression = readU32(data, 30);
                if (dibSize >= 40)
                {
                    colorsUsed = readU32(data, 46);
                }
                paletteOffset = checked(14 + (int)dibSize);
            }

            if (compression != 0)
            {
                throw new GImage.Error("BMP images must be uncompressed", filename);
            }
            if (width <= 0 || width > MAX_DIMENSION)
            {
                throw new GImage.Error("The BMP width is invalid.", filename);
            }
            if (height == int.MinValue)
            {
                throw new GImage.Error("The BMP height is invalid.", filename);
            }
            int actualHeight = height < 0 ? -height : height;
            if (actualHeight <= 0 || actualHeight > MAX_DIMENSION)
            {
                throw new GImage.Error("The BMP height is invalid.", filename);
            }
            if (bitCount != 1 && bitCount != 4 && bitCount != 8 && bitCount != 24)
            {
                if (bitCount == 16)
                {
                    throw new GImage.Error("16-bit bitmaps not supported", filename);
                }
                if (bitCount == 32)
                {
                    throw new GImage.Error("32 bit bitmaps not supported", filename);
                }
                throw new GImage.Error("Not a bitmap!", filename);
            }

            int paletteCount = 0;
            int paletteEntrySize = dibSize == 12 ? 3 : 4;
            if (bitCount <= 8)
            {
                paletteCount = colorsUsed == 0 ? 1 << bitCount : (int)colorsUsed;
                if (paletteCount <= 0 || paletteCount > 256)
                {
                    throw new GImage.Error("The BMP palette is invalid.", filename);
                }
            }

            int paletteBytes = checked(paletteCount * paletteEntrySize);
            if (paletteOffset < 0 || paletteOffset > length || paletteBytes > length - paletteOffset)
            {
                throw new GImage.Error("The BMP palette is truncated.", filename);
            }
            byte[] palette = new byte[paletteCount * 3];
            for (int i = 0; i < paletteCount; ++i)
            {
                int source = paletteOffset + i * paletteEntrySize;
                byte b = data[source];
                byte g = data[source + 1];
                byte r = data[source + 2];
                palette[i * 3] = r;
                palette[i * 3 + 1] = g;
                palette[i * 3 + 2] = b;
            }

            int rowBytes;
            switch (bitCount)
            {
                case 1:
                    rowBytes = (width + 7) / 8;
                    break;
                case 4:
                    rowBytes = (width + 1) / 2;
                    break;
                case 8:
                    rowBytes = width;
                    break;
                default:
                    rowBytes = checked(width * 3);
                    break;
            }
            rowBytes = checked((rowBytes + 3) / 4 * 4);

            if (pixelOffset > int.MaxValue)
            {
                throw new GImage.Error("The BMP pixel offset is invalid.", filename);
            }
            int dataOffset = pixelOffset == 0 ? checked(paletteOffset + paletteBytes) : (int)pixelOffset;
            if (dataOffset < 0 || dataOffset > length)
            {
                throw new GImage.Error("The BMP pixel offset is invalid.", filename);
            }
            int pixelBytes = checked(rowBytes * actualHeight);
            if (pixelBytes > length - dataOffset)
            {
                throw new GImage.Error("The BMP pixel data is truncated.", filename);
            }

            byte[] pixels = new byte[checked(width * actualHeight * 3)];
            bool topDown = height < 0;
            for (int row = 0; row < actualHeight; ++row)
            {
                int sourceRow = topDown ? row : actualHeight - row - 1;
                int rowOffset = checked(dataOffset + sourceRow * rowBytes);
                int destinationRow = row;
                for (int x = 0; x < width; ++x)
                {
                    byte r;
                    byte g;
                    byte b;
                    if (bitCount == 24)
                    {
                        int source = checked(rowOffset + x * 3);
                        b = data[source];
                        g = data[source + 1];
                        r = data[source + 2];
                    }
                    else
                    {
                        int index;
                        if (bitCount == 1)
                        {
                            int source = checked(rowOffset + x / 8);
                            index = (data[source] >> (7 - (x & 7))) & 1;
                        }
                        else if (bitCount == 4)
                        {
                            int source = checked(rowOffset + x / 2);
                            index = (x & 1) == 0 ? (data[source] >> 4) : (data[source] & 0x0F);
                        }
                        else
                        {
                            index = data[checked(rowOffset + x)];
                        }
                        if (index >= paletteCount)
                        {
                            throw new GImage.Error("The BMP palette index is invalid.", filename);
                        }
                        r = palette[index * 3];
                        g = palette[index * 3 + 1];
                        b = palette[index * 3 + 2];
                    }
                    int destination = (x + destinationRow * width) * 3;
                    pixels[destination] = r;
                    pixels[destination + 1] = g;
                    pixels[destination + 2] = b;
                }
            }

            return GImage.fromMemory(pixels, width, actualHeight, 3);
        }

        internal static byte[] encodeTGA(GImage image)
        {
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }
            if (image.channels != 3 && image.channels != 4)
            {
                throw new GImage.Error("TGA encoding requires three or four channels.");
            }
            if (image.width < 0 || image.height < 0 || image.width > 65535 || image.height > 65535)
            {
                throw new GImage.Error("The TGA dimensions are invalid.");
            }

            byte[] source = image.getBytes();
            if (source == null)
            {
                throw new GImage.Error("The image has no pixel data.");
            }

            int pixelBytes = checked(image.width * image.height * image.channels);
            List<byte> output = new List<byte>(checked(18 + pixelBytes + 18));
            output.Add(0);
            output.Add(0);
            output.Add(2);
            for (int i = 0; i < 5; ++i)
            {
                output.Add(0);
            }
            addU16(output, 0);
            addU16(output, 0);
            addU16(output, image.width);
            addU16(output, image.height);
            output.Add((byte)(8 * image.channels));
            output.Add((byte)(image.channels == 4 ? 8 : 0));

            for (int y = image.height - 1; y >= 0; --y)
            {
                for (int x = 0; x < image.width; ++x)
                {
                    int sourceIndex = (x + y * image.width) * image.channels;
                    output.Add(source[sourceIndex + 2]);
                    output.Add(source[sourceIndex + 1]);
                    output.Add(source[sourceIndex]);
                    if (image.channels == 4)
                    {
                        output.Add(source[sourceIndex + 3]);
                    }
                }
            }

            byte[] footer = Encoding.ASCII.GetBytes("TRUEVISION-XFILE ");
            output.AddRange(footer);
            output.Add(0);
            return output.ToArray();
        }

        internal static GImage decodeTGA(byte[] data, int length, string filename)
        {
            require(data, length, 36, filename, "TGA");
            if (length < 18 || data[length - 18] != (byte)'T')
            {
                throw new GImage.Error("Not a TGA file", filename);
            }
            string footer = Encoding.ASCII.GetString(data, length - 18, 16);
            if (footer != "TRUEVISION-XFILE")
            {
                throw new GImage.Error("Not a TGA file", filename);
            }

            int idLength = data[0];
            int imageType = data[2];
            if (imageType != 2)
            {
                throw new GImage.Error("TGA images must be type 2 (Uncompressed truecolor)", filename);
            }

            int width = readU16(data, 12);
            int height = readU16(data, 14);
            int colorDepth = data[16];
            int descriptor = data[17];
            if (width <= 0 || height <= 0 || width > MAX_DIMENSION || height > MAX_DIMENSION)
            {
                throw new GImage.Error("The TGA dimensions are invalid.", filename);
            }
            if (colorDepth != 24 && colorDepth != 32)
            {
                throw new GImage.Error("TGA files must be 24 or 32 bit.", filename);
            }

            int channels = colorDepth == 32 ? 4 : 3;
            int dataOffset = checked(18 + idLength);
            int pixelBytes = checked(width * height * channels);
            if (dataOffset > length || pixelBytes > length - dataOffset)
            {
                throw new GImage.Error("The TGA pixel data is truncated.", filename);
            }

            byte[] pixels = new byte[pixelBytes];
            bool topDown = (descriptor & 0x20) != 0;
            int sourceOffset = dataOffset;
            for (int row = 0; row < height; ++row)
            {
                int destinationRow = topDown ? row : height - row - 1;
                for (int x = 0; x < width; ++x)
                {
                    int destination = (x + destinationRow * width) * channels;
                    pixels[destination] = data[sourceOffset + 2];
                    pixels[destination + 1] = data[sourceOffset + 1];
                    pixels[destination + 2] = data[sourceOffset];
                    if (channels == 4)
                    {
                        pixels[destination + 3] = data[sourceOffset + 3];
                    }
                    sourceOffset += channels;
                }
            }
            return GImage.fromMemory(pixels, width, height, channels);
        }

        internal static byte[] encodePPM(GImage image)
        {
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }
            if (image.channels != 3)
            {
                throw new GImage.Error("PPM encoding requires three channels.");
            }
            byte[] source = image.getBytes();
            if (source == null)
            {
                throw new GImage.Error("The image has no pixel data.");
            }
            string header = Format.format("P6 %d %d 255 ", image.width, image.height);
            byte[] headerBytes = Encoding.ASCII.GetBytes(header);
            byte[] pixels = new byte[checked(image.width * image.height * 3)];
            Array.Copy(source, pixels, pixels.Length);
            byte[] result = new byte[checked(headerBytes.Length + pixels.Length)];
            Array.Copy(headerBytes, result, headerBytes.Length);
            Array.Copy(pixels, 0, result, headerBytes.Length, pixels.Length);
            return result;
        }

        internal static byte[] encodePPMASCII(GImage image)
        {
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }
            if (image.channels != 3)
            {
                throw new GImage.Error("ASCII PPM encoding requires three channels.");
            }
            byte[] source = image.getBytes();
            if (source == null)
            {
                throw new GImage.Error("The image has no pixel data.");
            }

            StringBuilder output = new StringBuilder();
            output.Append(Format.format("P3\n%d %d\n255\n", image.width, image.height));
            int count = checked(image.width * image.height);
            for (int i = 0; i < count; ++i)
            {
                output.Append(source[i * 3].ToString(CultureInfo.InvariantCulture));
                output.Append(' ');
                output.Append(source[i * 3 + 1].ToString(CultureInfo.InvariantCulture));
                output.Append(' ');
                output.Append(source[i * 3 + 2].ToString(CultureInfo.InvariantCulture));
                output.Append((i + 1 == count) ? '\n' : ' ');
            }
            return Encoding.ASCII.GetBytes(output.ToString());
        }

        internal static GImage decodePPM(byte[] data, int length, string filename)
        {
            int position = 0;
            string magic = readToken(data, ref position, length, filename, "PPM");
            if (magic != "P6")
            {
                throw new GImage.Error("Invalid PPM Header.", filename);
            }
            int width = readIntToken(data, ref position, length, filename, "PPM");
            int height = readIntToken(data, ref position, length, filename, "PPM");
            int maxColor = readIntToken(data, ref position, length, filename, "PPM");
            validatePPMSize(width, height, filename);
            if (maxColor < 1 || maxColor > 255)
            {
                throw new GImage.Error("Only 8-bit PPM data is supported.", filename);
            }
            if (position >= length)
            {
                throw new GImage.Error("The PPM header is truncated.", filename);
            }
            byte separator = data[position++];
            if (separator == (byte)'\r' && position < length && data[position] == (byte)'\n')
            {
                ++position;
            }

            int pixelBytes = checked(width * height * 3);
            if (pixelBytes > length - position)
            {
                throw new GImage.Error("The PPM pixel data is truncated.", filename);
            }
            byte[] pixels = new byte[pixelBytes];
            Array.Copy(data, position, pixels, 0, pixelBytes);
            return GImage.fromMemory(pixels, width, height, 3);
        }

        internal static GImage decodePPMASCII(byte[] data, int length, string filename)
        {
            int position = 0;
            string magic = readToken(data, ref position, length, filename, "PPM");
            if (magic != "P1" && magic != "P2" && magic != "P3")
            {
                throw new GImage.Error("Invalid PPM Header.", filename);
            }
            int width = readIntToken(data, ref position, length, filename, "PPM");
            int height = readIntToken(data, ref position, length, filename, "PPM");
            validatePPMSize(width, height, filename);
            int maxColor = magic == "P1" ? 1 : readIntToken(data, ref position, length, filename, "PPM");
            if (maxColor < 1)
            {
                throw new GImage.Error("Invalid PPM Header.", filename);
            }

            int valuesPerPixel = magic == "P3" ? 3 : 1;
            int valueCount = checked(width * height * valuesPerPixel);
            if (valueCount > length - position)
            {
                throw new GImage.Error("The PPM pixel data is truncated.", filename);
            }
            int[] values = new int[valueCount];
            for (int i = 0; i < valueCount; ++i)
            {
                values[i] = readIntToken(data, ref position, length, filename, "PPM");
                if (values[i] < 0)
                {
                    throw new GImage.Error("Invalid PPM pixel value.", filename);
                }
            }

            double scale = magic == "P1" ? 255.0 : (maxColor <= 255 ? 1.0 : 255.0 / maxColor);
            byte[] pixels = new byte[checked(width * height * 3)];
            int valueIndex = 0;
            for (int i = 0; i < width * height; ++i)
            {
                byte r;
                byte g;
                byte b;
                if (magic == "P3")
                {
                    r = clampByte(values[valueIndex++] * scale);
                    g = clampByte(values[valueIndex++] * scale);
                    b = clampByte(values[valueIndex++] * scale);
                }
                else
                {
                    r = clampByte(values[valueIndex++] * scale);
                    g = r;
                    b = r;
                }
                pixels[i * 3] = r;
                pixels[i * 3 + 1] = g;
                pixels[i * 3 + 2] = b;
            }
            return GImage.fromMemory(pixels, width, height, 3);
        }

        internal static GImage decodePCX(byte[] data, int length, string filename)
        {
            require(data, length, 128, filename, "PCX");
            if (data[0] != 0x0A || data[2] != 0x01)
            {
                throw new GImage.Error("PCX file is corrupted", filename);
            }
            int bitsPerPixel = data[3];
            int planes = data[65];
            if (bitsPerPixel != 8 || (planes != 1 && planes != 3))
            {
                throw new GImage.Error("Only 8-bit paletted and 24-bit PCX files supported.", filename);
            }
            int xmin = readU16(data, 4);
            int ymin = readU16(data, 6);
            int xmax = readU16(data, 8);
            int ymax = readU16(data, 10);
            int width = xmax - xmin + 1;
            int height = ymax - ymin + 1;
            validatePPMSize(width, height, filename);
            byte[] pixels = new byte[checked(width * height * 3)];

            if (planes == 3)
            {
                int source = 128;
                int p = 0;
                for (int row = 0; row < height; ++row)
                {
                    for (int plane = 0; plane < planes; ++plane)
                    {
                        int rowEnd = p + width;
                        while (p < rowEnd)
                        {
                            if (source >= length)
                            {
                                throw new GImage.Error("The PCX pixel data is truncated.", filename);
                            }
                            int value = data[source++];
                            int runLength = 1;
                            if (value >= 192)
                            {
                                runLength = value & 0x3F;
                                if (source >= length)
                                {
                                    throw new GImage.Error("The PCX pixel data is truncated.", filename);
                                }
                                value = data[source++];
                            }
                            if (runLength == 0)
                            {
                                runLength = 1;
                            }
                            if (runLength > rowEnd - p)
                            {
                                throw new GImage.Error("The PCX pixel data contains an invalid run.", filename);
                            }
                            for (int i = 0; i < runLength; ++i, ++p)
                            {
                                pixels[p * 3 + plane] = (byte)value;
                            }
                        }
                    }
                }
            }
            else
            {
                int paletteStart = length - 769;
                if (paletteStart < 128)
                {
                    throw new GImage.Error("The PCX palette is truncated.", filename);
                }
                if (data[paletteStart] != 12)
                {
                    throw new GImage.Error("Corrupted PCX file (palette marker byte was missing)", filename);
                }
                byte[] palette = new byte[768];
                Array.Copy(data, paletteStart + 1, palette, 0, palette.Length);
                int source = 128;
                int p = 0;
                while (p < width * height)
                {
                    if (source >= length)
                    {
                        throw new GImage.Error("The PCX pixel data is truncated.", filename);
                    }
                    int index = data[source++];
                    int runLength = 1;
                    if (index >= 192)
                    {
                        runLength = index & 0x3F;
                        if (source >= length)
                        {
                            throw new GImage.Error("The PCX pixel data is truncated.", filename);
                        }
                        index = data[source++];
                    }
                    if (runLength == 0)
                    {
                        runLength = 1;
                    }
                    if (index >= 256)
                    {
                        throw new GImage.Error("The PCX palette index is invalid.", filename);
                    }
                    if (runLength > width * height - p)
                    {
                        throw new GImage.Error("The PCX pixel data contains an invalid run.", filename);
                    }
                    for (int i = 0; i < runLength; ++i, ++p)
                    {
                        pixels[p * 3] = palette[index * 3];
                        pixels[p * 3 + 1] = palette[index * 3 + 1];
                        pixels[p * 3 + 2] = palette[index * 3 + 2];
                    }
                }
            }
            return GImage.fromMemory(pixels, width, height, 3);
        }

        internal static GImage decodeICO(byte[] data, int length, string filename)
        {
            require(data, length, 6, filename, "ICO");
            if (data[0] != 0 || data[1] != 0 || data[2] != 0 || data[3] != 0 ||
                data[4] != 1 || data[5] != 0)
            {
                throw new GImage.Error("Invalid ICO header.", filename);
            }
            int count = readU16(data, 6);
            if (count <= 0)
            {
                throw new GImage.Error("The ICO file contains no images.", filename);
            }
            require(data, length, checked(6 + count * 16), filename, "ICO");
            int selected = 0;
            int selectedArea = -1;
            for (int i = 0; i < count; ++i)
            {
                int entry = 6 + i * 16;
                int w = data[entry] == 0 ? 256 : data[entry];
                int h = data[entry + 1] == 0 ? 256 : data[entry + 1];
                int area = w * h;
                if (area > selectedArea)
                {
                    selectedArea = area;
                    selected = i;
                }
            }
            int selectedEntry = 6 + selected * 16;
            int directoryWidth = data[selectedEntry] == 0 ? 256 : data[selectedEntry];
            int directoryHeight = data[selectedEntry + 1] == 0 ? 256 : data[selectedEntry + 1];
            uint rawImageOffset = readU32(data, selectedEntry + 12);
            if (rawImageOffset > int.MaxValue)
            {
                throw new GImage.Error("The ICO image offset is invalid.", filename);
            }
            int imageOffset = (int)rawImageOffset;
            if (imageOffset < 0 || imageOffset > length - 4)
            {
                throw new GImage.Error("The ICO image offset is invalid.", filename);
            }
            if (data[imageOffset] == 0x89 && data[imageOffset + 1] == 0x50 &&
                data[imageOffset + 2] == 0x4E && data[imageOffset + 3] == 0x47)
            {
                throw new GImage.Error("PNG-compressed ICO files require a registered PNG decoder.", filename);
            }
            uint headerSize = readU32(data, imageOffset);
            if (headerSize < 40 || imageOffset > length - 40)
            {
                throw new GImage.Error("The ICO image header is invalid.", filename);
            }
            int width = directoryWidth;
            int height = directoryHeight;
            int bitCount = readU16(data, imageOffset + 14);
            uint compression = readU32(data, imageOffset + 16);
            if (compression != 0)
            {
                throw new GImage.Error("Compressed ICO images are not supported.", filename);
            }
            if (bitCount != 1 && bitCount != 4 && bitCount != 8 && bitCount != 24 && bitCount != 32)
            {
                throw new GImage.Error("Unsupported ICO color count.", filename);
            }
            int paletteCount = bitCount <= 8 ? (1 << bitCount) : 0;
            int paletteOffset = checked(imageOffset + 40);
            int paletteBytes = checked(paletteCount * 4);
            if (paletteBytes > length - paletteOffset)
            {
                throw new GImage.Error("The ICO palette is truncated.", filename);
            }
            byte[] palette = new byte[paletteCount * 4];
            for (int i = 0; i < paletteCount; ++i)
            {
                int source = paletteOffset + i * 4;
                palette[i * 4] = data[source + 2];
                palette[i * 4 + 1] = data[source + 1];
                palette[i * 4 + 2] = data[source];
                palette[i * 4 + 3] = data[source + 3];
            }

            int rowBytes = bitCount <= 8 ? ((width * bitCount + 31) / 32) * 4 : width * 4;
            int pixelOffset = checked(paletteOffset + paletteBytes);
            int xorBytes = checked(rowBytes * height);
            int maskRowBytes = ((width + 31) / 32) * 4;
            int maskOffset = checked(pixelOffset + xorBytes);
            int maskBytes = checked(maskRowBytes * height);
            if (xorBytes > length - pixelOffset || maskBytes > length - maskOffset)
            {
                throw new GImage.Error("The ICO pixel data is truncated.", filename);
            }

            byte[] pixels = new byte[checked(width * height * 4)];
            for (int row = 0; row < height; ++row)
            {
                int sourceRow = height - row - 1;
                int source = checked(pixelOffset + sourceRow * rowBytes);
                for (int x = 0; x < width; ++x)
                {
                    byte r;
                    byte g;
                    byte b;
                    byte a = 255;
                    if (bitCount <= 8)
                    {
                        int index;
                        if (bitCount == 1)
                        {
                            index = (data[source + x / 8] >> (7 - (x & 7))) & 1;
                        }
                        else if (bitCount == 4)
                        {
                            int value = data[source + x / 2];
                            index = (x & 1) == 0 ? (value >> 4) : (value & 0x0F);
                        }
                        else
                        {
                            index = data[source + x];
                        }
                        if (index >= paletteCount)
                        {
                            throw new GImage.Error("The ICO palette index is invalid.", filename);
                        }
                        r = palette[index * 4];
                        g = palette[index * 4 + 1];
                        b = palette[index * 4 + 2];
                        a = palette[index * 4 + 3];
                    }
                    else if (bitCount == 24)
                    {
                        int pixel = source + x * 3;
                        b = data[pixel];
                        g = data[pixel + 1];
                        r = data[pixel + 2];
                    }
                    else
                    {
                        int pixel = source + x * 4;
                        b = data[pixel];
                        g = data[pixel + 1];
                        r = data[pixel + 2];
                        a = data[pixel + 3];
                    }
                    int destination = (x + row * width) * 4;
                    pixels[destination] = r;
                    pixels[destination + 1] = g;
                    pixels[destination + 2] = b;
                    pixels[destination + 3] = a;
                }
            }

            for (int row = 0; row < height; ++row)
            {
                int sourceRow = height - row - 1;
                int source = checked(maskOffset + sourceRow * maskRowBytes);
                for (int x = 0; x < width; ++x)
                {
                    int bit = (data[source + x / 8] >> (7 - (x & 7))) & 1;
                    pixels[(x + row * width) * 4 + 3] = bit == 0 ? (byte)255 : (byte)0;
                }
            }
            return GImage.fromMemory(pixels, width, height, 4);
        }

        private static void validatePPMSize(int width, int height, string filename)
        {
            if (width <= 0 || height <= 0 || width > MAX_DIMENSION || height > MAX_DIMENSION)
            {
                throw new GImage.Error("Invalid PPM size in header.", filename);
            }
        }

        private static byte clampByte(double value)
        {
            int rounded = g3dmath.iRound(value);
            return (byte)g3dmath.iClamp(rounded, 0, 255);
        }

        private static string readToken(byte[] data, ref int position, int length, string filename, string format)
        {
            while (position < length)
            {
                if (char.IsWhiteSpace((char)data[position]))
                {
                    ++position;
                    continue;
                }
                if (data[position] == (byte)'#')
                {
                    while (position < length && data[position] != (byte)'\n' && data[position] != (byte)'\r')
                    {
                        ++position;
                    }
                    continue;
                }
                break;
            }
            if (position >= length)
            {
                throw new GImage.Error("The " + format + " header is truncated.", filename);
            }
            int start = position;
            while (position < length && !char.IsWhiteSpace((char)data[position]) && data[position] != (byte)'#')
            {
                ++position;
            }
            return Encoding.ASCII.GetString(data, start, position - start);
        }

        private static int readIntToken(byte[] data, ref int position, int length, string filename, string format)
        {
            string token = readToken(data, ref position, length, filename, format);
            int result;
            if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
            {
                throw new GImage.Error("The " + format + " header contains an invalid number.", filename);
            }
            return result;
        }

        private static void require(byte[] data, int length, int required, string filename, string format)
        {
            if (data == null || length < required)
            {
                throw new GImage.Error("The " + format + " file is truncated.", filename);
            }
        }

        private static int readU16(byte[] data, int offset)
        {
            return data[offset] | (data[offset + 1] << 8);
        }

        private static uint readU32(byte[] data, int offset)
        {
            return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
        }

        private static int readI32(byte[] data, int offset)
        {
            return unchecked((int)readU32(data, offset));
        }

        private static void addU16(List<byte> output, int value)
        {
            output.Add((byte)(value & 0xFF));
            output.Add((byte)((value >> 8) & 0xFF));
        }

        private static void addU32(List<byte> output, int value)
        {
            output.Add((byte)(value & 0xFF));
            output.Add((byte)((value >> 8) & 0xFF));
            output.Add((byte)((value >> 16) & 0xFF));
            output.Add((byte)((value >> 24) & 0xFF));
        }
    }
}

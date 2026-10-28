using System;
using System.IO;
using System.Text;
using G3D;
using G3D.Image.SystemDrawing;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private static void testGImage()
        {
            SystemDrawingImageDecoder.register();
            testGImageCoreCodecs();
            testGImageDesktopCodecs();
            testGImagePublicAPI();
        }

        private static void testGImageCoreCodecs()
        {
            byte[] rgbBytes = new byte[]
            {
                255, 0, 0,
                0, 255, 0,
                0, 0, 255,
                255, 255, 255
            };
            GImage rgb = GImage.fromMemory(rgbBytes, 2, 2, 3);

            byte[] bmp = rgb.encode(GImage.Format.BMP);
            Check(GImage.resolveFormat(bmp, bmp.Length) == GImage.Format.BMP, "BMP format detection");
            GImage decodedBMP = new GImage(bmp, bmp.Length, GImage.Format.BMP);
            CheckEqual(2, decodedBMP.width, "BMP width");
            CheckEqual(2, decodedBMP.height, "BMP height");
            CheckEqual(3, decodedBMP.channels, "BMP channels");
            checkImageBytes(decodedBMP, rgbBytes, "BMP pixels");

            byte[] tga = rgb.encode(GImage.Format.TGA);
            Check(GImage.resolveFormat(tga, tga.Length) == GImage.Format.TGA, "TGA format detection");
            GImage decodedTGA = new GImage(tga, tga.Length, GImage.Format.TGA);
            CheckEqual(3, decodedTGA.channels, "TGA channels");
            checkImageBytes(decodedTGA, rgbBytes, "TGA pixels");

            byte[] rgbaBytes = new byte[]
            {
                255, 0, 0, 17,
                0, 255, 0, 68,
                0, 0, 255, 129,
                255, 255, 255, 255
            };
            GImage rgba = GImage.fromMemory(rgbaBytes, 2, 2, 4);
            byte[] tga32 = rgba.encode(GImage.Format.TGA);
            GImage decodedTGA32 = new GImage(tga32, tga32.Length, GImage.Format.TGA);
            CheckEqual(4, decodedTGA32.channels, "32-bit TGA channels");
            checkImageBytes(decodedTGA32, rgbaBytes, "32-bit TGA pixels");

            byte[] ppm = rgb.encode(GImage.Format.PPM);
            Check(GImage.resolveFormat(ppm, ppm.Length) == GImage.Format.PPM, "PPM format detection");
            GImage decodedPPM = new GImage(ppm, ppm.Length, GImage.Format.PPM);
            CheckEqual(3, decodedPPM.channels, "PPM channels");
            checkImageBytes(decodedPPM, rgbBytes, "PPM pixels");

            byte[] ppmASCII = rgb.encode(GImage.Format.PPM_ASCII);
            Check(GImage.resolveFormat(ppmASCII, ppmASCII.Length) == GImage.Format.PPM_ASCII, "ASCII PPM format detection");
            GImage decodedPPMASCII = new GImage(ppmASCII, ppmASCII.Length, GImage.Format.PPM_ASCII);
            CheckEqual(3, decodedPPMASCII.channels, "ASCII PPM channels");
            checkImageBytes(decodedPPMASCII, rgbBytes, "ASCII PPM pixels");

            byte[] p1 = Encoding.ASCII.GetBytes("P1\n2 2\n0 1 1 0\n");
            GImage decodedP1 = new GImage(p1, p1.Length, GImage.Format.PPM_ASCII);
            checkImageBytes(decodedP1, new byte[]
            {
                0, 0, 0,
                255, 255, 255,
                255, 255, 255,
                0, 0, 0
            }, "P1 pixels");

            byte[] p2 = Encoding.ASCII.GetBytes("P2\n2 1\n255\n10 200\n");
            GImage decodedP2 = new GImage(p2, p2.Length, GImage.Format.PPM_ASCII);
            checkImageBytes(decodedP2, new byte[]
            {
                10, 10, 10,
                200, 200, 200
            }, "P2 pixels");

            byte[] bmp1 = createIndexedBMP(1,
                new byte[] { 0x80, 0, 0, 0 },
                new byte[] { 0x40, 0, 0, 0 }, 2);
            GImage decodedBMP1 = new GImage(bmp1, bmp1.Length, GImage.Format.BMP);
            checkImageBytes(decodedBMP1, new byte[]
            {
                0, 0, 0,
                255, 255, 255,
                255, 255, 255,
                0, 0, 0
            }, "1-bit BMP pixels");

            byte[] bmp4 = createIndexedBMP(4,
                new byte[] { 0x10, 0, 0, 0 },
                new byte[] { 0x01, 0, 0, 0 }, 2);
            GImage decodedBMP4 = new GImage(bmp4, bmp4.Length, GImage.Format.BMP);
            checkImageBytes(decodedBMP4, new byte[]
            {
                0, 0, 0,
                255, 255, 255,
                255, 255, 255,
                0, 0, 0
            }, "4-bit BMP pixels");

            byte[] bmp8 = createIndexedBMP(8,
                new byte[] { 1, 0, 0, 0 },
                new byte[] { 0, 1, 0, 0 }, 2);
            GImage decodedBMP8 = new GImage(bmp8, bmp8.Length, GImage.Format.BMP);
            checkImageBytes(decodedBMP8, new byte[]
            {
                0, 0, 0,
                255, 255, 255,
                255, 255, 255,
                0, 0, 0
            }, "8-bit BMP pixels");
        }

        private static void testGImageDesktopCodecs()
        {
            byte[] rgbBytes = new byte[]
            {
                255, 0, 0,
                0, 255, 0,
                0, 0, 255,
                255, 255, 255
            };
            GImage rgb = GImage.fromMemory(rgbBytes, 2, 2, 3);
            byte[] png = rgb.encode(GImage.Format.PNG);
            Check(GImage.resolveFormat(png, png.Length) == GImage.Format.PNG, "PNG format detection");
            GImage decodedPNG = new GImage(png, png.Length, GImage.Format.PNG);
            CheckEqual(3, decodedPNG.channels, "PNG RGB channels");
            checkImageBytes(decodedPNG, rgbBytes, "PNG RGB pixels");

            byte[] rgbaBytes = new byte[]
            {
                255, 0, 0, 17,
                0, 255, 0, 68,
                0, 0, 255, 129,
                255, 255, 255, 255
            };
            GImage rgba = GImage.fromMemory(rgbaBytes, 2, 2, 4);
            byte[] rgbaPNG = rgba.encode(GImage.Format.PNG);
            GImage decodedRGBA = new GImage(rgbaPNG, rgbaPNG.Length, GImage.Format.PNG);
            CheckEqual(4, decodedRGBA.channels, "PNG RGBA channels");
            checkImageBytes(decodedRGBA, rgbaBytes, "PNG RGBA pixels");

            byte[] grayBytes = new byte[] { 0, 64, 128, 255 };
            GImage gray = GImage.fromMemory(grayBytes, 2, 2, 1);
            byte[] grayPNG = gray.encode(GImage.Format.PNG);
            GImage decodedGray = new GImage(grayPNG, grayPNG.Length, GImage.Format.PNG);
            CheckEqual(1, decodedGray.channels, "PNG grayscale channels");
            checkImageBytes(decodedGray, grayBytes, "PNG grayscale pixels");

            bool explicitFormatRejectedMismatch = false;
            try
            {
                GImage wrong = new GImage(png, png.Length, GImage.Format.BMP);
            }
            catch (GImage.Error)
            {
                explicitFormatRejectedMismatch = true;
            }
            Check(explicitFormatRejectedMismatch, "Explicit format is not replaced by content detection");

            bool explicitUnknownRejected = false;
            try
            {
                GImage unknown = new GImage(png, png.Length, GImage.Format.UNKNOWN);
            }
            catch (GImage.Error)
            {
                explicitUnknownRejected = true;
            }
            Check(explicitUnknownRejected, "Explicit unknown format is not replaced by content detection");

            Check(GImage.resolveFormat("ignored.bmp", png, png.Length, GImage.Format.PNG) == GImage.Format.PNG,
                "Explicit format is returned by resolveFormat");

            string temporaryFile = Path.Combine(Path.GetTempPath(),
                "g3d-image-" + Guid.NewGuid().ToString("N") + ".bmp");
            try
            {
                rgb.save(temporaryFile, GImage.Format.PNG);
                GImage savedPNG = new GImage(temporaryFile, GImage.Format.PNG);
                checkImageBytes(savedPNG, rgbBytes, "Explicit PNG save pixels");
            }
            finally
            {
                if (File.Exists(temporaryFile))
                {
                    File.Delete(temporaryFile);
                }
            }

            byte[] jpegSource = new byte[16 * 16 * 3];
            for (int y = 0; y < 16; ++y)
            {
                for (int x = 0; x < 16; ++x)
                {
                    int index = (x + y * 16) * 3;
                    if (x < 8 && y < 8)
                    {
                        jpegSource[index] = 255;
                    }
                    else if (x >= 8 && y < 8)
                    {
                        jpegSource[index + 1] = 255;
                    }
                    else if (x < 8)
                    {
                        jpegSource[index + 2] = 255;
                    }
                    else
                    {
                        jpegSource[index] = 255;
                        jpegSource[index + 1] = 255;
                        jpegSource[index + 2] = 255;
                    }
                }
            }
            GImage jpegImage = GImage.fromMemory(jpegSource, 16, 16, 3);
            byte[] jpeg = jpegImage.encode(GImage.Format.JPEG);
            Check(GImage.resolveFormat(jpeg, jpeg.Length) == GImage.Format.JPEG, "JPEG format detection");
            GImage decodedJPEG = new GImage(jpeg, jpeg.Length, GImage.Format.JPEG);
            CheckEqual(16, decodedJPEG.width, "JPEG width");
            CheckEqual(16, decodedJPEG.height, "JPEG height");
            CheckEqual(3, decodedJPEG.channels, "JPEG channels");
            byte[] decodedJPEGBytes = decodedJPEG.getBytes();
            long error = 0;
            for (int i = 0; i < jpegSource.Length; ++i)
            {
                error += Math.Abs((int)jpegSource[i] - (int)decodedJPEGBytes[i]);
            }
            double averageError = error / (double)jpegSource.Length;
            Check(averageError < 12.0, "JPEG average pixel error (expected < 12, actual " + averageError + ")");
        }

        private static void testGImagePublicAPI()
        {
            GImage image = new GImage(2, 2, 3);
            image.setPixel3(0, 0, new Color3uint8(255, 0, 0));
            image.setPixel3(1, 0, new Color3uint8(0, 255, 0));
            image.setPixel3(0, 1, new Color3uint8(0, 0, 255));
            image.setPixel3(1, 1, new Color3uint8(255, 255, 255));

            GImage copy = new GImage(image);
            copy.setPixel3(0, 0, new Color3uint8(1, 2, 3));
            Check(image.pixel3(0, 0) == new Color3uint8(255, 0, 0), "GImage copy is independent");

            GImage alpha = new GImage(2, 2, 1);
            alpha.setPixel1(0, 0, 10);
            alpha.setPixel1(1, 0, 20);
            alpha.setPixel1(0, 1, 30);
            alpha.setPixel1(1, 1, 40);
            GImage combined = image.insertRedAsAlpha(alpha);
            CheckEqual(4, combined.channels, "insertRedAsAlpha channels");
            CheckEqual(10, combined.pixel4(0, 0).a, "insertRedAsAlpha alpha");
            CheckEqual(3, combined.stripAlpha().channels, "stripAlpha channels");

            GImage destination = new GImage(4, 4, 3);
            Check(GImage.pasteSubImage(destination, image, 1, 1, 0, 0, 2, 2), "pasteSubImage succeeds");
            Check(destination.pixel3(1, 1) == image.pixel3(0, 0), "pasteSubImage pixels");
            GImage subImage = new GImage();
            Check(GImage.copySubImage(subImage, image, 0, 0, 2, 2), "copySubImage succeeds");
            CheckEqual(12, subImage.sizeInMemory() - 32, "copySubImage byte count");

            image.convertToRGBA();
            CheckEqual(4, image.channels, "convertToRGBA channels");
            image.convertToRGB();
            CheckEqual(3, image.channels, "convertToRGB channels");
            image.convertToL8();
            CheckEqual(1, image.channels, "convertToL8 channels");

            GImage checker = new GImage(4, 4, 3);
            GImage.makeCheckerboard(checker, 1);
            Check(checker.pixel3(0, 0) == new Color3uint8(0, 0, 0), "makeCheckerboard first pixel");
            Check(checker.pixel3(1, 0) == new Color3uint8(255, 255, 255), "makeCheckerboard second pixel");

            byte[] encoded;
            int encodedLength;
            image.encode(GImage.Format.BMP, out encoded, out encodedLength);
            CheckEqual(encoded.Length, encodedLength, "encode output length");
            BinaryOutput output = new BinaryOutput();
            image.encode(GImage.Format.BMP, output);
            Check(output.size() > 0, "encode to BinaryOutput");

            Check(GImage.supportedFormat("bmp"), "supportedFormat BMP");
            Check(GImage.stringToFormat("JPEG") == GImage.Format.JPEG, "stringToFormat JPEG");
            Check(GImage.stringToFormat("unknown") == GImage.Format.UNKNOWN, "stringToFormat unknown");
        }

        private static byte[] createIndexedBMP(int bitCount, byte[] topRow, byte[] bottomRow, int width)
        {
            int paletteCount = 2;
            int paletteBytes = paletteCount * 4;
            int rowBytes = (width * bitCount + 7) / 8;
            rowBytes = (rowBytes + 3) / 4 * 4;
            int pixelBytes = rowBytes * 2;

            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write((byte)'B');
                    writer.Write((byte)'M');
                    writer.Write((uint)(54 + paletteBytes + pixelBytes));
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write((uint)(54 + paletteBytes));
                    writer.Write((uint)40);
                    writer.Write((uint)width);
                    writer.Write((uint)2);
                    writer.Write((ushort)1);
                    writer.Write((ushort)bitCount);
                    writer.Write((uint)0);
                    writer.Write((uint)pixelBytes);
                    writer.Write((uint)0);
                    writer.Write((uint)0);
                    writer.Write((uint)paletteCount);
                    writer.Write((uint)0);
                    writer.Write(new byte[] { 0, 0, 0, 0 });
                    writer.Write(new byte[] { 255, 255, 255, 0 });
                    writer.Write(topRow);
                    writer.Write(bottomRow);
                    writer.Flush();
                    return stream.ToArray();
                }
            }
        }

        private static void checkImageBytes(GImage image, byte[] expected, string name)
        {
            byte[] actual = image.getBytes();
            Check(actual != null, name + " data exists");
            if (actual == null)
            {
                return;
            }
            CheckEqual(expected.Length, actual.Length, name + " length");
            int count = expected.Length < actual.Length ? expected.Length : actual.Length;
            for (int i = 0; i < count; ++i)
            {
                if (expected[i] != actual[i])
                {
                    Check(false, name + " byte " + i + " (expected " + expected[i] + ", actual " + actual[i] + ")");
                    break;
                }
            }
        }
    }
}

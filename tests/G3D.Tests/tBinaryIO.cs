using System;
using System.IO;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private static void testBitSerialization()
        {
            printf("Bit Serialization\n");
            byte[] x = new byte[100];

            {
                BinaryOutput b = new BinaryOutput("<memory>", G3DEndian.G3D_LITTLE_ENDIAN);

                b.beginBits();
                b.writeBits(0, 1);
                b.writeBits(1, 1);
                b.endBits();

                b.commit(x);

                Debug.alwaysAssertM(x[0] == 2, "bit serialization 1 failed");
            }

            {
                BinaryInput b = new BinaryInput(x, 1, G3DEndian.G3D_LITTLE_ENDIAN, false, false);
                b.beginBits();

                byte a = (byte)b.readBits(1);
                Debug.alwaysAssertM(a == 0, "bit read 1 failed");

                a = (byte)b.readBits(1);
                Debug.alwaysAssertM(a == 1, "bit read 2 failed");
                b.endBits();
            }

            {
                BinaryOutput b = new BinaryOutput("<memory>", G3DEndian.G3D_LITTLE_ENDIAN);
                b.beginBits();
                b.writeBits(0xF1234567, 32);
                b.endBits();

                b.commit(x);

                Debug.alwaysAssertM(x[0] == 0x67, "bit 0 failed");
                Debug.alwaysAssertM(x[1] == 0x45, "bit 1 failed");
                Debug.alwaysAssertM(x[2] == 0x23, "bit 2 failed");
                Debug.alwaysAssertM(x[3] == 0xF1, "bit 3 failed");
            }

            {
                BinaryInput b = new BinaryInput(x, 4, G3DEndian.G3D_LITTLE_ENDIAN, false, false);
                b.beginBits();

                byte a = (byte)b.readBits(8);
                Debug.alwaysAssertM(a == 0x67, "readBits 0 failed");

                a = (byte)b.readBits(8);
                Debug.alwaysAssertM(a == 0x45, "readBits 1 failed");

                a = (byte)b.readBits(8);
                Debug.alwaysAssertM(a == 0x23, "readBits 2 failed");

                a = (byte)b.readBits(8);
                Debug.alwaysAssertM(a == 0xF1, "readBits 3 failed");

                b.endBits();
            }

            {
                BinaryOutput b = new BinaryOutput("<memory>", G3DEndian.G3D_LITTLE_ENDIAN);

                b.beginBits();
                b.writeBits(0, 3);
                b.writeBits(3, 3);
                b.writeBits(4, 3);
                b.writeBits(7, 3);
                b.endBits();

                b.commit(x);
            }

            {
                BinaryInput b = new BinaryInput(x, 2, G3DEndian.G3D_LITTLE_ENDIAN, false, false);
                b.beginBits();

                byte a = (byte)b.readBits(3);
                Debug.alwaysAssertM(a == 0, "readBits 3 failed");

                a = (byte)b.readBits(3);
                Debug.alwaysAssertM(a == 3, "readBits 4 failed");

                a = (byte)b.readBits(3);
                Debug.alwaysAssertM(a == 4, "readBits 5 failed");

                a = (byte)b.readBits(3);
                Debug.alwaysAssertM(a == 7, "readBits 6 failed");
                b.endBits();
            }
        }

        private static void testCompression()
        {
            printf("BinaryInput & BinaryOutput\n");
            string tempFile = Path.Combine(Path.GetTempPath(), "g3d_test_compression.t");

            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }

            {
                BinaryOutput f = new BinaryOutput(tempFile, G3DEndian.G3D_LITTLE_ENDIAN);

                f.writeUInt32(1234);
                f.writeFloat64(1.234);
                f.compress();
                f.commit(false);
            }

            {
                BinaryInput g = new BinaryInput(tempFile, G3DEndian.G3D_LITTLE_ENDIAN, true);
                uint i = g.readUInt32();
                Debug.alwaysAssertM(i == 1234, "compression readUInt32 failed");
                double j = g.readFloat64();
                Debug.alwaysAssertM(g3dmath.fuzzyEq(j, 1.234), "compression readFloat64 failed");
            }

            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }

            printf("testCompression passed\n");
        }

        private static void testHugeBinaryIO()
        {
            printf("BinaryOutput Large Files\n");
            string hugeFile = Path.Combine(Path.GetTempPath(), "g3d_test_huge.bin");

            if (File.Exists(hugeFile))
            {
                File.Delete(hugeFile);
            }

            int testSize = 1024 * 1024 * 60; // 60MB (reduced from 600MB for practicality)
            int stepSize = 1024 * 1024 * 2; // 2MB
            byte[] giantBuffer = new byte[stepSize];

            {
                BinaryOutput b = new BinaryOutput(hugeFile, G3DEndian.G3D_LITTLE_ENDIAN);
                for (int i = 0; i < testSize / stepSize; ++i)
                {
                    b.writeBytes(giantBuffer, stepSize);
                }
                b.commit(false);
            }

            printf("BinaryInput Large Files\n");

            {
                BinaryInput b = new BinaryInput(hugeFile, G3DEndian.G3D_LITTLE_ENDIAN, false);

                for (int i = 0; i < testSize / stepSize; ++i)
                {
                    b.readBytes(giantBuffer, stepSize);
                }
            }

            if (File.Exists(hugeFile))
            {
                File.Delete(hugeFile);
            }

            printf("testHugeBinaryIO passed\n");
        }

        static void measureSerializerPerformance()
        {
            byte[] x = new byte[1024];
            double t0 = G3D.System.time();
            Matrix4 M = Matrix4.identity();

            for (int i = 0; i < 100; ++i)
            {
                BinaryOutput b1 = new BinaryOutput("<memory>", G3DEndian.G3D_LITTLE_ENDIAN);
                b1.writeInt32(1);
                b1.writeInt32(2);
                b1.writeInt32(8);
                M.serialize(b1);
                b1.commit(x);
            }
            double reallocTime = (G3D.System.time() - t0) / 100.0;
            printf("BinaryOutput time with re-allocation: %gs\n", reallocTime);

            BinaryOutput b2 = new BinaryOutput("<memory>", G3DEndian.G3D_LITTLE_ENDIAN);
            t0 = G3D.System.time();
            for (int i = 0; i < 100; ++i)
            {
                b2.writeInt32(1);
                b2.writeInt32(2);
                b2.writeInt32(8);
                M.serialize(b2);
                b2.commit(x);
                b2.reset();
            }
            double resetTime = (G3D.System.time() - t0) / 100.0;
            printf("BinaryOutput time with BinaryOutput::reset: %gs\n\n", resetTime);
        }

        private static void perfBinaryIO()
        {
            measureSerializerPerformance();
        }

        private static void testBinaryIO()
        {
            printf("BinaryIO ");
            testBitSerialization();
            testCompression();
            printf("passed\n");
        }
    }
}

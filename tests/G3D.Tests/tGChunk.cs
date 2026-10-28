using System;
using System.IO;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private enum ChunkType { HEADER, NAME, BODY, NUM, DATA };

        private static void testGChunk()
        {
            printf("GChunk ");

            string tempFile = Path.Combine(Path.GetTempPath(), "g3d_test_gchunk.dat");

            // Write phase
            {
                BinaryOutput b = new BinaryOutput(tempFile, G3DEndian.G3D_LITTLE_ENDIAN);

                {
                    GChunk c = new GChunk(b, (ushort)ChunkType.HEADER);

                    {
                        GChunk c2 = new GChunk(b, (ushort)ChunkType.NAME, BinaryFormat.STRING_BINFMT);
                        b.writeString("abcdefg");
                        c2.finish(b);
                    }
                    c.finish(b);
                }

                {
                    GChunk c = new GChunk(b, (ushort)ChunkType.BODY);

                    {
                        GChunk c2 = new GChunk(b, (ushort)ChunkType.NUM, BinaryFormat.INT32_BINFMT);
                        b.writeInt32(10);
                        c2.finish(b);
                    }

                    {
                        GChunk c2 = new GChunk(b, (ushort)ChunkType.DATA, BinaryFormat.FLOAT32_BINFMT);
                        for (int i = 0; i < 10; ++i)
                        {
                            b.writeFloat32((float)Math.Sqrt(i));
                        }
                        c2.finish(b);
                    }
                    c.finish(b);
                }

                b.commit(false);
            }

            // Read phase
            {
                BinaryInput b = new BinaryInput(tempFile, G3DEndian.G3D_LITTLE_ENDIAN, false);

                {
                    GChunk c = new GChunk(b, (ushort)ChunkType.HEADER);

                    {
                        GChunk c2 = new GChunk(b, (ushort)ChunkType.NAME, BinaryFormat.STRING_BINFMT);
                        Debug.alwaysAssertM(c2.count == -1, "count should be -1 for STRING_BINFMT");
                        Debug.alwaysAssertM(b.readString() == "abcdefg", "string content mismatch");
                        c2.finish(b);
                    }
                    c.finish(b);
                }

                {
                    GChunk c = new GChunk(b, (ushort)ChunkType.BODY);

                    {
                        GChunk c2 = new GChunk(b, (ushort)ChunkType.NUM, BinaryFormat.INT32_BINFMT);
                        Debug.alwaysAssertM(c2.count == 1, "count should be 1 for INT32_BINFMT");
                        Debug.alwaysAssertM(b.readInt32() == 10, "NUM value should be 10");
                        c2.finish(b);
                    }

                    {
                        GChunk c2 = new GChunk(b, (ushort)ChunkType.DATA, BinaryFormat.FLOAT32_BINFMT);
                        Debug.alwaysAssertM(c2.count == 10, "count should be 10 for FLOAT32_BINFMT");
                        for (int i = 0; i < 10; ++i)
                        {
                            Debug.alwaysAssertM(g3dmath.fuzzyEq(b.readFloat32(), Math.Sqrt(i)), "Data in chunk corrupted");
                        }
                        c2.finish(b);
                    }
                    c.finish(b);
                }
            }

            // Clean up
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }

            printf("passed\n");
        }
    }
}
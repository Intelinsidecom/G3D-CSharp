/**
 @file BinaryInput.h

 @maintainer Morgan McGuire, graphics3d.com

 @created 2001-08-09
 @edited  2005-06-07

 Copyright 2000-2005, Morgan McGuire.
 All rights reserved.
 */

using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace G3D
{
    public class BinaryInput
    {
        public const bool NO_COPY = false;
        private const int INITIAL_BUFFER_LENGTH = 50000000;

        private G3DEndian fileEndian;
        private string filename;
        private bool swapBytes;
        private int bitPos;
        private uint bitString;
        private int beginEndBits;
        private int alreadyRead;
        private int length;
        private int bufferLength;
        private byte[] buffer;
        private int pos;
        private bool freeBuffer;
        private bool compressed;

        private static G3DEndian machineEndian()
        {
            return BitConverter.IsLittleEndian ? G3DEndian.G3D_LITTLE_ENDIAN : G3DEndian.G3D_BIG_ENDIAN;
        }

        private static bool needSwapBytes(G3DEndian fileEndian)
        {
            return fileEndian != machineEndian();
        }

        private static uint readUInt32(byte[] data, bool swapBytes)
        {
            if (swapBytes)
            {
                return (uint)(data[3] | (data[2] << 8) | (data[1] << 16) | (data[0] << 24));
            }
            return (uint)(data[0] | (data[1] << 8) | (data[2] << 16) | (data[3] << 24));
        }

        private static uint readUInt32(string filename, bool swapBytes)
        {
            using (FileStream file = File.OpenRead(filename))
            {
                byte[] bytes = new byte[4];
                int read = file.Read(bytes, 0, 4);
                if (read != 4)
                {
                    throw new InvalidOperationException("Cannot read 4 bytes from file for compressed size.");
                }
                if (swapBytes)
                {
                    return (uint)(bytes[3] | (bytes[2] << 8) | (bytes[1] << 16) | (bytes[0] << 24));
                }
                return (uint)(bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24));
            }
        }

        private void decompress(byte[] source, int sourceOffset, int sourceLength)
        {
            int uncompressedLength = (int)readUInt32(source, swapBytes);
            if (uncompressedLength < 0)
            {
                throw new InvalidOperationException("Invalid compressed size.");
            }

            // zlib format: 2-byte header, deflate data, 4-byte Adler-32 checksum.
            if (sourceLength < 6)
            {
                throw new InvalidOperationException("Compressed data is too short.");
            }

            buffer = new byte[uncompressedLength];
            using (MemoryStream stream = new MemoryStream(source, sourceOffset + 6, sourceLength - 10))
            using (DeflateStream deflate = new DeflateStream(stream, CompressionMode.Decompress))
            {
                int read = 0;
                while (read < uncompressedLength)
                {
                    int count = deflate.Read(buffer, read, uncompressedLength - read);
                    if (count <= 0)
                    {
                        throw new InvalidOperationException("Compressed data is truncated.");
                    }
                    read += count;
                }
            }

            length = uncompressedLength;
            bufferLength = uncompressedLength;
        }

        private void decompress(string filename, int sourceOffset, int sourceLength)
        {
            using (FileStream file = File.OpenRead(filename))
            {
                file.Seek(sourceOffset, SeekOrigin.Begin);
                byte[] compressedData = new byte[sourceLength];
                int read = file.Read(compressedData, 0, sourceLength);
                if (read != sourceLength)
                {
                    throw new InvalidOperationException("Cannot read compressed data from file.");
                }
                decompress(compressedData, 0, sourceLength);
            }
        }

        private void loadIntoMemory(int startPosition, int minLength)
        {
            Debug.debugAssertM(filename != "<memory>", "Read past end of file.");

            int absPos = alreadyRead + pos;

            if (bufferLength < minLength)
            {
                bufferLength = minLength;
                Debug.debugAssert(freeBuffer);
                buffer = new byte[bufferLength];
                if (buffer == null)
                {
                    throw new InvalidOperationException("Tried to read a larger memory chunk than could fit in memory. (2)");
                }
            }

            alreadyRead = startPosition;

            using (FileStream file = File.OpenRead(filename))
            {
                file.Seek(alreadyRead, SeekOrigin.Begin);
                int toRead = Math.Min(bufferLength, length - alreadyRead);
                int read = file.Read(buffer, 0, toRead);
                Debug.debugAssert(read == toRead);
            }

            pos = absPos - alreadyRead;
            Debug.debugAssert(pos >= 0);
        }

        private void prepareToRead(int bytes)
        {
            Debug.debugAssertM(pos + bytes + alreadyRead <= length, "Read past end of file.");

            if (pos + bytes > bufferLength)
            {
                loadIntoMemory(pos + alreadyRead, bytes);
            }
        }

        public BinaryInput(string filename, G3DEndian fileEndian, bool compressed)
        {
            alreadyRead = 0;
            freeBuffer = true;
            this.filename = filename;
            this.compressed = compressed;
            buffer = null;
            bufferLength = 0;
            length = 0;
            pos = 0;
            beginEndBits = 0;
            bitPos = 0;
            fileEndian = fileEndian;
            swapBytes = needSwapBytes(fileEndian);
            if (compressed)
            {
                // For compressed files, the file format is:
                // [4-byte uncompressed length][zlib header][deflate data][4-byte Adler32]
                // We need to pass the entire file to decompress (which expects the 4-byte length header at offset 0)
                long fileSize = new FileInfo(filename).Length;
                int compressedDataSize = (int)fileSize;
                decompress(filename, 0, compressedDataSize);
                // Now length is set by decompress
            }
            else
            {
                loadFile(filename);
            }
        }

        public BinaryInput(string filename, G3DEndian fileEndian)
            : this(filename, fileEndian, false)
        {
        }

        private void loadFile(string filename)
        {
            using (FileStream file = File.OpenRead(filename))
            {
                length = (int)file.Length;
                if (!compressed && length > INITIAL_BUFFER_LENGTH)
                {
                    bufferLength = INITIAL_BUFFER_LENGTH;
                }
                else
                {
                    bufferLength = length;
                }

                Debug.debugAssert(freeBuffer);
                buffer = new byte[bufferLength];
                if (buffer == null)
                {
                    if (compressed)
                    {
                        throw new OutOfMemoryException("Not enough memory to load compressed file. (1)");
                    }

                    while (buffer == null && bufferLength > 1024)
                    {
                        bufferLength /= 2;
                        buffer = new byte[bufferLength];
                    }
                }
                Debug.debugAssert(buffer != null);

                int read = file.Read(buffer, 0, bufferLength);
                Debug.debugAssert(read == bufferLength);
            }

            pos = 0;

            if (compressed)
            {
                if (bufferLength != length)
                {
                    throw new OutOfMemoryException("Not enough memory to load compressed file. (2)");
                }

                byte[] tempBuffer = buffer;
                decompress(tempBuffer, 0, tempBuffer.Length);
                // decompress allocates buffer; tempBuffer becomes eligible for collection.
            }
        }

        public BinaryInput(byte[] data, int dataLen, G3DEndian dataEndian, bool compressed, bool copyMemory)
        {
            Debug.debugAssert(!(compressed && !copyMemory));

            beginEndBits = 0;
            bitPos = 0;
            alreadyRead = 0;
            bufferLength = 0;
            freeBuffer = copyMemory || compressed;
            filename = "<memory>";
            pos = 0;
            fileEndian = dataEndian;
            swapBytes = needSwapBytes(dataEndian);

            if (compressed)
            {
                length = (int)readUInt32(data, swapBytes);
                Debug.debugAssert(freeBuffer);
                decompress(data, 0, dataLen);
            }
            else
            {
                length = dataLen;
                bufferLength = length;
                if (!copyMemory)
                {
                    Debug.debugAssert(!freeBuffer);
                    buffer = data;
                }
                else
                {
                    Debug.debugAssert(freeBuffer);
                    buffer = new byte[length];
                    Array.Copy(data, buffer, dataLen);
                }
            }
        }

        public string getFilename()
        {
            return filename;
        }

        public byte[] getCArray()
        {
            if (alreadyRead > 0)
            {
                throw new InvalidOperationException("Cannot getCArray for a huge file");
            }
            return buffer;
        }

        public byte this[int n]
        {
            get
            {
                setPosition(n);
                return readUInt8();
            }
        }

        public int getLength()
        {
            return length;
        }

        public int size()
        {
            return getLength();
        }

        public int getPosition()
        {
            return pos + alreadyRead;
        }

        public void setPosition(int p)
        {
            Debug.debugAssertM(p <= length, "Read past end of file");
            pos = p - alreadyRead;
            if (pos < 0 || pos > bufferLength)
            {
                loadIntoMemory(pos + alreadyRead, 0);
            }
        }

        public void reset()
        {
            setPosition(0);
        }

        public sbyte readInt8()
        {
            prepareToRead(1);
            return (sbyte)buffer[pos++];
        }

        public bool readBool8()
        {
            return readInt8() != 0;
        }

        public byte readUInt8()
        {
            prepareToRead(1);
            return buffer[pos++];
        }

        public ushort readUInt16()
        {
            prepareToRead(2);
            pos += 2;
            if (swapBytes)
            {
                return (ushort)(buffer[pos - 1] | (buffer[pos - 2] << 8));
            }
            return (ushort)(buffer[pos - 2] | (buffer[pos - 1] << 8));
        }

        public short readInt16()
        {
            return (short)readUInt16();
        }

        public uint readUInt32()
        {
            prepareToRead(4);
            pos += 4;
            if (swapBytes)
            {
                return (uint)(buffer[pos - 1] | (buffer[pos - 2] << 8) | (buffer[pos - 3] << 16) | (buffer[pos - 4] << 24));
            }
            return (uint)(buffer[pos - 4] | (buffer[pos - 3] << 8) | (buffer[pos - 2] << 16) | (buffer[pos - 1] << 24));
        }

        public int readInt32()
        {
            return (int)readUInt32();
        }

        public ulong readUInt64()
        {
            prepareToRead(8);
            byte[] outBytes = new byte[8];
            if (swapBytes)
            {
                for (int i = 0; i < 8; ++i)
                {
                    outBytes[i] = buffer[pos + 7 - i];
                }
            }
            else
            {
                Array.Copy(buffer, pos, outBytes, 0, 8);
            }
            pos += 8;
            return BitConverter.ToUInt64(outBytes, 0);
        }

        public long readInt64()
        {
            return (long)readUInt64();
        }

        public float readFloat32()
        {
            uint value = readUInt32();
            byte[] bytes = BitConverter.GetBytes(value);
            return BitConverter.ToSingle(bytes, 0);
        }

        public double readFloat64()
        {
            ulong value = readUInt64();
            byte[] bytes = BitConverter.GetBytes(value);
            return BitConverter.ToDouble(bytes, 0);
        }

        public void readBytes(int n, byte[] bytes)
        {
            prepareToRead(n);
            if (bytes == null || bytes.Length < n)
            {
                throw new ArgumentException("Destination buffer is too small.", "bytes");
            }
            Array.Copy(buffer, pos, bytes, 0, n);
            pos += n;
        }

        public void readBytes(byte[] bytes, int n)
        {
            readBytes(n, bytes);
        }

        public string readString(int n)
        {
            prepareToRead(n);
            Debug.debugAssertM(pos + n <= length, "Read past end of file");
            byte[] bytes = new byte[n];
            Array.Copy(buffer, pos, bytes, 0, n);
            pos += n;
            int len = 0;
            while (len < bytes.Length && bytes[len] != 0)
            {
                ++len;
            }
            return Encoding.Default.GetString(bytes, 0, len);
        }

        public string readString()
        {
            int n = 0;
            if (pos + alreadyRead + n < length - 1)
            {
                prepareToRead(1);
            }

            if (pos + alreadyRead + n < length - 1 && buffer[pos + n] != 0)
            {
                ++n;
                while (pos + alreadyRead + n < length - 1 && buffer[pos + n] != 0)
                {
                    prepareToRead(1);
                    ++n;
                }
            }

            ++n;
            return readString(n);
        }

        public string readStringEven()
        {
            string x = readString();
            if (hasMore() && ((x.Length + 1) % 2 != 0))
            {
                skip(1);
            }
            return x;
        }

        public string readString32()
        {
            int len = readInt32();
            return readString(len);
        }

        public void readBool8(bool[] output, int n)
        {
            if (output == null || output.Length < n)
            {
                throw new ArgumentException("Destination buffer is too small.", "output");
            }
            for (int i = 0; i < n; ++i)
            {
                output[i] = readBool8();
            }
        }

        public void readBool8(Array<bool> output, int n)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            output.resize(n);
            for (int i = 0; i < n; ++i)
            {
                output[i] = readBool8();
            }
        }

        public void readUInt8(byte[] output, int n)
        {
            readBytes(n, output);
        }

        public void readUInt8(Array<byte> output, int n)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            output.resize(n);
            for (int i = 0; i < n; ++i)
            {
                output[i] = readUInt8();
            }
        }

        public void readInt8(sbyte[] output, int n)
        {
            if (output == null || output.Length < n)
            {
                throw new ArgumentException("Destination buffer is too small.", "output");
            }
            for (int i = 0; i < n; ++i)
            {
                output[i] = readInt8();
            }
        }

        public void readInt8(Array<sbyte> output, int n)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            output.resize(n);
            for (int i = 0; i < n; ++i)
            {
                output[i] = readInt8();
            }
        }

        public void readUInt16(ushort[] output, int n)
        {
            if (output == null || output.Length < n)
            {
                throw new ArgumentException("Destination buffer is too small.", "output");
            }
            for (int i = 0; i < n; ++i)
            {
                output[i] = readUInt16();
            }
        }

        public void readUInt16(Array<ushort> output, int n)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            output.resize(n);
            for (int i = 0; i < n; ++i)
            {
                output[i] = readUInt16();
            }
        }

        public void readInt16(short[] output, int n)
        {
            if (output == null || output.Length < n)
            {
                throw new ArgumentException("Destination buffer is too small.", "output");
            }
            for (int i = 0; i < n; ++i)
            {
                output[i] = readInt16();
            }
        }

        public void readInt16(Array<short> output, int n)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            output.resize(n);
            for (int i = 0; i < n; ++i)
            {
                output[i] = readInt16();
            }
        }

        public void readUInt32(uint[] output, int n)
        {
            if (output == null || output.Length < n)
            {
                throw new ArgumentException("Destination buffer is too small.", "output");
            }
            for (int i = 0; i < n; ++i)
            {
                output[i] = readUInt32();
            }
        }

        public void readUInt32(Array<uint> output, int n)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            output.resize(n);
            for (int i = 0; i < n; ++i)
            {
                output[i] = readUInt32();
            }
        }

        public void readInt32(int[] output, int n)
        {
            if (output == null || output.Length < n)
            {
                throw new ArgumentException("Destination buffer is too small.", "output");
            }
            for (int i = 0; i < n; ++i)
            {
                output[i] = readInt32();
            }
        }

        public void readInt32(Array<int> output, int n)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            output.resize(n);
            for (int i = 0; i < n; ++i)
            {
                output[i] = readInt32();
            }
        }

        public void readUInt64(ulong[] output, int n)
        {
            if (output == null || output.Length < n)
            {
                throw new ArgumentException("Destination buffer is too small.", "output");
            }
            for (int i = 0; i < n; ++i)
            {
                output[i] = readUInt64();
            }
        }

        public void readUInt64(Array<ulong> output, int n)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            output.resize(n);
            for (int i = 0; i < n; ++i)
            {
                output[i] = readUInt64();
            }
        }

        public void readInt64(long[] output, int n)
        {
            if (output == null || output.Length < n)
            {
                throw new ArgumentException("Destination buffer is too small.", "output");
            }
            for (int i = 0; i < n; ++i)
            {
                output[i] = readInt64();
            }
        }

        public void readInt64(Array<long> output, int n)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            output.resize(n);
            for (int i = 0; i < n; ++i)
            {
                output[i] = readInt64();
            }
        }

        public void readFloat32(float[] output, int n)
        {
            if (output == null || output.Length < n)
            {
                throw new ArgumentException("Destination buffer is too small.", "output");
            }
            for (int i = 0; i < n; ++i)
            {
                output[i] = readFloat32();
            }
        }

        public void readFloat32(Array<float> output, int n)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            output.resize(n);
            for (int i = 0; i < n; ++i)
            {
                output[i] = readFloat32();
            }
        }

        public void readFloat64(double[] output, int n)
        {
            if (output == null || output.Length < n)
            {
                throw new ArgumentException("Destination buffer is too small.", "output");
            }
            for (int i = 0; i < n; ++i)
            {
                output[i] = readFloat64();
            }
        }

        public void readFloat64(Array<double> output, int n)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            output.resize(n);
            for (int i = 0; i < n; ++i)
            {
                output[i] = readFloat64();
            }
        }

        public void skip(int n)
        {
            setPosition(getPosition() + n);
        }

        public bool hasMore()
        {
            return getPosition() < length;
        }

        public void beginBits()
        {
            Debug.debugAssert(beginEndBits == 0);
            beginEndBits = 1;
            bitPos = 0;
            Debug.debugAssertM(hasMore(), "Can't call beginBits when at the end of a file");
            bitString = readUInt8();
        }

        public uint readBits(int numBits)
        {
            Debug.debugAssert(beginEndBits == 1);
            uint output = 0;
            int total = numBits;
            while (numBits > 0)
            {
                if (bitPos > 7)
                {
                    bitPos = 0;
                    bitString = readUInt8();
                }

                output |= (bitString & 1) << (total - numBits);
                bitString >>= 1;
                ++bitPos;
                --numBits;
            }
            return output;
        }

        public void endBits()
        {
            Debug.debugAssert(beginEndBits == 1);
            if (bitPos == 0)
            {
                --pos;
            }
            beginEndBits = 0;
            bitPos = 0;
        }

        public Vector2 readVector2()
        {
            double x = readFloat32();
            double y = readFloat32();
            return new Vector2((float)x, (float)y);
        }

        public Vector3 readVector3()
        {
            double x = readFloat32();
            double y = readFloat32();
            double z = readFloat32();
            return new Vector3((float)x, (float)y, (float)z);
        }

        public Vector4 readVector4()
        {
            double x = readFloat32();
            double y = readFloat32();
            double z = readFloat32();
            double w = readFloat32();
            return new Vector4((float)x, (float)y, (float)z, (float)w);
        }

        public Color3 readColor3()
        {
            double r = readFloat32();
            double g = readFloat32();
            double b = readFloat32();
            return new Color3((float)r, (float)g, (float)b);
        }

        public Color4 readColor4()
        {
            double r = readFloat32();
            double g = readFloat32();
            double b = readFloat32();
            double a = readFloat32();
            return new Color4((float)r, (float)g, (float)b, (float)a);
        }
    }
}
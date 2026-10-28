/**
 @file BinaryOutput.h

 @maintainer Morgan McGuire, graphics3d.com

 @created 2001-08-09
 @edited  2006-01-24

 Copyright 2000-2006, Morgan McGuire.
 All rights reserved.
 */

using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace G3D
{
    public class BinaryOutput
    {
        private string filename;
        private bool committed;
        private int beginEndBits;
        private int bitString;
        private int bitPos;
        private bool swapBytes;
        private byte[] buffer;
        private int bufferLen;
        private int maxBufferLen;
        private int pos;
        /** is this initialized? */
        public bool init;
        private int alreadyWritten;

        public BinaryOutput()
        {
            alreadyWritten = 0;
            swapBytes = false;
            pos = 0;
            filename = "<memory>";
            buffer = null;
            bufferLen = 0;
            maxBufferLen = 0;
            beginEndBits = 0;
            bitString = 0;
            bitPos = 0;
            committed = false;
            init = true;
        }

        public BinaryOutput(string filename, G3DEndian fileEndian)
        {
            pos = 0;
            alreadyWritten = 0;
            setEndian(fileEndian);
            this.filename = filename;
            buffer = null;
            bufferLen = 0;
            maxBufferLen = 0;
            beginEndBits = 0;
            bitString = 0;
            bitPos = 0;
            committed = false;
            init = true;
        }

        public void setEndian(G3DEndian fileEndian)
        {
            swapBytes = (fileEndian != (BitConverter.IsLittleEndian ? G3DEndian.G3D_LITTLE_ENDIAN : G3DEndian.G3D_BIG_ENDIAN));
        }

        public string getFilename()
        {
            return filename;
        }

        public byte[] getCArray()
        {
            return buffer;
        }

        private void reserveBytes(int bytes)
        {
            Debug.debugAssert(bytes > 0);
            int oldBufferLen = bufferLen;
            bufferLen = Math.Max(bufferLen, pos + bytes);
            if (bufferLen > maxBufferLen)
            {
                reallocBuffer(bytes, oldBufferLen);
            }
        }

        private void reallocBuffer(int bytes, int oldBufferLen)
        {
            int newBufferLen = (int)(bufferLen * 1.5) + 100;
            if (newBufferLen < bufferLen)
            {
                newBufferLen = bufferLen;
            }

            byte[] newBuffer = null;
            if (filename == "<memory>" || newBufferLen < 400000000)
            {
                newBuffer = new byte[newBufferLen];
                if (buffer != null)
                {
                    Array.Copy(buffer, newBuffer, Math.Min(buffer.Length, oldBufferLen));
                }
                maxBufferLen = newBufferLen;
            }

            if (newBuffer == null && bytes > 0)
            {
                bufferLen = oldBufferLen;
                reserveBytesWhenOutOfMemory(bytes);
            }
            else
            {
                buffer = newBuffer;
                Debug.debugAssert(buffer != null);
            }
        }

        private void reserveBytesWhenOutOfMemory(int bytes)
        {
            if (filename == "<memory>")
            {
                throw new InvalidOperationException("Out of memory while writing to memory in BinaryOutput (no RAM left).");
            }
            if (bytes > maxBufferLen)
            {
                throw new InvalidOperationException("Out of memory while writing to disk in BinaryOutput (could not create a large enough buffer).");
            }

            int writeBytes = bufferLen - 10 * 1024 * 1024;
            if (writeBytes < bufferLen / 3)
            {
                writeBytes = bufferLen;
            }
            Debug.debugAssert(writeBytes > 0);

            string mode = (alreadyWritten > 0) ? "ab" : "wb";
            using (FileStream file = new FileStream(filename, alreadyWritten > 0 ? FileMode.Append : FileMode.Create))
            {
                file.Write(buffer, 0, writeBytes);
            }

            alreadyWritten += writeBytes;
            bufferLen -= writeBytes;
            pos -= writeBytes;
            Debug.debugAssert(bufferLen < maxBufferLen);
            Debug.debugAssert(bufferLen >= 0);
            Debug.debugAssert(pos >= 0);
            Debug.debugAssert(pos <= bufferLen);

            Array.Copy(buffer, writeBytes, buffer, 0, bufferLen);
            reserveBytes(bytes);
        }

        public void compress()
        {
            if (alreadyWritten > 0)
            {
                throw new InvalidOperationException("Cannot compress huge files (part of this file has already been written to disk).");
            }

            int uncompressedLength = bufferLen;
            uint adler = Adler32(buffer, 0, uncompressedLength);
            using (MemoryStream raw = new MemoryStream())
            {
                using (DeflateStream deflate = new DeflateStream(raw, CompressionMode.Compress, true))
                {
                    deflate.Write(buffer, 0, bufferLen);
                }

                byte[] rawDeflate = raw.ToArray();
                int totalLength = 4 + 2 + rawDeflate.Length + 4;
                if (totalLength > maxBufferLen || buffer == null)
                {
                    buffer = new byte[totalLength];
                    maxBufferLen = totalLength;
                }

                // Uncompressed size header.
                if (swapBytes)
                {
                    buffer[0] = (byte)((uncompressedLength >> 24) & 0xFF);
                    buffer[1] = (byte)((uncompressedLength >> 16) & 0xFF);
                    buffer[2] = (byte)((uncompressedLength >> 8) & 0xFF);
                    buffer[3] = (byte)(uncompressedLength & 0xFF);
                }
                else
                {
                    buffer[0] = (byte)(uncompressedLength & 0xFF);
                    buffer[1] = (byte)((uncompressedLength >> 8) & 0xFF);
                    buffer[2] = (byte)((uncompressedLength >> 16) & 0xFF);
                    buffer[3] = (byte)((uncompressedLength >> 24) & 0xFF);
                }

                // zlib header.
                buffer[4] = 0x78;
                buffer[5] = 0x9C;
                Array.Copy(rawDeflate, 0, buffer, 6, rawDeflate.Length);

                // Oops: buffer was overwritten. We lost original data. Need compute adler before.
                buffer[6 + rawDeflate.Length] = (byte)((adler >> 24) & 0xFF);
                buffer[6 + rawDeflate.Length + 1] = (byte)((adler >> 16) & 0xFF);
                buffer[6 + rawDeflate.Length + 2] = (byte)((adler >> 8) & 0xFF);
                buffer[6 + rawDeflate.Length + 3] = (byte)(adler & 0xFF);

                bufferLen = totalLength;
                pos = bufferLen;
            }
        }

        private static uint Adler32(byte[] data, int offset, int length)
        {
            uint a = 1;
            uint b = 0;
            for (int i = 0; i < length; ++i)
            {
                a = (a + data[offset + i]) % 65521;
                b = (b + a) % 65521;
            }
            return (b << 16) | a;
        }

        public void commit(bool flush = true)
        {
            if (committed)
            {
                throw new InvalidOperationException("Cannot commit twice");
            }
            committed = true;
            Debug.debugAssert(beginEndBits == 0);

            if (filename != "<memory>")
            {
                string root = "";
                Array<string> pathArray = new Array<string>();
                string baseName = "";
                string ext = "";
                FileUtils.parseFilename(filename, out root, pathArray, out baseName, out ext);
                string path = root + StringUtils.stringJoin(pathArray, '/');
                if (!FileUtils.fileExists(path))
                {
                    FileUtils.createDirectory(path);
                }
            }

            string mode = (alreadyWritten > 0) ? "ab" : "wb";
            using (FileStream file = new FileStream(filename, alreadyWritten > 0 ? FileMode.Append : FileMode.Create))
            {
                file.Write(buffer, 0, bufferLen);
                if (flush)
                {
                    file.Flush();
                }
            }
            alreadyWritten += bufferLen;
        }

        public void commit(byte[] output)
        {
            if (committed)
            {
                throw new InvalidOperationException("Cannot commit twice");
            }
            committed = true;
            Array.Copy(buffer, output, bufferLen);
        }

        public void reset()
        {
            Debug.debugAssert(beginEndBits == 0);
            pos = 0;
            alreadyWritten = 0;
            bufferLen = 0;
            beginEndBits = 0;
            bitString = 0;
            bitPos = 0;
            committed = false;
        }

        public int length()
        {
            return bufferLen + alreadyWritten;
        }

        public int getLength()
        {
            return length();
        }

        public int size()
        {
            return length();
        }

        public void setLength(int n)
        {
            n = n - alreadyWritten;
            if (n < 0)
            {
                throw new InvalidOperationException("Cannot resize huge files to be shorter.");
            }
            if (n < bufferLen)
            {
                pos = n;
            }
            if (n > bufferLen)
            {
                reserveBytes(n - bufferLen);
            }
        }

        public int position()
        {
            return pos + alreadyWritten;
        }

        public int getPosition()
        {
            return position();
        }

        public void setPosition(int p)
        {
            p = p - alreadyWritten;
            if (p > bufferLen)
            {
                setLength(p + alreadyWritten);
            }
            if (p < 0)
            {
                throw new InvalidOperationException("Cannot seek more than 10 MB backwards on huge files.");
            }
            pos = p;
        }

        public void writeBytes(byte[] b, int count)
        {
            writeBytes(b, 0, count);
        }

        public void writeBytes(byte[] b, int offset, int count)
        {
            reserveBytes(count);
            Debug.debugAssert(pos >= 0);
            Debug.debugAssert(bufferLen >= count);
            Array.Copy(b, offset, buffer, pos, count);
            pos += count;
        }

        public void writeInt8(sbyte i)
        {
            reserveBytes(1);
            buffer[pos] = (byte)i;
            ++pos;
        }

        public void writeBool8(bool b)
        {
            writeInt8(b ? (sbyte)1 : (sbyte)0);
        }

        public void writeUInt8(byte i)
        {
            reserveBytes(1);
            buffer[pos] = i;
            ++pos;
        }

        public void writeUInt16(ushort u)
        {
            reserveBytes(2);
            byte[] bytes = BitConverter.GetBytes(u);
            if (swapBytes)
            {
                buffer[pos] = bytes[1];
                buffer[pos + 1] = bytes[0];
            }
            else
            {
                buffer[pos] = bytes[0];
                buffer[pos + 1] = bytes[1];
            }
            pos += 2;
        }

        public void writeInt16(short i)
        {
            writeUInt16((ushort)i);
        }

        public void writeUInt32(uint u)
        {
            reserveBytes(4);
            byte[] bytes = BitConverter.GetBytes(u);
            Debug.debugAssert(beginEndBits == 0);
            if (swapBytes)
            {
                buffer[pos] = bytes[3];
                buffer[pos + 1] = bytes[2];
                buffer[pos + 2] = bytes[1];
                buffer[pos + 3] = bytes[0];
            }
            else
            {
                buffer[pos] = bytes[0];
                buffer[pos + 1] = bytes[1];
                buffer[pos + 2] = bytes[2];
                buffer[pos + 3] = bytes[3];
            }
            pos += 4;
        }

        public void writeInt32(int i)
        {
            writeUInt32((uint)i);
        }

        public void writeUInt64(ulong u)
        {
            reserveBytes(8);
            byte[] bytes = BitConverter.GetBytes(u);
            if (swapBytes)
            {
                for (int i = 0; i < 8; ++i)
                {
                    buffer[pos + i] = bytes[7 - i];
                }
            }
            else
            {
                Array.Copy(bytes, 0, buffer, pos, 8);
            }
            pos += 8;
        }

        public void writeInt64(long i)
        {
            writeUInt64((ulong)i);
        }

        public void writeFloat32(float f)
        {
            Debug.debugAssert(beginEndBits == 0);
            writeUInt32(BitConverter.ToUInt32(BitConverter.GetBytes(f), 0));
        }

        public void writeFloat64(double f)
        {
            writeUInt64(BitConverter.ToUInt64(BitConverter.GetBytes(f), 0));
        }

        public void writeString(string s)
        {
            byte[] bytes = Encoding.Default.GetBytes(s);
            writeBytes(bytes, bytes.Length);
            writeUInt8(0);
        }

        public void writeStringEven(string s)
        {
            byte[] bytes = Encoding.Default.GetBytes(s);
            writeBytes(bytes, bytes.Length);
            writeUInt8(0);
            if ((bytes.Length + 1) % 2 == 1)
            {
                writeUInt8(0);
            }
        }

        public void writeString32(string s)
        {
            byte[] bytes = Encoding.Default.GetBytes(s);
            writeUInt32((uint)(bytes.Length + 1));
            writeString(s);
        }

        public void writeBool8(bool[] output, int n)
        {
            for (int i = 0; i < n; ++i)
            {
                writeBool8(output[i]);
            }
        }

        public void writeBool8(Array<bool> output, int n)
        {
            for (int i = 0; i < n; ++i)
            {
                writeBool8(output[i]);
            }
        }

        public void writeUInt8(byte[] output, int n)
        {
            writeBytes(output, n);
        }

        public void writeUInt8(Array<byte> output, int n)
        {
            for (int i = 0; i < n; ++i)
            {
                writeUInt8(output[i]);
            }
        }

        public void writeInt8(sbyte[] output, int n)
        {
            for (int i = 0; i < n; ++i)
            {
                writeInt8(output[i]);
            }
        }

        public void writeInt8(Array<sbyte> output, int n)
        {
            for (int i = 0; i < n; ++i)
            {
                writeInt8(output[i]);
            }
        }

        public void writeUInt16(ushort[] output, int n)
        {
            for (int i = 0; i < n; ++i)
            {
                writeUInt16(output[i]);
            }
        }

        public void writeUInt16(Array<ushort> output, int n)
        {
            for (int i = 0; i < n; ++i)
            {
                writeUInt16(output[i]);
            }
        }

        public void writeInt16(short[] output, int n)
        {
            for (int i = 0; i < n; ++i)
            {
                writeInt16(output[i]);
            }
        }

        public void writeInt16(Array<short> output, int n)
        {
            for (int i = 0; i < n; ++i)
            {
                writeInt16(output[i]);
            }
        }

        public void writeUInt32(uint[] output, int n)
        {
            for (int i = 0; i < n; ++i)
            {
                writeUInt32(output[i]);
            }
        }

        public void writeUInt32(Array<uint> output, int n)
        {
            for (int i = 0; i < n; ++i)
            {
                writeUInt32(output[i]);
            }
        }

        public void writeInt32(int[] output, int n)
        {
            for (int i = 0; i < n; ++i)
            {
                writeInt32(output[i]);
            }
        }

        public void writeInt32(Array<int> output, int n)
        {
            for (int i = 0; i < n; ++i)
            {
                writeInt32(output[i]);
            }
        }

        public void writeUInt64(ulong[] output, int n)
        {
            for (int i = 0; i < n; ++i)
            {
                writeUInt64(output[i]);
            }
        }

        public void writeUInt64(Array<ulong> output, int n)
        {
            for (int i = 0; i < n; ++i)
            {
                writeUInt64(output[i]);
            }
        }

        public void writeInt64(long[] output, int n)
        {
            for (int i = 0; i < n; ++i)
            {
                writeInt64(output[i]);
            }
        }

        public void writeInt64(Array<long> output, int n)
        {
            for (int i = 0; i < n; ++i)
            {
                writeInt64(output[i]);
            }
        }

        public void writeFloat32(float[] output, int n)
        {
            for (int i = 0; i < n; ++i)
            {
                writeFloat32(output[i]);
            }
        }

        public void writeFloat32(Array<float> output, int n)
        {
            for (int i = 0; i < n; ++i)
            {
                writeFloat32(output[i]);
            }
        }

        public void writeFloat64(double[] output, int n)
        {
            for (int i = 0; i < n; ++i)
            {
                writeFloat64(output[i]);
            }
        }

        public void writeFloat64(Array<double> output, int n)
        {
            for (int i = 0; i < n; ++i)
            {
                writeFloat64(output[i]);
            }
        }

        public void skip(int n)
        {
            if (pos + n > bufferLen)
            {
                setLength(pos + alreadyWritten + n);
            }
            pos += n;
        }

        public void beginBits()
        {
            Debug.debugAssertM(beginEndBits == 0, "Already in beginBits...endBits");
            bitString = 0x00;
            bitPos = 0;
            beginEndBits = 1;
        }

        public void writeBits(uint bitString, int numBits)
        {
            while (numBits > 0)
            {
                this.bitString |= (int)(bitString & 1) << bitPos;
                ++bitPos;
                bitString >>= 1;
                --numBits;

                if (bitPos > 7)
                {
                    writeUInt8((byte)this.bitString);
                    this.bitString = 0x00;
                    bitPos = 0;
                }
            }
        }

        public void endBits()
        {
            Debug.debugAssertM(beginEndBits == 1, "Not in beginBits...endBits");
            if (bitPos > 0)
            {
                writeUInt8((byte)bitString);
            }
            bitString = 0;
            bitPos = 0;
            beginEndBits = 0;
        }

        public void writeVector2(Vector2 v)
        {
            writeFloat32(v.x);
            writeFloat32(v.y);
        }

        public void writeVector3(Vector3 v)
        {
            writeFloat32(v.x);
            writeFloat32(v.y);
            writeFloat32(v.z);
        }

        public void writeVector4(Vector4 v)
        {
            writeFloat32(v.x);
            writeFloat32(v.y);
            writeFloat32(v.z);
            writeFloat32(v.w);
        }

        public void writeColor3(Color3 v)
        {
            writeFloat32(v.r);
            writeFloat32(v.g);
            writeFloat32(v.b);
        }

        public void writeColor4(Color4 v)
        {
            writeFloat32(v.r);
            writeFloat32(v.g);
            writeFloat32(v.b);
            writeFloat32(v.a);
        }
    }
}

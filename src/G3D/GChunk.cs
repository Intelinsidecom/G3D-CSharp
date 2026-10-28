/**
  @file GChunk.h
  @maintainer Morgan McGuire, morgan@graphics3d.com
  @created 2005-06-07
  @edited  2005-06-07
 */

using System;

namespace G3D
{
    /** 
      Helper for writing files with extensible and discoverable formats.

      <B>Writing a Chunk:</B>

      <PRE>
        BinaryOutput b(...);
        enum MyFormat {HEIGHTS, POSITION};
        Array<int32> heights;
        ...

        Chunk c(b, HEIGHTS, Chunk::INT32, heights.size());
        for (int i = 0; i < heights.size(); ++i) {
            b.writeInt32(heights[i]);
        }
        c.finish();
       </PRE>

      <B>Reading a Chunk:</B>

      <HR NOSHADE>
      Chunks have the following binary format:

      <PRE>
      uint16            name (application defined code)
      uint16            format (Chunk::BinaryFormat)
      int32             size (not including these 8 header bytes)
      format[num]       ...data...
      </PRE>

      The number of data elements is <CODE>num = size / Chunk::byteSize(format)</CODE>.
     */
    public class GChunk : IDisposable
    {
        private const int HEADER_SIZE = 8;

        /** Keeps track of whether this chunk is for reading or writing */
        private enum Mode { WRITE = 0, READ = 1, UNINITIALIZED = -1 }

        private Mode mode;
        private bool finished;

        /** Start of the header relative to the beginning of the file. Data 
            payload is HEADER_SIZE bytes later. */
        private int startPos;

        /** Application-defined code for this chunk. */
        public ushort name;

        /** BinaryFormat of the data payload. */
        public BinaryFormat format;

        /** Size of the data payload in bytes, excluding the 8 header bytes. */
        public int size;

        /** Number of elements in the data payload. -1 if unknown because the format is CUSTOM or string.*/
        public int count;

        public GChunk()
        {
            mode = Mode.UNINITIALIZED;
            finished = true;
        }

        private void readInit(BinaryInput b)
        {
            mode = Mode.READ;
            finished = false;

            startPos = b.getPosition();

            name = b.readUInt16();
            format = (BinaryFormat)b.readUInt16();

            Debug.alwaysAssertM((format > BinaryFormat.FIRST_BINFMT) && (format < BinaryFormat.LAST_BINFMT),
                "GChunk file is corrupted-- format tag is invalid.");

            size = b.readInt32();
            Debug.alwaysAssertM(size < b.getLength(),
                "GChunk file is corrupted-- chunk is larger than file!");

            if (format != BinaryFormat.CUSTOM_BINFMT)
            {
                int s = BinaryFormatUtil.byteSize(format);
                if (s != -1)
                {
                    count = size / s;
                }
                else
                {
                    count = -1;
                }
            }
            else
            {
                count = -1;
            }
        }

        /** Begin a read chunk.  After the constructor finishes
            you can access the member fields to discover the chunk
            type.  Use GChunk::finish() to jump to the end of the chunk.
          */
        public GChunk(BinaryInput b)
        {
            readInit(b);
        }

        /** Shortcut for:
          <PRE>
           GChunk c(b);
           alwaysAssertM(c.name == name);
          </PRE>
          */
        public GChunk(BinaryInput b, ushort _name)
        {
            readInit(b);
            Debug.alwaysAssertM(name == _name, "File does not match format.");
        }

        public GChunk(BinaryInput b, ushort _name, BinaryFormat _format)
        {
            readInit(b);
            Debug.alwaysAssertM(name == _name, "File does not match format.");
            Debug.alwaysAssertM(format == _format, "File does not match format.");
        }

        public GChunk(BinaryInput b, ushort _name, BinaryFormat _format, int _count)
        {
            readInit(b);
            Debug.alwaysAssertM(name == _name, "File does not match format.");
            Debug.alwaysAssertM(format == _format, "File does not match format.");
            Debug.alwaysAssertM(count == _count, "File does not match format.");
        }

        /** Begin a write chunk.  Use GChunk::finish() when done.*/
        public GChunk(BinaryOutput b, ushort name, BinaryFormat format = BinaryFormat.CUSTOM_BINFMT)
        {
            startPos = b.getPosition();
            this.name = name;
            this.format = format;
            // count will be default 0
            finished = false;
            mode = Mode.WRITE;

            b.writeUInt16(name);
            b.writeUInt16((ushort)format);

            // Leave space for the payload size
            b.skip(4);
        }

        ~GChunk()
        {
            Debug.alwaysAssertM(finished, "GChunk went out of scope or destroyed without being finished or aborted.");
        }

        /** Stop writing to a chunk abruptly.  Avoids the assertion failure that will
            occur in the destructor if a chunk is destroyed without being finished. */
        public void abort()
        {
            Debug.alwaysAssertM(!finished, "Aborted a chunk that was already finished.");
            finished = true;
        }

        /** End writing to a chunk and write the header. */
        public void finish(BinaryInput b)
        {
            Debug.alwaysAssertM(mode == Mode.READ, "Called finish with a binary input on a write chunk.");
            Debug.alwaysAssertM(!finished, "Called finish on the same chunk twice.");

            b.setPosition(startPos + size + HEADER_SIZE);

            finished = true;
        }

        // Not called commit because we perform it on input as well
        /** End writing to the chunk and write the header. Only call when at the end of the chunk.
            Leaves the file pointer at the end of the chunk. */
        public void finish(BinaryOutput b)
        {
            Debug.alwaysAssertM(mode == Mode.WRITE, "Called finish with a binary output on a read chunk.");
            Debug.alwaysAssertM(!finished, "Called finish on the same chunk twice.");

            // Write the size into the header
            int currentPos = b.position();
            b.setPosition(startPos + HEADER_SIZE - 4);
            b.writeInt32(currentPos - startPos - HEADER_SIZE);
            b.setPosition(currentPos);

            finished = true;
        }

        public void Dispose()
        {
            if (!finished)
            {
                abort();
            }
            GC.SuppressFinalize(this);
        }
    }
}
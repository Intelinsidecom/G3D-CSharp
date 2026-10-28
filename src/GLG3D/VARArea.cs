/**
  @file VARArea.h
  @maintainer Morgan McGuire, matrix@graphics3d.com
  @created 2003-08-09
  @edited  2006-02-06
*/

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace G3D
{
    /**
     Wrapper for OpenGL Vertex Buffer Object
     http://oss.sgi.com/projects/ogl-sample/registry/ARB/vertex_buffer_object.txt
     http://developer.nvidia.com/docs/IO/8230/GDC2003_OGL_BufferObjects.ppt

     Allocate a VARArea, then allocate VARs within it.  VARAreas are garbage
     collected.  When no pointers remain to VARs inside it or the VARArea itself,
     it will automatically be reclaimed by the system.

     You cannot mix pointers from different VARAreas when rendering.  For
     example, if the vertex VAR is in one VARArea, the normal VAR and color
     VAR must come from the same area.


     You can't find out how much space is left for VARAreas in video memory,
     except by checking the VARArea::create value and seeing if it is NULL.
     */
    public sealed class VARArea : ReferenceCountedObject
    {
        /**
         These values are <B>hints</B>. Your program will work correctly
         regardless of which you use, but using the appropriate value
         lets the renderer optimize for your useage patterns and can
         increase performance.

         Use WRITE_EVERY_FRAME if you write <I>at least</I> once per frame
         (e.g. software animation).

         Use WRITE_EVERY_FEW_FRAMES if you write to the area as part of
         the rendering loop, but not every frame (e.g. impostors, deformable
         data).

         Use WRITE_ONCE if you do not write to the area inside the rendering
         loop (e.g. rigid bodies loaded once at the beginning of a game level).  
         This does not mean you can't write multiple times
         to the area, just that writing might be very slow compared to rendering.

         Correspond to OpenGL hints: 
          WRITE_ONCE : GL_STATIC_DRAW_ARB
          WRITE_EVERY_FRAME : GL_STREAM_DRAW_ARB
          WRITE_EVERY_FEW_FRAMEs : DYNAMIC_DRAW_ARB
         */
        public enum UsageHint
        {
            WRITE_ONCE,
            WRITE_EVERY_FEW_FRAMES,
            WRITE_EVERY_FRAME
        }

        internal enum Mode
        {
            UNINITIALIZED,
            VBO_MEMORY,
            MAIN_MEMORY
        }

        internal const int ARRAY_BUFFER = 0x8892;

        internal sealed class AreaArray
        {
            public VAR Variable;
            public long Offset;
            public long Length;
            public bool InUse;
        }

        private sealed class Block
        {
            public long Offset;
            public long Length;
            public long Reserved;
            public VAR Variable;
        }

        private struct Hole
        {
            public long Start;
            public long End;

            public Hole(long start, long end)
            {
                Start = start;
                End = end;
            }
        }

        /**
         The milestone is used for finish().  It is created
         by RenderDevice::setVARAreaMilestones.  If NULL, there
         is no milestone pending.
         */
        private Milestone milestone;

        /** Number of bytes currently allocated out of size total. */
        private long allocated;

        /**
         This count prevents vertex arrays that have been freed from
         accidentally being used-- it is incremented every time
         the VARArea is reset.
         */
        private ulong generation;

        /** The maximum size of this area that was ever used. */
        private long peakAllocated;

        /** Set by RenderDevice */
        private RenderDevice renderDevice;

        /** Total  number of bytes in this area.  May be zero if resources have been freed.*/
        private long size;

        /**
         The OpenGL buffer object associated with this area
         (only used when mode == VBO_MEMORY)
         */
        private uint glbuffer;

        /** Pointer to the memory (NULL when
            the VBO extension is not present). */
        private byte[] basePointer;

        private static Mode mode = Mode.UNINITIALIZED;

        private static long _sizeOfAllVARAreasInMemory;

        private static List<WeakReference> allVARAreas = new List<WeakReference>();

        private readonly Dictionary<VAR.Type, AreaArray> arrays = new Dictionary<VAR.Type, AreaArray>();

        private readonly List<Block> blocks = new List<Block>();

        private UsageHint usageHint;

        private bool begun;

        private bool released;

        private VARArea(long _size, UsageHint hint)
        {
            size = _size < 0 ? 0 : _size;
            usageHint = hint;
            renderDevice = null;
            milestone = null;
            allocated = 0;
            generation = 1;
            peakAllocated = 0;
            glbuffer = 0;
            basePointer = size > 0 ? new byte[size] : null;
            begun = false;
            released = false;

            if (mode == Mode.UNINITIALIZED)
            {
                IGraphicsBackend backend = GLCaps.getBackendInstance();
                if (backend != null && backend.SupportsVertexBufferObject() && !backend.HasBug_slowVBO())
                {
                    mode = Mode.VBO_MEMORY;
                }
                else
                {
                    mode = Mode.MAIN_MEMORY;
                }
            }

            _sizeOfAllVARAreasInMemory += size;

            if (mode == Mode.VBO_MEMORY)
            {
                IGraphicsBackend backend = GLCaps.getBackendInstance();
                if (backend != null && size > 0)
                {
                    glbuffer = (uint)backend.CreateBuffer();
                    backend.BindBuffer(ARRAY_BUFFER, (int)glbuffer);
                    backend.BufferData(ARRAY_BUFFER, (int)size, null, usageOf(usageHint));
                    backend.BindBuffer(ARRAY_BUFFER, 0);
                }
            }
        }

        public static VARArea create(long s)
        {
            return create(s, UsageHint.WRITE_EVERY_FRAME);
        }

        public static VARArea create(long s, UsageHint h)
        {
            cleanCache();
            VARArea x = new VARArea(s, h);
            allVARAreas.Add(new WeakReference(x));
            return x;
        }

        public void begin()
        {
            if (begun)
            {
                throw new InvalidOperationException("The VARArea is already open.");
            }
            begun = true;
        }

        public void end()
        {
            if (!begun)
            {
                throw new InvalidOperationException("The VARArea is not open.");
            }
            begun = false;
        }

        /**
         Blocks the CPU until all rendering calls referencing 
         this area have completed.
         */
        public void finish()
        {
            if (renderDevice == null)
            {
                return;
            }

            if (GLCaps.getBackendInstance() == null)
            {
                return;
            }

            if (milestone == null)
            {
                milestone = renderDevice.createMilestone("VAR Milestone");
            }

            milestone.setRenderDevice(renderDevice);
            milestone.set();
            milestone.wait();
        }

        /** Finishes, then frees all VAR memory inside this area.*/
        public void reset()
        {
            finish();
            ++generation;
            allocated = 0;
            blocks.Clear();

            foreach (KeyValuePair<VAR.Type, AreaArray> pair in arrays)
            {
                pair.Value.InUse = false;
                pair.Value.Variable.detach();
            }

            if (basePointer != null)
            {
                Array.Clear(basePointer, 0, basePointer.Length);
            }
        }

        public long totalSize()
        {
            return size;
        }

        public long freeSize()
        {
            return size - allocated;
        }

        public long allocatedSize()
        {
            return allocated;
        }

        public long peakAllocatedSize()
        {
            return peakAllocated;
        }

        public ulong currentGeneration()
        {
            return generation;
        }

        public UsageHint getUsageHint()
        {
            return usageHint;
        }

        /**
         Provided for breaking the VARArea abstraction; use G3D::VAR and 
         G3D::RenderDevice in general.

         When using the OpenGL vertex buffer API, this is the underlying 
         OpenGL vertex buffer object.  It is zero when using system memory.
         The caller cannot control whether VBO is used or not; G3D selects
         the best method automatically.
         */
        public uint gl_vertexBufferObject()
        {
            return glbuffer;
        }

        /**
         Provided for breaking the VARArea abstraction; use G3D::VAR and 
         G3D::RenderDevice in general.

         When using system memory, this is a pointer to the beginning of 
         the system memory block in which data is stored.  Null when using VBO.
         */
        public byte[] gl_basePointer()
        {
            if (mode == Mode.VBO_MEMORY)
            {
                return null;
            }
            return basePointer;
        }

        public void setRenderDevice(RenderDevice device)
        {
            renderDevice = device;
        }

        public RenderDevice getRenderDevice()
        {
            return renderDevice;
        }

        /** Returns the total size of all VARAreas allocated.  Note that not all
            will be in video memory, and some will be backed by main memory 
            even if nominally stored in video memory, so the total size may
            exceed the video memory size.*/
        public static long sizeOfAllVARAreasInMemory()
        {
            return _sizeOfAllVARAreasInMemory;
        }

        /** Releases all VARAreas. Called before shutdown by RenderDevice. */
        public static void cleanupAllVARAreas()
        {
            for (int i = 0; i < allVARAreas.Count; ++i)
            {
                VARArea area = allVARAreas[i].Target as VARArea;
                if (area != null && !area.released)
                {
                    area.reset();
                    area.releaseResources(true);
                }
            }
            allVARAreas.Clear();
            _sizeOfAllVARAreasInMemory = 0;
        }

        /**
         Finds the offset of the first free block of memory that is at least
         size bytes long.  Returns -1 when no such block exists.
         */
        public long find(long size)
        {
            if (size < 0)
            {
                throw new ArgumentOutOfRangeException("size");
            }
            long reserved;
            return findFreeBlock(size, out reserved);
        }

        /**
         Finds the offset of the first free block of memory that is at least
         size bytes long and that is separated from the end of the area by at
         least one allocated block.  Returns -1 when no such block exists.
         */
        public long findSeparate(long size)
        {
            if (size < 0)
            {
                throw new ArgumentOutOfRangeException("size");
            }
            List<Hole> holes = getHoles();
            for (int i = 0; i < holes.Count - 1; ++i)
            {
                long start = alignOffset(holes[i].Start);
                if (start + size <= holes[i].End)
                {
                    return start;
                }
            }
            return -1;
        }

        /**
         Replaces the contents of a VAR that belongs to this area.  The
         replacement must not be larger than the memory allocated to the VAR.
         */
        public void replace(VAR variable, byte[] contents)
        {
            if (variable == null)
            {
                throw new ArgumentNullException("variable");
            }
            if (contents == null)
            {
                throw new ArgumentNullException("contents");
            }

            Block block = findBlock(variable);
            if (block == null)
            {
                throw new InvalidOperationException("The VAR does not belong to this VARArea.");
            }
            if (contents.Length > block.Length)
            {
                throw new ArgumentException("The replacement data is larger than the memory allocated to the VAR.", "contents");
            }

            writeAt(block.Offset, contents, 0, contents.Length);
            variable.replaceContents(contents.Length);
        }

        /**
         Frees the memory allocated to a VAR that belongs to this area.  The
         VAR cannot be used for rendering after it has been removed.
         */
        public bool remove(VAR variable)
        {
            if (variable == null)
            {
                throw new ArgumentNullException("variable");
            }

            Block block = findBlock(variable);
            if (block == null)
            {
                return false;
            }

            blocks.Remove(block);
            allocated -= block.Reserved;
            if (allocated < 0)
            {
                allocated = 0;
            }
            variable.detach();
            return true;
        }

        /**
         Changes the amount of memory allocated to a VAR that belongs to this
         area, preserving the contents that still fit.  The VAR is returned.
         */
        public VAR reallocate(VAR variable, long newLength)
        {
            if (variable == null)
            {
                throw new ArgumentNullException("variable");
            }
            if (newLength < 0)
            {
                throw new ArgumentOutOfRangeException("newLength");
            }

            Block block = findBlock(variable);
            if (block == null)
            {
                throw new InvalidOperationException("The VAR does not belong to this VARArea.");
            }
            if (newLength == block.Length)
            {
                return variable;
            }

            long oldOffset = block.Offset;
            long oldLength = block.Length;
            byte[] oldData = readAt(oldOffset, (int)Math.Min(oldLength, int.MaxValue));

            blocks.Remove(block);
            allocated -= block.Reserved;

            long reserved;
            long newOffset = findFreeBlock(newLength, out reserved);
            if (newOffset < 0)
            {
                block.Offset = oldOffset;
                block.Length = oldLength;
                blocks.Add(block);
                allocated += block.Reserved;
                throw new InvalidOperationException("VARArea too small to hold new VAR (possibly due to rounding to 8-byte boundaries).");
            }

            Block newBlock = new Block();
            newBlock.Offset = newOffset;
            newBlock.Length = newLength;
            newBlock.Reserved = reserved;
            newBlock.Variable = variable;
            blocks.Add(newBlock);
            updateAllocation(reserved);

            if (newLength > 0)
            {
                int copyCount = (int)Math.Min(oldLength, newLength);
                writeAt(newOffset, oldData, 0, copyCount);
            }

            variable.setBlockOffset(newOffset);
            variable.setMaxSize(newLength);
            variable.replaceContents((int)Math.Min(oldLength, newLength));
            return variable;
        }

        public VAR getVertexArray()
        {
            return getOrCreate(VAR.Type.VERTEX, capacityShare()).Variable;
        }

        public VAR getNormalArray()
        {
            return getOrCreate(VAR.Type.NORMAL, capacityShare()).Variable;
        }

        public VAR getColorArray()
        {
            return getOrCreate(VAR.Type.COLOR, capacityShare()).Variable;
        }

        public VAR getTexCoordArray(int unit)
        {
            if (unit < 0 || unit > 7)
            {
                throw new ArgumentOutOfRangeException("unit");
            }
            return getOrCreate((VAR.Type)(VAR.Type.TEXCOORD0 + unit), capacityShare()).Variable;
        }

        public VAR getIndexArray()
        {
            return getOrCreate(VAR.Type.INDEX, capacityShare()).Variable;
        }

        public VAR getGenericArray(int index)
        {
            if (index < 0 || index > 15)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            return getOrCreate((VAR.Type)(VAR.Type.GENERIC0 + index), capacityShare()).Variable;
        }

        public int getMaxVertices()
        {
            return (int)(size / 48);
        }

        internal AreaArray getOrCreate(VAR.Type type, long bytes)
        {
            AreaArray array;
            if (!arrays.TryGetValue(type, out array))
            {
                array = new AreaArray();
                array.Variable = VAR.create(type, 0, usageOf(usageHint));
                array.Variable.attachToArea(this, generation, 0, 0);
                array.Offset = 0;
                array.Length = 0;
                array.InUse = false;
                arrays.Add(type, array);
            }
            if (array.InUse)
            {
                throw new InvalidOperationException("The requested VAR array is already in use.");
            }

            long length = bytes;
            if (length <= 0)
            {
                length = array.Variable.getElementSize();
            }

            long reserved;
            long offset = findFreeBlock(length, out reserved);
            if (offset < 0)
            {
                int elementSize = array.Variable.getElementSize();
                length = (freeSize() / (elementSize > 0 ? elementSize : 1)) * (elementSize > 0 ? elementSize : 1);
                if (length <= 0)
                {
                    throw new InvalidOperationException("The VARArea does not have enough free memory for the requested array.");
                }
                offset = findFreeBlock(length, out reserved);
                if (offset < 0)
                {
                    throw new InvalidOperationException("The VARArea does not have enough free memory for the requested array.");
                }
            }

            Block block = new Block();
            block.Offset = offset;
            block.Length = length;
            block.Reserved = reserved;
            block.Variable = array.Variable;
            blocks.Add(block);
            updateAllocation(reserved);

            array.Offset = offset;
            array.Length = length;
            array.InUse = true;
            array.Variable.attachToArea(this, generation, offset, length);
            return array;
        }

        internal long allocate(VAR variable, long length)
        {
            if (length <= 0)
            {
                return allocated;
            }

            long reserved;
            long offset = findFreeBlock(length, out reserved);
            if (offset < 0)
            {
                throw new InvalidOperationException("VARArea too small to hold new VAR (possibly due to rounding to 8-byte boundaries).");
            }

            Block block = new Block();
            block.Offset = offset;
            block.Length = length;
            block.Reserved = reserved;
            block.Variable = variable;
            blocks.Add(block);
            updateAllocation(reserved);
            return offset;
        }

        internal bool owns(VAR variable)
        {
            return findBlock(variable) != null;
        }

        internal void writeAt(long offset, byte[] source, int sourceOffset, int count)
        {
            if (basePointer == null)
            {
                throw new InvalidOperationException("The VARArea has been released.");
            }
            if (offset < 0 || count < 0 || offset + count > basePointer.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            Array.Copy(source, sourceOffset, basePointer, (int)offset, count);
            uploadRegion(offset, count);
        }

        internal byte[] readAt(long offset, int count)
        {
            byte[] result = new byte[count < 0 ? 0 : count];
            if (basePointer != null && count > 0)
            {
                Array.Copy(basePointer, (int)offset, result, 0, count);
            }
            return result;
        }

        internal VAR.Usage getUsage()
        {
            return usageOf(usageHint);
        }

        internal static Mode getMode()
        {
            return mode;
        }

        internal static VAR.Usage usageOf(UsageHint hint)
        {
            switch (hint)
            {
                case UsageHint.WRITE_ONCE:
                    return VAR.Usage.STATIC;
                case UsageHint.WRITE_EVERY_FEW_FRAMES:
                    return VAR.Usage.DYNAMIC;
                case UsageHint.WRITE_EVERY_FRAME:
                    return VAR.Usage.STREAM;
                default:
                    return VAR.Usage.STREAM;
            }
        }

        /** Updates allocation and peakAllocation based off of new allocation. */
        private void updateAllocation(long newAllocation)
        {
            allocated += newAllocation;
            if (allocated > peakAllocated)
            {
                peakAllocated = allocated;
            }
        }

        private long capacityShare()
        {
            return size / 4;
        }

        private void uploadRegion(long offset, int count)
        {
            if (mode != Mode.VBO_MEMORY || glbuffer == 0 || count <= 0)
            {
                return;
            }

            IGraphicsBackend backend = GLCaps.getBackendInstance();
            if (backend == null)
            {
                return;
            }

            byte[] chunk = readAt(offset, count);
            backend.BindBuffer(ARRAY_BUFFER, (int)glbuffer);
            backend.BufferSubData(ARRAY_BUFFER, (int)offset, count, chunk);
            backend.BindBuffer(ARRAY_BUFFER, 0);
        }

        private Block findBlock(VAR variable)
        {
            for (int i = 0; i < blocks.Count; ++i)
            {
                if (blocks[i].Variable == variable)
                {
                    return blocks[i];
                }
            }
            return null;
        }

        private long findFreeBlock(long length, out long reserved)
        {
            reserved = 0;
            List<Hole> holes = getHoles();
            for (int i = 0; i < holes.Count; ++i)
            {
                long start = alignOffset(holes[i].Start);
                if (start + length <= holes[i].End)
                {
                    reserved = (start + length) - holes[i].Start;
                    return start;
                }
            }
            return -1;
        }

        private List<Hole> getHoles()
        {
            List<Hole> holes = new List<Hole>();
            List<Block> sorted = new List<Block>(blocks);
            sorted.Sort(delegate(Block a, Block b) { return a.Offset.CompareTo(b.Offset); });

            long at = 0;
            for (int i = 0; i < sorted.Count; ++i)
            {
                if (sorted[i].Offset > at)
                {
                    holes.Add(new Hole(at, sorted[i].Offset));
                }
                at = sorted[i].Offset + sorted[i].Reserved;
            }
            if (at < size)
            {
                holes.Add(new Hole(at, size));
            }
            return holes;
        }

        private static long alignOffset(long offset)
        {
            return (offset + 7) & ~7L;
        }

        /** Removes elements of allVARAreas that are not externally referenced.
            Called whenever a new VARArea is created.*/
        private static void cleanCache()
        {
            for (int i = allVARAreas.Count - 1; i >= 0; --i)
            {
                if (allVARAreas[i].Target == null)
                {
                    allVARAreas.RemoveAt(i);
                }
            }
        }

        private void releaseResources(bool deleteBackendBuffer)
        {
            if (released)
            {
                return;
            }
            released = true;

            _sizeOfAllVARAreasInMemory -= size;
            if (_sizeOfAllVARAreasInMemory < 0)
            {
                _sizeOfAllVARAreasInMemory = 0;
            }

            size = 0;
            allocated = 0;
            blocks.Clear();

            if (deleteBackendBuffer && mode == Mode.VBO_MEMORY && glbuffer != 0)
            {
                IGraphicsBackend backend = GLCaps.getBackendInstance();
                if (backend != null)
                {
                    backend.DeleteBuffer((int)glbuffer);
                }
            }
            glbuffer = 0;
            basePointer = null;
        }

        public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                releaseResources(true);
            }
            else
            {
                releaseResources(false);
            }
            base.Dispose(disposing);
        }
    }
}

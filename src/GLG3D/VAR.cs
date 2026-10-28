/**
  @file VAR.h

  @maintainer Morgan McGuire, morgan@graphics3d.com
  @created 2001-05-29
  @edited  2004-01-06
*/

using System;
using System.Runtime.InteropServices;

namespace G3D
{
    /**
     A pointer to an array of vertices, colors, or normals in video memory.
     It is safe to copy these (the pointer will be copied, not the video 
     memory).  
  
     There is no destructor because the memory referenced is freed when
     the parent VARArea is reset or freed.
     */
    public sealed class VAR : ReferenceCountedObject
    {
        public enum Type
        {
            VERTEX = 0,
            NORMAL = 1,
            COLOR = 2,
            TEXCOORD0 = 3,
            TEXCOORD1 = 4,
            TEXCOORD2 = 5,
            TEXCOORD3 = 6,
            TEXCOORD4 = 7,
            TEXCOORD5 = 8,
            TEXCOORD6 = 9,
            TEXCOORD7 = 10,
            INDEX = 11,
            GENERIC0 = 12,
            GENERIC1 = 13,
            GENERIC2 = 14,
            GENERIC3 = 15,
            GENERIC4 = 16,
            GENERIC5 = 17,
            GENERIC6 = 18,
            GENERIC7 = 19,
            GENERIC8 = 20,
            GENERIC9 = 21,
            GENERIC10 = 22,
            GENERIC11 = 23,
            GENERIC12 = 24,
            GENERIC13 = 25,
            GENERIC14 = 26,
            GENERIC15 = 27
        }

        public enum Usage
        {
            STATIC = 0,
            DYNAMIC = 1,
            STREAM = 2
        }

        public enum Format
        {
            NONE = 0,
            BYTE = 1,
            UNSIGNED_BYTE = 2,
            SHORT = 3,
            UNSIGNED_SHORT = 4,
            INT = 5,
            UNSIGNED_INT = 6,
            FLOAT = 7,
            DOUBLE = 8
        }

        private VARArea area;
        /** Size of one element */
        private int elementSize;

        private int numElements;

        private ulong generation;

        private Format underlyingRepresentation;

        /**
         The initial size this VAR was allocated with.
         */
        private long _maxSize;

        /** For VBO_MEMORY, this is the offset.  For
            MAIN_MEMORY, this is a pointer to the block
            of uploaded memory */
        private long baseOffset;
        private bool detached;
        private Type type;
        private Usage usage;
        private object backendHandle;
        private byte[] data;
        private int dataVersion;

        /** Creates an invalid VAR */
        public VAR()
        {
            area = null;
            elementSize = 0;
            numElements = 0;
            generation = 0;
            underlyingRepresentation = Format.FLOAT;
            _maxSize = 0;
            baseOffset = 0;
            detached = false;
            type = Type.VERTEX;
            usage = Usage.STATIC;
            backendHandle = null;
            data = null;
            dataVersion = 0;
        }

        public VAR(byte[] source, int numElements, int elementSize, VARArea area)
            : this(source, numElements, elementSize, area, Format.NONE, Type.VERTEX, Usage.STATIC)
        {
        }

        public VAR(byte[] source, int numElements, int elementSize, VARArea area, Format format, Type type, Usage usage)
            : this()
        {
            init(source, numElements, area, format, elementSize, type, usage);
        }

        /**
         Uploads the memory.  The element type is inferred from the
         pointer type by the preprocessor.  Sample usage:

     
	  <PRE>
	    // Once at the beginning of the program
        VARAreaRef area = VARArea::create(1024 * 1024);

        //----------
        // Store data in main memory
	    Array<Vector3> vertex;
        Array<int>     index;
		
        //... fill out vertex & index arrays

        //------------
        // Upload to graphics card every frame
        area.reset();
		VAR varray(vertex, area);
        renderDevice->beginIndexedPrimitives();
    		renderDevice->setVertexArray(varray);
            renderDevice->sendIndices(RenderDevice::TRIANGLES, index);
        renderDevice->endIndexedPrimitives();
	  </PRE>
		See GLG3D_Demo for examples.
    */
        public VAR(Vector3[] source, VARArea area)
            : this(source, source == null ? 0 : source.Length, area)
        {
        }

        public VAR(Vector3[] source, int numElements, VARArea area)
            : this(toBytes(source), numElements, 12, area, Format.FLOAT, Type.VERTEX, Usage.STATIC)
        {
        }

        public VAR(Vector2[] source, VARArea area)
            : this(source, source == null ? 0 : source.Length, area)
        {
        }

        public VAR(Vector2[] source, int numElements, VARArea area)
            : this(toBytes(source), numElements, 8, area, Format.FLOAT, Type.TEXCOORD0, Usage.STATIC)
        {
        }

        public VAR(Vector4[] source, VARArea area)
            : this(source, source == null ? 0 : source.Length, area)
        {
        }

        public VAR(Vector4[] source, int numElements, VARArea area)
            : this(toBytes(source), numElements, 16, area, Format.FLOAT, Type.GENERIC0, Usage.STATIC)
        {
        }

        public VAR(Color3[] source, VARArea area)
            : this(source, source == null ? 0 : source.Length, area)
        {
        }

        public VAR(Color3[] source, int numElements, VARArea area)
            : this(toBytes(source), numElements, 12, area, Format.FLOAT, Type.COLOR, Usage.STATIC)
        {
        }

        public VAR(Color4[] source, VARArea area)
            : this(source, source == null ? 0 : source.Length, area)
        {
        }

        public VAR(Color4[] source, int numElements, VARArea area)
            : this(toBytes(source), numElements, 16, area, Format.FLOAT, Type.COLOR, Usage.STATIC)
        {
        }

        public VAR(int[] source, VARArea area)
            : this(source, source == null ? 0 : source.Length, area)
        {
        }

        public VAR(int[] source, int numElements, VARArea area)
            : this(toBytes(source), numElements, 4, area, Format.INT, Type.INDEX, Usage.STATIC)
        {
        }

        public VAR(uint[] source, VARArea area)
            : this(source, source == null ? 0 : source.Length, area)
        {
        }

        public VAR(uint[] source, int numElements, VARArea area)
            : this(toBytes(source), numElements, 4, area, Format.UNSIGNED_INT, Type.INDEX, Usage.STATIC)
        {
        }

        public VAR(float[] source, VARArea area)
            : this(source, source == null ? 0 : source.Length, area)
        {
        }

        public VAR(float[] source, int numElements, VARArea area)
            : this(toBytes(source), numElements, 4, area, Format.FLOAT, Type.VERTEX, Usage.STATIC)
        {
        }

        public VAR(ushort[] source, VARArea area)
            : this(source, source == null ? 0 : source.Length, area)
        {
        }

        public VAR(ushort[] source, int numElements, VARArea area)
            : this(toBytes(source), numElements, 2, area, Format.UNSIGNED_SHORT, Type.INDEX, Usage.STATIC)
        {
        }

        public static VAR create(Type type, int count)
        {
            return create(type, count, Usage.STATIC);
        }

        public static VAR create(Type type, int count, Usage usage)
        {
            VAR var = new VAR();
            var.type = type;
            var.usage = usage;
            var.elementSize = strideOfType(type);
            var.underlyingRepresentation = formatOfType(type);
            var.numElements = count < 0 ? 0 : count;
            var._maxSize = (long)var.elementSize * var.numElements;
            var.data = var._maxSize > 0 ? new byte[var._maxSize] : new byte[0];
            return var;
        }

        public static VAR create<T>(T[] data) where T : struct
        {
            return create<T>(data, Usage.STATIC);
        }

        public static VAR create<T>(T[] data, Usage usage) where T : struct
        {
            VAR var = new VAR();
            var.type = typeOfElement(typeof(T));
            var.usage = usage;
            var.elementSize = elementSizeOf<T>();
            var.underlyingRepresentation = formatOf<T>();
            var.numElements = data == null ? 0 : data.Length;
            var._maxSize = (long)var.elementSize * var.numElements;
            var.data = var._maxSize > 0 ? new byte[var._maxSize] : new byte[0];
            var.setData(data);
            return var;
        }

        public static VAR create<T>(T[] data, VARArea area) where T : struct
        {
            if (area == null)
            {
                throw new ArgumentNullException("area");
            }
            VAR var = new VAR();
            var.init(toBytes(data), data == null ? 0 : data.Length, area,
                     formatOf<T>(), elementSizeOf<T>(), typeOfElement(typeof(T)), Usage.STATIC);
            return var;
        }

        private void init(byte[] source, int count, VARArea owner, Format format, int eltSize, Type varType, Usage varUsage)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("area", "Bad VARArea");
            }
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("numElements");
            }
            if (eltSize <= 0)
            {
                throw new ArgumentOutOfRangeException("elementSize");
            }

            numElements = count;
            area = owner;
            underlyingRepresentation = format;
            elementSize = eltSize;
            type = varType;
            usage = owner.getUsage();

            long size = (long)elementSize * numElements;
            _maxSize = size;

            Debug.debugAssertM(
                format == Format.NONE || (elementSize % sizeOfFormat(format)) == 0,
                "Sanity check failed on OpenGL data format; you may" +
                " be using an unsupported type in a vertex array.");

            generation = area.currentGeneration();
            detached = false;

            baseOffset = area.allocate(this, size);

            if (size > 0)
            {
                uploadToCard(source, 0, 0, (int)size);
            }
        }

        public void update<T>(T[] source, int count) where T : struct
        {
            update(toBytes(source, count), count, formatOf<T>(), elementSizeOf<T>());
        }

        public void update<T>(T[] source) where T : struct
        {
            update(source, source == null ? 0 : source.Length);
        }

        public void update<T>(Array<T> source) where T : struct
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            update(source.getCArray(), source.size());
        }

        public void update(byte[] source, int count, Format format, int eltSize)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("numElements");
            }

            long size = (long)eltSize * count;

            if (size > _maxSize)
            {
                throw new ArgumentException("A VAR can only be updated with an array that is smaller " +
                                            "or equal size (in bytes) to the original array.");
            }

            if (area == null)
            {
                throw new InvalidOperationException("A VAR can only be updated when it was allocated inside a VARArea.");
            }
            if (detached || generation != area.currentGeneration())
            {
                throw new InvalidOperationException("The VARArea has been reset since this VAR was created.");
            }

            numElements = count;
            underlyingRepresentation = format;
            elementSize = eltSize;

            Debug.debugAssertM(
                format == Format.NONE || (elementSize % sizeOfFormat(format)) == 0,
                "Sanity check failed on OpenGL data format; you may" +
                " be using an unsupported type in a vertex array.");

            if (size > 0)
            {
                uploadToCard(source, 0, 0, (int)size);
            }
        }

        public void set<T>(int index, T value) where T : struct
        {
            set(index, toBytes(new T[] { value }), 0, formatOf<T>(), elementSizeOf<T>());
        }

        private void set(int index, byte[] value, int valueOffset, Format format, int eltSize)
        {
            Debug.debugAssertM(index < numElements && index >= 0,
                "Cannot call VAR::set with out of bounds index");

            Debug.debugAssertM(format == underlyingRepresentation,
                "Value argument to VAR::set must match the intialization type.");

            Debug.debugAssertM(eltSize == elementSize,
                "Value argument to VAR::set must match the intialization type's memory footprint.");

            if (index < 0 || index >= numElements)
            {
                throw new ArgumentOutOfRangeException("index", "Cannot call VAR::set with out of bounds index");
            }
            if (format != underlyingRepresentation)
            {
                throw new ArgumentException("Value argument to VAR::set must match the intialization type.");
            }
            if (eltSize != elementSize)
            {
                throw new ArgumentException("Value argument to VAR::set must match the intialization type's memory footprint.");
            }

            uploadToCard(value, valueOffset, index * eltSize, eltSize);
        }

        /** Performs the actual memory transfer (like memcpy).  The dstPtrOffset is the number of
            <B>bytes</B> to add to _pointer when performing the transfer. */
        private void uploadToCard(byte[] source, int sourceOffset, int destinationOffset, int size)
        {
            if (size <= 0)
            {
                return;
            }

            if (area != null)
            {
                area.writeAt(baseOffset + destinationOffset, source, sourceOffset, size);
            }
            else
            {
                if (data == null)
                {
                    data = new byte[destinationOffset + size];
                }
                else if (data.Length < destinationOffset + size)
                {
                    byte[] resized = new byte[destinationOffset + size];
                    Array.Copy(data, resized, data.Length);
                    data = resized;
                }
                Array.Copy(source, sourceOffset, data, destinationOffset, size);
            }

            dataVersion++;
        }

        /**
         Returns true if this VAR can be used for rendering
         (i.e. contains data and the parent VARArea has not been
         reset).
         */
        public bool valid()
        {
            return
                (area != null) &&
                (area.currentGeneration() == generation) &&
                !detached &&
                area.owns(this);
        }

        /** Maximum size that can be loaded via update into this VAR. */
        public long maxSize()
        {
            if (valid())
            {
                return _maxSize;
            }
            else
            {
                return 0;
            }
        }

        public void setData<T>(T[] data) where T : struct
        {
            setData(toBytes(data));
        }

        public void setData(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            if (_maxSize > 0 && bytes.Length > _maxSize)
            {
                throw new ArgumentException("The data exceeds the VAR capacity.", "bytes");
            }

            if (area != null)
            {
                if (detached || !area.owns(this))
                {
                    throw new InvalidOperationException("The VARArea has been reset since this VAR was created.");
                }
                area.writeAt(baseOffset, bytes, 0, bytes.Length);
                numElements = elementSize > 0 ? bytes.Length / elementSize : 0;
                dataVersion++;
                return;
            }

            if (data == null)
            {
                data = new byte[bytes.Length];
            }
            else if (data.Length < bytes.Length)
            {
                byte[] resized = new byte[bytes.Length];
                Array.Copy(data, resized, data.Length);
                data = resized;
            }
            Array.Copy(bytes, 0, data, 0, bytes.Length);
            numElements = elementSize > 0 ? bytes.Length / elementSize : 0;
            dataVersion++;
        }

        public void setSubData<T>(int offset, T[] data) where T : struct
        {
            setSubData(offset, toBytes(data));
        }

        public void setSubData(int offset, byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (_maxSize > 0 && offset + bytes.Length > _maxSize)
            {
                throw new ArgumentException("The sub-data exceeds the VAR capacity.", "bytes");
            }

            if (area != null)
            {
                if (detached || !area.owns(this))
                {
                    throw new InvalidOperationException("The VARArea has been reset since this VAR was created.");
                }
                area.writeAt(baseOffset + offset, bytes, 0, bytes.Length);
                dataVersion++;
                return;
            }

            if (data == null)
            {
                data = new byte[offset + bytes.Length];
            }
            else if (data.Length < offset + bytes.Length)
            {
                byte[] resized = new byte[offset + bytes.Length];
                Array.Copy(data, resized, data.Length);
                data = resized;
            }
            Array.Copy(bytes, 0, data, offset, bytes.Length);
            dataVersion++;
        }

        public T[] getData<T>() where T : struct
        {
            byte[] bytes = getData();
            int size = Marshal.SizeOf(typeof(T));
            T[] result = new T[bytes == null ? 0 : bytes.Length / size];
            if (result.Length > 0)
            {
                if (isPrimitiveType(typeof(T)))
                {
                    Buffer.BlockCopy(bytes, 0, result, 0, Math.Min(bytes.Length, result.Length * size));
                }
                else
                {
                    IntPtr ptr = Marshal.AllocHGlobal(result.Length * size);
                    try
                    {
                        Marshal.Copy(bytes, 0, ptr, result.Length * size);
                        for (int i = 0; i < result.Length; ++i)
                        {
                            result[i] = (T)Marshal.PtrToStructure(new IntPtr(ptr.ToInt64() + i * size), typeof(T));
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(ptr);
                    }
                }
            }
            return result;
        }

        public byte[] getData()
        {
            if (area != null)
            {
                int size = numElements * elementSize;
                if (size <= 0)
                {
                    return new byte[0];
                }
                return area.readAt(baseOffset, size);
            }
            return data;
        }

        public Type getType()
        {
            return type;
        }

        public void setType(Type newType)
        {
            type = newType;
            if (area == null)
            {
                elementSize = strideOfType(newType);
                underlyingRepresentation = formatOfType(newType);
            }
        }

        public Usage getUsage()
        {
            return usage;
        }

        public int getStride()
        {
            return elementSize;
        }

        public int getCount()
        {
            return numElements;
        }

        public int getDataVersion()
        {
            return dataVersion;
        }

        public Format getFormat()
        {
            return underlyingRepresentation;
        }

        public int getComponentCount()
        {
            int formatSize = sizeOfFormat(underlyingRepresentation);
            if (formatSize <= 0)
            {
                return 0;
            }
            return elementSize / formatSize;
        }

        public int getElementSize()
        {
            return elementSize;
        }

        public VARArea getArea()
        {
            return area;
        }

        public ulong getGeneration()
        {
            return generation;
        }

        public long getOffset()
        {
            return baseOffset;
        }

        internal void attachToArea(VARArea owner, ulong areaGeneration, long offset, long length)
        {
            area = owner;
            generation = areaGeneration;
            baseOffset = offset;
            detached = false;
            _maxSize = length;
            numElements = elementSize > 0 ? (int)(length / elementSize) : 0;
            data = null;
            dataVersion++;
        }

        internal void setMaxSize(long size)
        {
            _maxSize = size;
        }

        internal void detach()
        {
            detached = true;
            dataVersion++;
        }

        internal void setBlockOffset(long offset)
        {
            baseOffset = offset;
            dataVersion++;
        }

        internal void replaceContents(int byteCount)
        {
            numElements = elementSize > 0 ? byteCount / elementSize : 0;
            dataVersion++;
        }

        internal object getBackendHandle()
        {
            return backendHandle;
        }

        internal void setBackendHandle(object handle)
        {
            backendHandle = handle;
        }

        internal void vertexPointer()
        {
            throw new NotSupportedException("G3D: the backend-neutral VAR API does not expose fixed-function client-state vertex pointers; use RenderDevice.setVertexArray.");
        }

        internal void normalPointer()
        {
            throw new NotSupportedException("G3D: the backend-neutral VAR API does not expose fixed-function client-state normal pointers; use RenderDevice.setNormalArray.");
        }

        internal void colorPointer()
        {
            throw new NotSupportedException("G3D: the backend-neutral VAR API does not expose fixed-function client-state color pointers; use RenderDevice.setColorArray.");
        }

        internal void texCoordPointer(uint unit)
        {
            throw new NotSupportedException("G3D: the backend-neutral VAR API does not expose fixed-function client-state texture coordinate pointers; use RenderDevice.setTexCoordArray.");
        }

        internal void vertexAttribPointer(uint attribNum, bool normalize)
        {
            IGraphicsBackend backend = GLCaps.getBackendInstance();
            if (backend == null)
            {
                throw new InvalidOperationException("G3D: no graphics backend is available to bind a generic vertex attribute.");
            }
            backend.SetVertexAttribArray((int)attribNum, this, normalize);
        }

        internal static bool isPrimitiveType(global::System.Type type)
        {
            return type.IsPrimitive || type == typeof(decimal);
        }

        internal static int elementSizeOf<T>() where T : struct
        {
            return Marshal.SizeOf(typeof(T));
        }

        internal static byte[] toBytes<T>(T[] values)
        {
            return toBytes(values, values == null ? 0 : values.Length);
        }

        internal static byte[] toBytes<T>(T[] values, int count)
        {
            if (values == null || count <= 0)
            {
                return new byte[0];
            }
            if (count > values.Length)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            int elementSize = Marshal.SizeOf(typeof(T));
            byte[] bytes = new byte[count * elementSize];

            if (isPrimitiveType(typeof(T)))
            {
                Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            }
            else
            {
                IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);
                try
                {
                    for (int i = 0; i < count; ++i)
                    {
                        Marshal.StructureToPtr(values[i], new IntPtr(ptr.ToInt64() + i * elementSize), false);
                    }
                    Marshal.Copy(ptr, bytes, 0, bytes.Length);
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
            return bytes;
        }

        internal static Format formatOf<T>() where T : struct
        {
            return formatOfType(typeof(T));
        }

        internal static Format formatOfType(global::System.Type type)
        {
            if (type == typeof(float) || type == typeof(Vector2) || type == typeof(Vector3) ||
                type == typeof(Vector4) || type == typeof(Color3) || type == typeof(Color4))
            {
                return Format.FLOAT;
            }
            if (type == typeof(double))
            {
                return Format.DOUBLE;
            }
            if (type == typeof(int))
            {
                return Format.INT;
            }
            if (type == typeof(uint))
            {
                return Format.UNSIGNED_INT;
            }
            if (type == typeof(short) || type == typeof(Vector2int16) || type == typeof(Vector3int16))
            {
                return Format.SHORT;
            }
            if (type == typeof(ushort))
            {
                return Format.UNSIGNED_SHORT;
            }
            if (type == typeof(byte) || type == typeof(Color3uint8) || type == typeof(Color4uint8))
            {
                return Format.UNSIGNED_BYTE;
            }
            if (type == typeof(sbyte))
            {
                return Format.BYTE;
            }
            return Format.NONE;
        }

        internal static int sizeOfFormat(Format format)
        {
            switch (format)
            {
                case Format.BYTE:
                case Format.UNSIGNED_BYTE:
                    return 1;
                case Format.SHORT:
                case Format.UNSIGNED_SHORT:
                    return 2;
                case Format.INT:
                case Format.UNSIGNED_INT:
                case Format.FLOAT:
                    return 4;
                case Format.DOUBLE:
                    return 8;
                default:
                    return 0;
            }
        }

        internal static int strideOfType(Type type)
        {
            switch (type)
            {
                case Type.VERTEX:
                case Type.NORMAL:
                case Type.TEXCOORD0:
                case Type.TEXCOORD1:
                case Type.TEXCOORD2:
                case Type.TEXCOORD3:
                case Type.TEXCOORD4:
                case Type.TEXCOORD5:
                case Type.TEXCOORD6:
                case Type.TEXCOORD7:
                    return 12;
                case Type.COLOR:
                    return 16;
                case Type.INDEX:
                    return 4;
                default:
                    return 16;
            }
        }

        internal static Format formatOfType(Type type)
        {
            if (type == Type.INDEX)
            {
                return Format.UNSIGNED_INT;
            }
            return Format.FLOAT;
        }

        internal static Type typeOfElement(global::System.Type type)
        {
            if (type == typeof(Vector2))
            {
                return Type.TEXCOORD0;
            }
            if (type == typeof(Color3) || type == typeof(Color4) ||
                type == typeof(Color3uint8) || type == typeof(Color4uint8))
            {
                return Type.COLOR;
            }
            if (type == typeof(int) || type == typeof(uint) || type == typeof(ushort) ||
                type == typeof(short) || type == typeof(byte) || type == typeof(sbyte))
            {
                return Type.INDEX;
            }
            if (type == typeof(Vector4))
            {
                return Type.GENERIC0;
            }
            return Type.VERTEX;
        }
    }
}

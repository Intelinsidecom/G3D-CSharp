/**
  @file IFSModel.h
  
  @maintainer Morgan McGuire, matrix@graphics3d.com

  @cite Original IFS code by Nate Robbins

  @created 2003-11-12
  @edited  2006-04-26
*/ 

using System;
using System.IO;
using G3D;

namespace G3D
{
    /**
     Loads the IFS file format.  Note that you can convert 
     many other formats (e.g. 3DS, SM, OBJ, MD2) to IFS format
     using the IFSBuilder sample code provided with G3D.
    */
    public class IFSModel : ReferenceCountedObject
    {
        private const int managedArrayHeaderBytes = 24;
        private const int managedGeometryHeaderBytes = 2 * managedArrayHeaderBytes;
        private const int managedIFSModelHeaderBytes = 64;
        private const int managedFaceBytes = 24;
        private const int managedEdgeBytes = 16;
        private const int managedVertexBytes = 24;
        private const int managedVector3Bytes = 12;
        private const int managedVector2Bytes = 8;
        private const int managedIntBytes = 4;

        private class PosedIFSModel : PosedModel
        {
            public ReferenceCountedPointer<IFSModel> model;
            public CoordinateFrame cframe;
            public bool perVertexNormals;
            public bool useMaterial;
            public GMaterial material;

            public PosedIFSModel(ReferenceCountedPointer<IFSModel> _model, CoordinateFrame _cframe, bool _pvn,
                GMaterial _mat, bool _useMat)
            {
                model = _model;
                cframe = _cframe;
                perVertexNormals = _pvn;
                material = _mat;
                useMaterial = _useMat;
            }

            public override string name()
            {
                return model.pointer().name;
            }

            public override void getCoordinateFrame(out CoordinateFrame cf)
            {
                cf = cframe;
            }

            public override MeshAlg.Geometry objectSpaceGeometry()
            {
                return model.pointer().geometry;
            }

            public override Array<MeshAlg.Face> faces()
            {
                return model.pointer().faceArray;
            }

            public override Array<MeshAlg.Edge> edges()
            {
                return model.pointer().edgeArray;
            }

            public override Array<MeshAlg.Vertex> vertices()
            {
                return model.pointer().vertexArray;
            }

            public override Array<MeshAlg.Face> weldedFaces()
            {
                return model.pointer().weldedFaceArray;
            }

            public override Array<MeshAlg.Edge> weldedEdges()
            {
                return model.pointer().weldedEdgeArray;
            }

            public override Array<MeshAlg.Vertex> weldedVertices()
            {
                return model.pointer().weldedVertexArray;
            }

            public override Array<Vector2> texCoords()
            {
                if (!hasTexCoords())
                {
                    throw new InvalidOperationException("Model has no texture coordinates.");
                }
                return model.pointer().texArray;
            }

            public override bool hasTexCoords()
            {
                return model.pointer().texArray.size() > 0;
            }

            public override Array<int> triangleIndices()
            {
                return model.pointer().indexArray;
            }

            public override void getObjectSpaceBoundingSphere(out Sphere sphere)
            {
                sphere = model.pointer().boundingSphere;
            }

            public override void getObjectSpaceBoundingBox(out Box box)
            {
                box = model.pointer().boundingBox;
            }

            public override void render(RenderDevice renderDevice)
            {
                IFSModel m = model.pointer();

                if (useMaterial)
                {
                    material.configure(renderDevice);
                }

                renderDevice.setObjectToWorldMatrix(coordinateFrame());

                const int varSize = 2 * 1024 * 1024;
                if (IFSModel.varArea == null || IFSModel.varArea.isNull())
                {
                    // Initialize VAR
                    IFSModel.varArea = new ReferenceCountedPointer<VARArea>(
                        VARArea.create((long)varSize));
                }

                if (perVertexNormals)
                {
                    renderDevice.setShadeMode(RenderDevice.ShadeMode.SHADE_SMOOTH);
                    long modelSize = (long)managedVector3Bytes * 2 * m.geometry.vertexArray.size() +
                                     (long)managedVector3Bytes * m.texArray.size() +
                                     (long)managedIntBytes * m.indexArray.size();

                    if (IFSModel.varArea.notNull() && 
                        (IFSModel.varArea.pointer().totalSize() >= modelSize)) {
                        // Can use VAR

                        if (IFSModel.varArea.pointer().freeSize() < modelSize + 128) {
                            // Not enough free space left in the common area;
                            // reset it to allocate new arrays (this might
                            // stall the pipeline).  Otherwise, just allocate
                            // on top of what was already there.
                            IFSModel.varArea.pointer().reset();

                            // Resetting invalidates the old VAR arrays.
                            // Just knock out the lastModel to prevent a match.

                            IFSModel.clearLastModel();
                        }

                        if (model != IFSModel.lastModel) {
                            // Upload new data (cache miss)
                            IFSModel.clearLastModel();
                            IFSModel.lastModel = new ReferenceCountedPointer<IFSModel>(model.pointer());
                            IFSModel.lastVertexVAR = createVertexVAR(m.geometry.vertexArray.ToArray(), IFSModel.varArea.pointer());
                            IFSModel.lastNormalVAR = createNormalVAR(m.geometry.normalArray.ToArray(), IFSModel.varArea.pointer());
                            IFSModel.lastTexCoordVAR = createTexCoordVAR(m.texArray.ToArray(), IFSModel.varArea.pointer());
                            IFSModel.lastIndexVAR = createIndexVAR(m.indexArray.ToArray(), IFSModel.varArea.pointer());
                        }

                        VAR vertexVAR = (IFSModel.lastVertexVAR != null) && IFSModel.lastVertexVAR.valid() ? IFSModel.lastVertexVAR : null;
                        VAR normalVAR = (IFSModel.lastNormalVAR != null) && IFSModel.lastNormalVAR.valid() ? IFSModel.lastNormalVAR : null;
                        VAR texCoordVAR = (IFSModel.lastTexCoordVAR != null) && IFSModel.lastTexCoordVAR.valid() ? IFSModel.lastTexCoordVAR : null;
                        VAR indexVAR = (IFSModel.lastIndexVAR != null) && IFSModel.lastIndexVAR.valid() ? IFSModel.lastIndexVAR : null;

                        renderDevice.beginIndexedPrimitives();
                            renderDevice.setTexCoordArray(0, texCoordVAR);
                            renderDevice.setNormalArray(normalVAR);
                            renderDevice.setVertexArray(vertexVAR);
                            renderDevice.setIndexArray(indexVAR);
                            renderDevice.sendIndices(RenderDevice.Primitive.TRIANGLES, m.indexArray.size());
                        renderDevice.endIndexedPrimitives();

                    } else {
                        // No VAR
                        int n = m.indexArray.size();

                        renderDevice.beginPrimitive(RenderDevice.Primitive.TRIANGLES);
                            if (m.texArray.size() > 0) {
                                for (int i = 0; i < n; ++i) {
                                    int v = m.indexArray[i];            
                                    renderDevice.setTexCoord(0, m.texArray[v]);
                                    renderDevice.setNormal(m.geometry.normalArray[v]);
                                    renderDevice.sendVertex(m.geometry.vertexArray[v]);
                                }

                            } else {
                                for (int i = 0; i < n; ++i) {
                                    int v = m.indexArray[i];            
                                    renderDevice.setNormal(m.geometry.normalArray[v]);
                                    renderDevice.sendVertex(m.geometry.vertexArray[v]);
                                }
                            }
                        renderDevice.endPrimitive();
                    }

                } else {

                    // Face Normals (slow)

                    int n = m.faceArray.size();

                    renderDevice.beginPrimitive(RenderDevice.Primitive.TRIANGLES);
                        if (m.texArray.size() > 0) {
                            for (int f = 0; f < n; ++f) {
                                renderDevice.setNormal(m.faceNormalArray[f]);
                                for (int j = 0; j < 3; ++j) {                    
                                    renderDevice.setTexCoord(0, m.texArray[m.faceArray[f].vertexIndex[j]]);
                                    renderDevice.sendVertex(m.geometry.vertexArray[m.faceArray[f].vertexIndex[j]]);
                                }
                            }
                        } else {
                            for (int f = 0; f < n; ++f) {
                                renderDevice.setNormal(m.faceNormalArray[f]);
                                for (int j = 0; j < 3; ++j) {                    
                                    renderDevice.sendVertex(m.geometry.vertexArray[m.faceArray[f].vertexIndex[j]]);
                                }
                            }
                        }
                    renderDevice.endPrimitive();
                }
            }

            public override int numBoundaryEdges()
            {
                return model.pointer().numBoundaryEdges;
            }

            public override int numWeldedBoundaryEdges()
            {
                return model.pointer().numWeldedBoundaryEdges;
            }

            public override Array<Vector3> objectSpaceFaceNormals(bool normalize)
            {
                // Normals are always unit length
                return model.pointer().faceNormalArray;
            }
        }

        private string name;
        private string filename;
        private Array<int> indexArray = new Array<int>();
        private Array<MeshAlg.Vertex> vertexArray = new Array<MeshAlg.Vertex>();
        private Array<Vector2> texArray = new Array<Vector2>();

        private MeshAlg.Geometry geometry = new MeshAlg.Geometry();
        private Array<MeshAlg.Face> faceArray = new Array<MeshAlg.Face>();
        private Array<MeshAlg.Edge> edgeArray = new Array<MeshAlg.Edge>();
        private Array<Vector3> faceNormalArray = new Array<Vector3>();
        private Array<MeshAlg.Face> weldedFaceArray = new Array<MeshAlg.Face>();
        private Array<MeshAlg.Edge> weldedEdgeArray = new Array<MeshAlg.Edge>();
        private Array<MeshAlg.Vertex> weldedVertexArray = new Array<MeshAlg.Vertex>();
        private int numBoundaryEdges;
        private int numWeldedBoundaryEdges;
        private Sphere boundingSphere;
        private Box boundingBox;


        /** Shared by all models */
        private static ReferenceCountedPointer<VARArea> varArea;

        // Used by the last object to render.  If the
        // new model is the same as the old one, an upload
        // step can be avoided.
        private static ReferenceCountedPointer<IFSModel> lastModel;
        private static VAR lastVertexVAR;
        private static VAR lastNormalVAR;
        private static VAR lastTexCoordVAR;
        private static VAR lastIndexVAR;

        /** Only called from create */
        private IFSModel()
        {
        }

        /** Only called from create */
        private void load(string filename, Vector3 scale, CoordinateFrame cframe, bool weld)
        {
            reset();

            this.filename = filename;
            load(filename, out name, indexArray, geometry.vertexArray, texArray);

            Debug.debugAssert(geometry.vertexArray.size() > 0);
            Debug.debugAssert(indexArray.size() > 0);

            for (int i = 0; i < geometry.vertexArray.size(); ++i)
            {
                Vector3 v = geometry.vertexArray[i];
                v = cframe.pointToWorldSpace(v * scale);
                geometry.vertexArray[i] = v;
            }

            MeshAlg.computeAdjacency(geometry.vertexArray, indexArray, faceArray, edgeArray, vertexArray);
            weldedFaceArray = copyFaces(faceArray);
            weldedEdgeArray = copyEdges(edgeArray);
            weldedVertexArray = copyVertices(vertexArray);

            if (weld) {
                MeshAlg.weldAdjacency(geometry.vertexArray, faceArray, edgeArray, vertexArray);
            }
	
            MeshAlg.computeNormals(geometry.vertexArray, faceArray, vertexArray, geometry.normalArray, faceNormalArray);

            Box box;
            Sphere sphere;
            MeshAlg.computeBounds(geometry.vertexArray, out box, out sphere);
            boundingBox = box;
            boundingSphere = sphere;

            numBoundaryEdges = MeshAlg.countBoundaryEdges(edgeArray);
            numWeldedBoundaryEdges = MeshAlg.countBoundaryEdges(weldedEdgeArray);
        }

        /** Only called from create */
        private void reset()
        {
            geometry.clear();
            indexArray.clear();
            faceArray.clear();
            vertexArray.clear();
            edgeArray.clear();
            texArray.clear();
        }

        /**
         Throws an exception if anything
         goes wrong.
         @param scale 3D scale factors to apply to vertices while loading (*after* cframe)
         @param cframe Coordinate transform to apply to vertices while loading.  
                       This is not part of the object to world transformation
                       for the model when posed; it really modifies the object
                       space geometry.
         @param weld   Toggles welding colocated vertices, an O(n^2) operation. Defaults to true
        */
        public static ReferenceCountedPointer<IFSModel> create(string filename)
        {
            return create(filename, new Vector3(1, 1, 1), CoordinateFrame.identity(), true);
        }

        public static ReferenceCountedPointer<IFSModel> create(string filename, Vector3 scale)
        {
            return create(filename, scale, CoordinateFrame.identity(), true);
        }

        public static ReferenceCountedPointer<IFSModel> create(string filename, Vector3 scale, CoordinateFrame cframe)
        {
            return create(filename, scale, cframe, true);
        }

        public static ReferenceCountedPointer<IFSModel> create(string filename, Vector3 scale, CoordinateFrame cframe, bool weld)
        {
            IFSModel ret = new IFSModel();
            try
            {
                ret.load(filename, scale, cframe, weld);
            }
            catch
            {
                ret.Dispose();
                throw;
            }
            return new ReferenceCountedPointer<IFSModel>(ret);
        }

        public static ReferenceCountedPointer<IFSModel> create(string filename, double scale)
        {
            return create(filename, scale, CoordinateFrame.identity(), true);
        }

        public static ReferenceCountedPointer<IFSModel> create(string filename, double scale, CoordinateFrame cframe)
        {
            return create(filename, scale, cframe, true);
        }

        public static ReferenceCountedPointer<IFSModel> create(string filename, double scale, CoordinateFrame cframe, bool weld)
        {
            return create(filename, new Vector3((float)scale, (float)scale, (float)scale), cframe, weld);
        }

        /**
         If perVertexNormals is false, the model is rendered with per-face normals,
         which are slower.  No color or texture is set when rendered (that is,
         the existing RenderDevice values are used.)
        */
        public virtual ReferenceCountedPointer<PosedModel> pose()
        {
            return pose(CoordinateFrame.identity(), true);
        }

        public virtual ReferenceCountedPointer<PosedModel> pose(CoordinateFrame cframe)
        {
            return pose(cframe, true);
        }

        public virtual ReferenceCountedPointer<PosedModel> pose(CoordinateFrame cframe, bool perVertexNormals)
        {
            return new ReferenceCountedPointer<PosedModel>(new PosedIFSModel(new ReferenceCountedPointer<IFSModel>(this), cframe, perVertexNormals, new GMaterial(), false));
        }

        /**
         If perVertexNormals is false, the model is rendered with per-face normals,
         which are slower.
         */
        public virtual ReferenceCountedPointer<PosedModel> pose(CoordinateFrame cframe, GMaterial material)
        {
            return pose(cframe, material, true);
        }

        public virtual ReferenceCountedPointer<PosedModel> pose(CoordinateFrame cframe, GMaterial material, bool perVertexNormals)
        {
            return new ReferenceCountedPointer<PosedModel>(new PosedIFSModel(new ReferenceCountedPointer<IFSModel>(this), cframe, perVertexNormals, material, true));
        }

        public virtual ulong mainMemorySize()
        {
            ulong frameSize = (ulong)(managedGeometryHeaderBytes +
                (managedVector3Bytes + managedVector3Bytes) * geometry.vertexArray.size());
            ulong texCoordSize = (ulong)(managedVector2Bytes * texArray.size());
            ulong indexSize = (ulong)(indexArray.size() * managedIntBytes);
            ulong faceSize = (ulong)(faceArray.size() * managedFaceBytes);
            ulong valentSize = (ulong)(vertexArray.size() * managedVertexBytes);
            for (int i = 0; i < vertexArray.size(); ++i)
            {
                valentSize += (ulong)(vertexArray[i].faceIndex.size() * managedIntBytes);
                valentSize += (ulong)(vertexArray[i].edgeIndex.size() * managedIntBytes);
            }

            ulong edgeSize = (ulong)(edgeArray.size() * managedEdgeBytes);

            return (ulong)managedIFSModelHeaderBytes + frameSize + indexSize + faceSize + valentSize + edgeSize + texCoordSize;
        }

        /** Writes an IFS or PLY2 file from data in arrays. (Note PLY2 Files do not support texCoords.) */
        public static void save(string filename, string name,
                     Array<int> index, Array<Vector3> vertex, Array<Vector2> texCoord)
        {
            if (index == null)
            {
                throw new ArgumentNullException("index");
            }
            if (vertex == null)
            {
                throw new ArgumentNullException("vertex");
            }
            if (texCoord == null)
            {
                throw new ArgumentNullException("texCoord");
            }

            string extension = FileUtils.filenameExt(filename);

            if ("ifs" == extension) {
	
                float ifs_version = (texCoord.size() == 0)? 1.0f : 1.1f;

                BinaryOutput b = new BinaryOutput(filename, G3DEndian.G3D_LITTLE_ENDIAN);

                b.writeString32("IFS");
                b.writeFloat32(ifs_version);
                b.writeString32(name == null ? "" : name);

                b.writeString32("VERTICES");

                b.writeUInt32((uint)vertex.size());

                for (uint v = 0; v < (uint)vertex.size(); ++v) {
                    vertex[(int)v].serialize(b);
                }

                b.writeString32("TRIANGLES");

                b.writeUInt32((uint)(index.size() / 3));
                for (uint i = 0; i < (uint)index.size(); ++i) {
                    b.writeUInt32((uint)index[(int)i]);
                }
	
                if (ifs_version == 1.1f) {
                    b.writeString32("TEXTURECOORD");
                    if (texCoord.size() != vertex.size()) {
                        throw new ArgumentException("Number of texCoords must match the number of vertices", "texCoord");
                    }
                    b.writeUInt32((uint)texCoord.size());
                    for (uint t = 0; t < (uint)texCoord.size(); ++t) {
                        texCoord[(int)t].serialize(b);
                    }
                }

                b.commit(false);
            } else if ("ply2" == extension) { 
                if (texCoord.size() != 0) {
                    throw new ArgumentException("texCoord.size() != 0, PLY2 files do not support saving texCoords.", "texCoord");
                }
                TextOutput to = new TextOutput(filename, new TextOutput.Options());

                int nF = index.size() / 3;
                to.printf("%d\n%d\n", vertex.size(), nF);

                for (uint i = 0; i < (uint)vertex.size(); ++i) {
                    to.printf("%f ", vertex[(int)i].x);
                    to.printf("%f ", vertex[(int)i].y);
                    to.printf("%f\n", vertex[(int)i].z);
                }

                for (uint i = 0; i < (uint)nF; ++i) {
                    to.printf("3 %d  %d  %d\n", index[(int)(3*i)], index[(int)(3*i + 1)], index[(int)(3*i + 2)]);
                }

                to.commit(false);
            } else {
                throw new NotSupportedException(Format.format("unsupported filename type %s", extension));
            }
        }

        /** Parses an IFS or PLY2 file from disk into arrays; does no cleanup or welding */
        public static void load(string filename, out string name,
            Array<int> index, Array<Vector3> vertex, Array<Vector2> texCoord)
        {
            if (index == null)
            {
                throw new ArgumentNullException("index");
            }
            if (vertex == null)
            {
                throw new ArgumentNullException("vertex");
            }
            if (texCoord == null)
            {
                throw new ArgumentNullException("texCoord");
            }

            string extension = FileUtils.filenameExt(filename);

            if (extension == "ifs" ) {
                BinaryInput bi;
                try
                {
                    bi = new BinaryInput(filename, G3DEndian.G3D_LITTLE_ENDIAN);
                }
                catch (FileNotFoundException)
                {
                    throw new FileNotFoundException("Failed to open " + filename, filename);
                }
                catch (DirectoryNotFoundException)
                {
                    throw new FileNotFoundException("Failed to open " + filename, filename);
                }

                if (bi.getLength() == 0) {
                    throw new FileNotFoundException("Failed to open " + filename, filename);
                }

                string header = bi.readString32();
                if (header != "IFS") {
                    throw new InvalidDataException("File is not an IFS file");
                }
                float ifsversion  = bi.readFloat32();
                if (ifsversion != 1.0f && ifsversion != 1.1f) {
                    throw new InvalidDataException("Bad IFS version, expecting 1.0 or 1.1");
                }

                name = bi.readString32();

                texCoord.resize(0);
                while (bi.hasMore()) {
                    string str = bi.readString32();

                    if (str == "VERTICES") {
                        Debug.debugAssertM(vertex.size() == 0, "Multiple vertex fields!");
                        uint num = bi.readUInt32();

                        if ((num == 0) || (num > 10000000)) {
                            throw new InvalidDataException("Bad number of vertices");
                        }

                        vertex.resize((int)num);

                        for (uint i = 0; i < num; ++i) {
                            Vector3 v = vertex[(int)i];
                            v.deserialize(bi);
                            vertex[(int)i] = v;
                        }

                    } else if (str == "TRIANGLES") {
                        Debug.debugAssertM(index.size() == 0,
                            "Multiple triangle fields!");
                        uint num = bi.readUInt32();

                        if ((num == 0) || (num > 100000000)) {
                            throw new InvalidDataException("Bad number of triangles");
                        }

                        index.resize((int)(num * 3));
                        for (uint i = 0; i < (uint)index.size(); ++i) {
                            index[(int)i] = (int)bi.readUInt32();
                        }
                    } else if (str == "TEXTURECOORD") {
                        Debug.debugAssertM(ifsversion == 1.1f,
                            "IFS Version should be 1.1");
                        Debug.debugAssertM(texCoord.size() == 0,
                            "Multiple texcoord fields!");
                        uint num = bi.readUInt32();
                        texCoord.resize((int)num);
                        Debug.debugAssertM(texCoord.size() == vertex.size(),
                            " Must have same number of texcoords as vertices");
                        for (uint t = 0; t < num; ++t) {
                            Vector2 uv = texCoord[(int)t];
                            uv.deserialize(bi);
                            texCoord[(int)t] = uv;
                        }
                    }
                }
            } else if ("ply2" == extension) {	

                TextInput ti;
                try
                {
                    ti = new TextInput(filename);
                }
                catch (FileNotFoundException)
                {
                    throw new FileNotFoundException("Failed to open " + filename, filename);
                }
                catch (DirectoryNotFoundException)
                {
                    throw new FileNotFoundException("Failed to open " + filename, filename);
                }

                int nV = g3dmath.iFloor(ti.readNumber());
                int nF = g3dmath.iFloor(ti.readNumber());

                if ((nV < 0) || (nF < 0)) {
                    throw new InvalidDataException("ill formed PLY2 file");
                }

                vertex.resize(nV);
                index.resize(3*nF);
                texCoord.resize(0);
                name = FileUtils.filenameBaseExt(filename);

                for(int i = 0; i < nV; ++i) {
                    double x = ti.readNumber();
                    double y = ti.readNumber();
                    double z = ti.readNumber();
                    vertex[i] = new Vector3((float)x, (float)y, (float)z);
                }

                for(int i = 0; i < nF; ++i) {
                    int three = g3dmath.iFloor(ti.readNumber());
                    if (three != 3) {
                        throw new InvalidDataException("ill formed PLY2 file");
                    }
                    index[3*i	 ] = g3dmath.iFloor(ti.readNumber());
                    index[3*i + 1] = g3dmath.iFloor(ti.readNumber());
                    index[3*i + 2] = g3dmath.iFloor(ti.readNumber());
                }
				
            } else {
                throw new NotSupportedException(Format.format("unsupported filename type %s", extension));
            }
        }

        /**
           Releases the cached reference to the last model that was rendered so
           that the model can be collected.
         */
        private static void clearLastModel()
        {
            if (lastModel != null)
            {
                lastModel.Dispose();
                lastModel = null;
            }
        }

        /**
           Creates the texture coordinate array used by the indexed rendering
           path.  The managed VAR uses a three component stride for texture
           coordinates, so the two component texture coordinates are uploaded
           with a zero third component.
         */
        private static VAR createTexCoordVAR(Vector2[] values, VARArea area)
        {
            Vector3[] expanded = new Vector3[values.Length];
            for (int i = 0; i < values.Length; ++i)
            {
                expanded[i] = new Vector3(values[i].x, values[i].y, 0.0f);
            }

            VAR var = new VAR(expanded, area);
            var.setType(VAR.Type.TEXCOORD0);
            return var;
        }

        /**
           Creates the vertex array used by the indexed rendering path.
         */
        private static VAR createVertexVAR(Vector3[] values, VARArea area)
        {
            return new VAR(values, area);
        }

        /**
           Creates the normal array used by the indexed rendering path.
         */
        private static VAR createNormalVAR(Vector3[] values, VARArea area)
        {
            VAR var = new VAR(values, area);
            var.setType(VAR.Type.NORMAL);
            return var;
        }

        /**
           Creates the index array used by the indexed rendering path.
         */
        private static VAR createIndexVAR(int[] values, VARArea area)
        {
            return new VAR(values, area);
        }

        /**
           Copies the faces so that welding the adjacency information does not
           modify the unwelded copy.
         */
        private static Array<MeshAlg.Face> copyFaces(Array<MeshAlg.Face> source)
        {
            Array<MeshAlg.Face> result = new Array<MeshAlg.Face>(source.size());
            for (int i = 0; i < source.size(); ++i)
            {
                MeshAlg.Face face = new MeshAlg.Face();
                for (int j = 0; j < 3; ++j)
                {
                    face.vertexIndex[j] = source[i].vertexIndex[j];
                    face.edgeIndex[j] = source[i].edgeIndex[j];
                }
                result[i] = face;
            }
            return result;
        }

        /**
           Copies the edges so that welding the adjacency information does not
           modify the unwelded copy.
         */
        private static Array<MeshAlg.Edge> copyEdges(Array<MeshAlg.Edge> source)
        {
            Array<MeshAlg.Edge> result = new Array<MeshAlg.Edge>(source.size());
            for (int i = 0; i < source.size(); ++i)
            {
                MeshAlg.Edge edge = new MeshAlg.Edge();
                for (int j = 0; j < 2; ++j)
                {
                    edge.vertexIndex[j] = source[i].vertexIndex[j];
                    edge.faceIndex[j] = source[i].faceIndex[j];
                }
                result[i] = edge;
            }
            return result;
        }

        /**
           Copies the vertices so that welding the adjacency information does not
           modify the unwelded copy.
         */
        private static Array<MeshAlg.Vertex> copyVertices(Array<MeshAlg.Vertex> source)
        {
            Array<MeshAlg.Vertex> result = new Array<MeshAlg.Vertex>(source.size());
            for (int i = 0; i < source.size(); ++i)
            {
                MeshAlg.Vertex vertex = new MeshAlg.Vertex();
                vertex.faceIndex = new Array<int>(source[i].faceIndex);
                vertex.edgeIndex = new Array<int>(source[i].edgeIndex);
                result[i] = vertex;
            }
            return result;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (lastModel != null && lastModel.pointer() == this)
                {
                    clearLastModel();
                }
            }
            base.Dispose(disposing);
        }
    }
}

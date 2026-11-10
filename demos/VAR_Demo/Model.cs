/**
  @file VAR_Demo/Model.cs

  Port of the Model class and the file-global renderMethod state in
  VAR_Demo/main.cpp.

  See also G3D::IFSModel and G3D::MD2Model.  This demo uses neither
  in order to demonstrate the use of vertex arrays explicitly,
  but you can save yourself a lot of coding
  by using the built-in classes (which are optimized for dynamic
  var usage).
 */

using System;
using G3D;

namespace VAR_Demo
{
    public enum RenderMethod
    {
        TRIANGLES = 0,
        VARSTREAM,
        VARSTATIC,
        NUM_RENDER_METHODS
    }

    public class Model
    {
        private VAR varVertex;
        private VAR varNormal;

        private Array<uint> index = new Array<uint>();
        private Array<Vector3> vertex = new Array<Vector3>();
        private Array<Vector3> normal = new Array<Vector3>();

        public static RenderMethod renderMethod;

        public Model(string filename, VARArea varStatic)
        {
            // This loads an IFS file.  Note that we could have used G3D::IFSModel::load to
            // parse the file for us.

            // file :=
            //    fileheader +
            //    vertexheader +
            //    vertex* +
            //    triheader +
            //    tri*
            //
            //  fileheader   := (string32) "IFS" + (float32)1.0 + (string32)modelname
            //  vertexheader := (string32) "VERTICES" + (uint32)numVertices
            //  vertex       := (float32)x + (float32)y + (float32)z
            //  triheader    := (string32)"TRIANGLES" + (uint32)numFaces
            //  tri          := (uint32)v0 + (uint32)v1 + (uint32)v2

            Log.common().println("Loading " + filename);

            BinaryInput b = new BinaryInput(filename, G3DEndian.G3D_LITTLE_ENDIAN);

            string fmt = b.readString32();
            float version = b.readFloat32();
            string name = b.readString32();

            Debug.debugAssert(fmt == "IFS");
            Debug.debugAssert(version == 1.0);

            string vertexHeader = b.readString32();

            // Load the vertices
            vertex.resize((int)b.readUInt32());

            for (int v = 0; v < vertex.size(); ++v)
            {
                vertex[v] = b.readVector3() * 5;
            }

            // Per-vertex normals
            normal.resize(vertex.size());

            // Load the triangles
            string triHeader = b.readString32();

            int numTris = (int)b.readUInt32();

            for (int t = 0; t < numTris; ++t)
            {
                int v0 = (int)b.readUInt32();
                int v1 = (int)b.readUInt32();
                int v2 = (int)b.readUInt32();

                // Compute the non-unit face normal
                Vector3 faceNormal =
                  (vertex[v1] - vertex[v0]).cross(
                   (vertex[v2] - vertex[v0]));

                normal[v0] += faceNormal;
                normal[v1] += faceNormal;
                normal[v2] += faceNormal;

                // Record the indices
                index.append((uint)v0, (uint)v1, (uint)v2);
            }

            // Rescale the normals to unit length
            for (int n = 0; n < normal.size(); ++n)
            {
                normal[n] = normal[n].direction();
            }

            varVertex = new VAR(vertex.ToArray(), varStatic);
            varNormal = new VAR(normal.ToArray(), varStatic);
        }

        public int numPolys()
        {
            return index.size() / 3;
        }

        public void render(RenderDevice renderDevice, CoordinateFrame c, LightingParameters lighting, VARArea varStream)
        {
            renderDevice.setObjectToWorldMatrix(c);

            // Draw the model
            switch (renderMethod)
            {
                case RenderMethod.TRIANGLES:
                    renderDevice.beginPrimitive(RenderDevice.Primitive.TRIANGLES);
                    {
                        for (int i = 0; i < index.size(); ++i)
                        {
                            renderDevice.setNormal(normal[(int)index[i]]);
                            renderDevice.sendVertex(vertex[(int)index[i]]);
                        }
                    }
                    renderDevice.endPrimitive();
                    break;

                case RenderMethod.VARSTREAM:
                    renderDevice.beginIndexedPrimitives();

                    // This implementation would work fine for a number of *different*
                    // models.  We happen to keep uploading the same model every time
                    // because it is a test.

                    {
                        if (varStream.freeSize() < normal.size() * System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector3)) * 2)
                        {
                            // There isn't enough head room left, so reset.
                            varStream.reset();
                        }
                        VAR n = new VAR(normal.ToArray(), varStream);
                        VAR v = new VAR(vertex.ToArray(), varStream);

                        renderDevice.setNormalArray(n);
                        renderDevice.setVertexArray(v);
                        renderDevice.sendIndices(RenderDevice.Primitive.TRIANGLES, index);
                    }
                    renderDevice.endIndexedPrimitives();
                    break;

                case RenderMethod.VARSTATIC:
                    renderDevice.beginIndexedPrimitives();
                    {
                        renderDevice.setNormalArray(varNormal);
                        renderDevice.setVertexArray(varVertex);
                        renderDevice.sendIndices(RenderDevice.Primitive.TRIANGLES, index);
                    }
                    renderDevice.endIndexedPrimitives();
                    break;

                default:
                    break;
            }
        }
    }
}

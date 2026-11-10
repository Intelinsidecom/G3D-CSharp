/**
  @file ASM_Shader_Demo/Model.cs

  Port of the Model class in ASM_Shader_Demo/main.cpp.

  This demo loads an IFS file directly rather than through G3D::IFSModel,
  because it renders with immediate-mode primitives under a hand-written
  ARB vertex program.
 */

using System;
using G3D;

namespace ASM_Shader_Demo
{
    public class Model
    {
        private Array<uint> index = new Array<uint>();
        private Array<Vector3> vertex = new Array<Vector3>();
        private Array<Vector3> normal = new Array<Vector3>();

        public static App globalApp;

        public Model(string filename)
        {
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

            globalApp.debugLog.println("Loading " + filename);

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
        }

        public void render(CoordinateFrame c, LightingParameters lighting)
        {
            globalApp.renderDevice.setObjectToWorldMatrix(c);

            globalApp.renderDevice.pushState();

            // Setup lighting
            globalApp.renderDevice.enableLighting();
            globalApp.renderDevice.setLight(0, GLight.directional(lighting.lightDirection, lighting.lightColor));
            globalApp.renderDevice.setLight(1, GLight.directional(-lighting.lightDirection, Color3.white() * .25f));

            globalApp.renderDevice.setShadeMode(RenderDevice.ShadeMode.SHADE_SMOOTH);
            globalApp.renderDevice.setAmbientLightColor(lighting.ambient);
            globalApp.renderDevice.setColor(Color3.white());

            // Draw the model

            globalApp.renderDevice.beginPrimitive(RenderDevice.Primitive.TRIANGLES);
            {
                for (int i = 0; i < index.size(); ++i)
                {
                    globalApp.renderDevice.setNormal(normal[(int)index[i]]);
                    globalApp.renderDevice.sendVertex(vertex[(int)index[i]]);
                }
            }
            globalApp.renderDevice.endPrimitive();

            globalApp.renderDevice.popState();
        }
    }
}

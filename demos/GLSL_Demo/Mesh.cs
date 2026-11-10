/**
  @file GLSL_Demo/Mesh.cs

  Port of the Mesh class in GLSL_Demo/header.h and GLSL_Demo/Mesh.cpp.

  Basic geometry with texture parameterization
  and tangent space basis.
 */

using System;
using G3D;

namespace GLSL_Demo
{
    public class Mesh : ReferenceCountedObject
    {
        private static VARArea varArea;

        private VAR vertexArray;
        private VAR tangentArray;
        private VAR binormalArray;
        private VAR normalArray;
        private VAR texCoordArray;

        // For debug vertex lines
        private Array<Vector3> dbgVertexArray = new Array<Vector3>();
        private Array<Vector3> dbgBinormalArray = new Array<Vector3>();
        private Array<Vector3> dbgTangentArray = new Array<Vector3>();
        private Array<Vector3> dbgNormalArray = new Array<Vector3>();

        private Array<int> indexArray = new Array<int>();

        private Mesh(
            Array<int> index,
            Array<Vector3> vertex,
            Array<Vector3> normal,
            Array<Vector2> tex)
        {
            if (varArea == null)
            {
                varArea = VARArea.create(1024 * 1024, VARArea.UsageHint.WRITE_ONCE);
            }

            indexArray = index;

            Array<Vector3> binormal = new Array<Vector3>();
            Array<Vector3> tangent = new Array<Vector3>();
            Array<MeshAlg.Face> face = new Array<MeshAlg.Face>();
            Array<MeshAlg.Edge> tmpEdge = new Array<MeshAlg.Edge>();
            Array<MeshAlg.Vertex> tmpVertex = new Array<MeshAlg.Vertex>();
            MeshAlg.computeAdjacency(vertex, index, face, tmpEdge, tmpVertex);

            MeshAlg.computeTangentSpaceBasis(vertex, tex, normal, face, tangent, binormal);

            // For visualization purposes, store the basis
            dbgVertexArray = vertex;
            dbgTangentArray = tangent;
            dbgBinormalArray = binormal;
            dbgNormalArray = normal;

            /*
            for (int i = 0; i < binormal.length(); ++i) {
                debugPrintf("%s %s %s\n",
                    tangent[i].toString().c_str(),
                    binormal[i].toString().c_str(),
                    normal[i].toString().c_str());
            }
            */

            vertexArray = new VAR(vertex.ToArray(), varArea);
            texCoordArray = new VAR(tex.ToArray(), varArea);
            normalArray = new VAR(normal.ToArray(), varArea);
            binormalArray = new VAR(binormal.ToArray(), varArea);
            tangentArray = new VAR(tangent.ToArray(), varArea);
        }

        public static ReferenceCountedPointer<Mesh> quad()
        {
            Array<Vector3> vertex = new Array<Vector3>();
            Array<Vector3> normal = new Array<Vector3>();
            Array<Vector2> tex = new Array<Vector2>();
            Array<int> index = new Array<int>();

            double s = 2;
            vertex.append(new Vector3((float)-s, (float)-s, 0));
            vertex.append(new Vector3((float)s, (float)-s, 0));
            vertex.append(new Vector3((float)s, (float)s, 0));
            vertex.append(new Vector3((float)-s, (float)s, 0));

            normal.append(Vector3.UNIT_Z);
            normal.append(Vector3.UNIT_Z);
            normal.append(Vector3.UNIT_Z);
            normal.append(Vector3.UNIT_Z);

            tex.append(new Vector2(0, 1));
            tex.append(new Vector2(1, 1));
            tex.append(new Vector2(1, 0));
            tex.append(new Vector2(0, 0));

            index.append(0, 1, 2);
            index.append(0, 2, 3);

            return new ReferenceCountedPointer<Mesh>(new Mesh(index, vertex, normal, tex));
        }

        public void render(RenderDevice rd)
        {
            rd.beginIndexedPrimitives();
            // The mapping from properties to texture coordinates is implicit in the bump shader
            rd.setVertexArray(vertexArray);
            rd.setTexCoordArray(0, texCoordArray);
            rd.setTexCoordArray(1, tangentArray);
            rd.setTexCoordArray(2, binormalArray);
            rd.setNormalArray(normalArray);
            rd.sendIndices(RenderDevice.Primitive.TRIANGLES, indexArray);
            rd.endIndexedPrimitives();

            rd.setShader(null);
            // For debugging purposes, render the tangent space basis
            Draw.vertexVectors(dbgVertexArray, dbgTangentArray, rd, new Color4(Color3.red(), 1));
            Draw.vertexVectors(dbgVertexArray, dbgBinormalArray, rd, new Color4(Color3.green(), 1));
            Draw.vertexVectors(dbgVertexArray, dbgNormalArray, rd, new Color4(Color3.blue(), 1));
        }
    }
}

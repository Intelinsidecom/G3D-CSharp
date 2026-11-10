/**
  @file Cg_Shader_Demo/Mesh.cs

  Port of the Mesh class in Cg_Shader_Demo/Mesh.h.
 */

using System;
using G3D;

namespace Cg_Shader_Demo
{
    public class Mesh
    {
        public Array<Vector3> vertexArray = new Array<Vector3>();
        public Array<Vector2> texCoordArray = new Array<Vector2>();

        public Array<Vector3> normalArray = new Array<Vector3>();
        public Array<Vector3> tangentArray = new Array<Vector3>();
        public Array<Vector3> binormalArray = new Array<Vector3>();

        public Array<int> indexArray = new Array<int>();
        public Array<MeshAlg.Face> faceArray = new Array<MeshAlg.Face>();

        public Array<MeshAlg.Edge> edgeArray = new Array<MeshAlg.Edge>();

        public Array<Array<int>> adjacentFaceArray = new Array<Array<int>>();
        public Array<Vector3> faceNormalArray = new Array<Vector3>();

        public Mesh()
        {
            vertexArray.resize(4);
            texCoordArray.resize(4);

            vertexArray[0] = new Vector3(-2, 1, -2);
            texCoordArray[0] = new Vector2(0, 0);

            vertexArray[1] = new Vector3(-2, 1, 2);
            texCoordArray[1] = new Vector2(0, 1);

            vertexArray[2] = new Vector3(2, 1, 2);
            texCoordArray[2] = new Vector2(1, 1);

            vertexArray[3] = new Vector3(2, 1, -2);
            texCoordArray[3] = new Vector2(1, 0);

            indexArray.append(0, 1, 2);
            indexArray.append(0, 2, 3);

            MeshAlg.computeAdjacency(vertexArray, indexArray,
                faceArray, edgeArray, adjacentFaceArray);

            MeshAlg.computeNormals(vertexArray, faceArray,
                adjacentFaceArray, normalArray, faceNormalArray);

            MeshAlg.computeTangentSpaceBasis(vertexArray, texCoordArray,
                normalArray, faceArray, tangentArray, binormalArray);
        }

        public void render(RenderDevice rd)
        {
            rd.beginPrimitive(RenderDevice.Primitive.TRIANGLES);
            for (int i = 0; i < indexArray.size(); ++i)
            {
                int v = indexArray[i];
                rd.setTexCoord(0, texCoordArray[v]);
                rd.setNormal(normalArray[v]);
                rd.setTexCoord(1, tangentArray[v]);
                rd.setTexCoord(2, binormalArray[v]);
                rd.sendVertex(vertexArray[v]);
            }
            rd.endPrimitive();

            /*
            for (int v = 0; v < vertexArray.size(); ++v) {
                CoordinateFrame cframe(vertexArray[v]);
                cframe.rotation.setColumn(0, tangentArray[v]);
                cframe.rotation.setColumn(1, normalArray[v]);
                cframe.rotation.setColumn(2, binormalArray[v]);
                Draw::axes(cframe, rd, Color3::RED, Color3::GREEN, Color3::BLUE, 0.5);
            }
            */
        }
    }
}

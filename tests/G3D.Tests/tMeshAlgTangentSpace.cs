using System;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private static void testMeshAlgTangentSpace()
        {
            printf("MeshAlg::computeTangentSpaceBasis ");

            MeshAlg.Geometry geometry = new MeshAlg.Geometry();
            Array<MeshAlg.Face> face = new Array<MeshAlg.Face>();
            Array<MeshAlg.Edge> edge = new Array<MeshAlg.Edge>();
            Array<MeshAlg.Vertex> vertex = new Array<MeshAlg.Vertex>();

	        Array<Vector2> texCoord = new Array<Vector2>();
            Array<Vector3> tangent = new Array<Vector3>();
            Array<Vector3> binormal = new Array<Vector3>();
	        Array<int>	   index = new Array<int>();

            // Create a quad
	        float s = 2;
	        geometry.vertexArray.append(new Vector3(-s, -s, 0));
	        geometry.vertexArray.append(new Vector3( s, -s, 0));
	        geometry.vertexArray.append(new Vector3( s,  s, 0));
	        geometry.vertexArray.append(new Vector3(-s,  s, 0));

            texCoord.append(new Vector2(0, 1));
	        texCoord.append(new Vector2(1, 1));
	        texCoord.append(new Vector2(1, 0));
	        texCoord.append(new Vector2(0, 0));

	        index.append(0, 1, 2);
	        index.append(0, 2, 3);

            MeshAlg.computeAdjacency(geometry.vertexArray, index, face, edge, vertex);
            MeshAlg.computeNormals(geometry, index);

            for (int i = 0; i < 4; ++i) {
                Debug.debugAssert(geometry.normalArray[i].fuzzyEq(Vector3.unitZ()));
            }

            MeshAlg.computeTangentSpaceBasis(geometry.vertexArray, texCoord, geometry.normalArray, face, tangent, binormal);

            for (int i = 0; i < 4; ++i) {
                Debug.debugAssert(tangent[i].fuzzyEq(Vector3.unitX()));
                Debug.debugAssert(binormal[i].fuzzyEq(Vector3.unitY()));
            }

            printf("passed\n");
        }
    }
}

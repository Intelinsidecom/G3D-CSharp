using System;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private sealed class MeshAlgProbe : MeshAlg
        {
            public static int callFindEdgeIndex(
                Array<Vector3> vertices,
                Array<MeshAlg.Edge> edges,
                int i0, int i1, int face, double area)
            {
                return findEdgeIndex(vertices, edges, i0, i1, face, area);
            }
        }

        private static void testMeshAlgWeld()
        {
            printf("MeshAlg.computeWeld ");

            Array<Vector3> oldVertices = new Array<Vector3>();
            oldVertices.append(new Vector3(0, 0, 0));
            oldVertices.append(new Vector3(0.001f, 0, 0));
            oldVertices.append(new Vector3(1, 0, 0));
            Array<Vector3> weldedVertices = new Array<Vector3>();
            Array<int> toNew = new Array<int>();
            Array<int> toOld = new Array<int>();
            MeshAlg.computeWeld(oldVertices, weldedVertices, toNew, toOld, 0.01);
            CheckEqual(2, weldedVertices.size(), "MeshAlg.computeWeld vertex count");
            CheckEqual(3, toNew.size(), "MeshAlg.computeWeld new map count");
            CheckEqual(2, toOld.size(), "MeshAlg.computeWeld old map count");
            Check(toNew[0] == toNew[1], "MeshAlg.computeWeld nearby vertices");
            Check(toNew[0] != toNew[2], "MeshAlg.computeWeld separated vertices");
            Check(toOld[toNew[0]] == 1, "MeshAlg.computeWeld old map");
            Check(weldedVertices[toNew[0]] == oldVertices[0], "MeshAlg.computeWeld preserves old position");

            Array<Vector3> exactVertices = new Array<Vector3>();
            exactVertices.append(new Vector3(0, 0, 0));
            exactVertices.append(new Vector3(0, 0, 0));
            exactVertices.append(new Vector3(1, 0, 0));
            Array<Vector3> exactWelded = new Array<Vector3>();
            Array<int> exactToNew = new Array<int>();
            Array<int> exactToOld = new Array<int>();
            MeshAlg.computeWeld(exactVertices, exactWelded, exactToNew, exactToOld, 0.0);
            CheckEqual(2, exactWelded.size(), "MeshAlg.computeWeld exact vertex count");
            Check(exactToNew[0] == exactToNew[1], "MeshAlg.computeWeld exact duplicate vertices");
            Check(exactToNew[0] != exactToNew[2], "MeshAlg.computeWeld exact separated vertices");

            Array<Vector3> boundaryVertices = new Array<Vector3>();
            boundaryVertices.append(new Vector3(0, 0, 0));
            boundaryVertices.append(new Vector3(0.01f, 0, 0));
            boundaryVertices.append(new Vector3(1, 0, 0));
            Array<Vector3> boundaryWelded = new Array<Vector3>();
            Array<int> boundaryToNew = new Array<int>();
            Array<int> boundaryToOld = new Array<int>();
            MeshAlg.computeWeld(boundaryVertices, boundaryWelded, boundaryToNew, boundaryToOld, 0.01);
            CheckEqual(2, boundaryWelded.size(), "MeshAlg.computeWeld boundary vertex count");
            Check(boundaryToNew[0] == boundaryToNew[1], "MeshAlg.computeWeld boundary radius");

            Array<Vector3> emptyVertices = new Array<Vector3>();
            Array<Vector3> emptyWelded = new Array<Vector3>();
            Array<int> emptyToNew = new Array<int>();
            Array<int> emptyToOld = new Array<int>();
            MeshAlg.computeWeld(emptyVertices, emptyWelded, emptyToNew, emptyToOld, 0.01);
            CheckEqual(0, emptyWelded.size(), "MeshAlg.computeWeld empty vertex count");
            CheckEqual(0, emptyToNew.size(), "MeshAlg.computeWeld empty new map count");
            CheckEqual(0, emptyToOld.size(), "MeshAlg.computeWeld empty old map count");

            printf("passed\n");
        }

        private static void testMeshAlg()
        {
            printf("MeshAlg ");

            Array<Vector3> vertices = new Array<Vector3>();
            vertices.append(new Vector3(0, 0, 0));
            vertices.append(new Vector3(1, 0, 0));
            vertices.append(new Vector3(1, 1, 0));
            vertices.append(new Vector3(0, 1, 0));
            Array<int> indices = new Array<int>();
            indices.append(0, 1, 2);
            indices.append(0, 2, 3);

            double minEdgeLength, meanEdgeLength, medianEdgeLength, maxEdgeLength;
            double minFaceArea, meanFaceArea, medianFaceArea, maxFaceArea;
            MeshAlg.computeAreaStatistics(vertices, indices,
                out minEdgeLength, out meanEdgeLength, out medianEdgeLength, out maxEdgeLength,
                out minFaceArea, out meanFaceArea, out medianFaceArea, out maxFaceArea);
            CheckEqual(1.0, minEdgeLength, "MeshAlg.computeAreaStatistics min edge");
            CheckEqual((4.0 + 2.0 * Math.Sqrt(2.0)) / 6.0, meanEdgeLength, "MeshAlg.computeAreaStatistics mean edge");
            CheckEqual(1.0, medianEdgeLength, "MeshAlg.computeAreaStatistics median edge");
            CheckEqual(Math.Sqrt(2.0), maxEdgeLength, "MeshAlg.computeAreaStatistics max edge");
            CheckEqual(0.5, minFaceArea, "MeshAlg.computeAreaStatistics min area");
            CheckEqual(0.5, meanFaceArea, "MeshAlg.computeAreaStatistics mean area");
            CheckEqual(0.5, medianFaceArea, "MeshAlg.computeAreaStatistics median area");
            CheckEqual(0.5, maxFaceArea, "MeshAlg.computeAreaStatistics max area");

            MeshAlg.Geometry geometry = new MeshAlg.Geometry();
            for (int i = 0; i < vertices.size(); ++i)
            {
                geometry.vertexArray.append(vertices[i]);
            }
            Array<MeshAlg.Face> faces = new Array<MeshAlg.Face>();
            Array<MeshAlg.Edge> edges = new Array<MeshAlg.Edge>();
            Array<MeshAlg.Vertex> meshVertices = new Array<MeshAlg.Vertex>();
            MeshAlg.computeAdjacency(geometry.vertexArray, indices, faces, edges, meshVertices);
            CheckEqual(2, faces.size(), "MeshAlg.computeAdjacency face count");
            CheckEqual(5, edges.size(), "MeshAlg.computeAdjacency edge count");
            CheckEqual(4, meshVertices.size(), "MeshAlg.computeAdjacency vertex count");

            Array<Vector3> faceNormals = new Array<Vector3>();
            MeshAlg.computeFaceNormals(geometry.vertexArray, faces, faceNormals);
            CheckEqual(2, faceNormals.size(), "MeshAlg.computeFaceNormals count");
            Check(faceNormals[0].fuzzyEq(Vector3.unitZ()), "MeshAlg.computeFaceNormals normal");
            Check(faceNormals[1].fuzzyEq(Vector3.unitZ()), "MeshAlg.computeFaceNormals normal 2");

            MeshAlg.computeNormals(geometry, indices);
            CheckEqual(4, geometry.normalArray.size(), "MeshAlg.computeNormals normal count");
            Check(geometry.normalArray[0].fuzzyEq(Vector3.unitZ()), "MeshAlg.computeNormals vertex normal");

            Array<bool> backfaces = new Array<bool>();
            MeshAlg.identifyBackfaces(geometry.vertexArray, faces, new Vector4(0, 0, 1, 0), backfaces);
            Check(!backfaces[0] && !backfaces[1], "MeshAlg.identifyBackfaces infinite");
            MeshAlg.identifyBackfaces(geometry.vertexArray, faces, new Vector4(0, 0, -1, 1), backfaces);
            Check(backfaces[0] && backfaces[1], "MeshAlg.identifyBackfaces finite");
            MeshAlg.identifyBackfaces(geometry.vertexArray, faces, new Vector4(0, 0, -1, 0), backfaces, faceNormals);
            Check(backfaces[0] && backfaces[1], "MeshAlg.identifyBackfaces precomputed normals");

            Array<int> created = new Array<int>();
            MeshAlg.createIndexArray(10, created);
            CheckArray(created, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            MeshAlg.createIndexArray(5, created, 2);
            CheckArray(created, 2, 3, 4, 5, 6);
            MeshAlg.createIndexArray(6, created, 0, 2, 1);
            CheckArray(created, 0, 1, 3, 4, 6, 7);

            Array<int> strip = new Array<int>();
            strip.append(0, 1, 2, 3);
            Array<int> triangleList = new Array<int>();
            MeshAlg.toIndexedTriList(strip, MeshAlg.Primitive.TRIANGLE_STRIP, triangleList);
            CheckArray(triangleList, 0, 1, 2, 2, 1, 3);

            Array<int> quads = new Array<int>();
            quads.append(0, 1, 2, 3);
            MeshAlg.toIndexedTriList(quads, MeshAlg.Primitive.QUADS, triangleList);
            CheckArray(triangleList, 0, 1, 3, 1, 2, 3);

            Array<int> quadStrip = new Array<int>();
            quadStrip.append(0, 1, 2, 3);
            MeshAlg.toIndexedTriList(quadStrip, MeshAlg.Primitive.QUAD_STRIP, triangleList);
            CheckArray(triangleList, 0, 1, 2, 2, 1, 3);

            Array<int> longStrip = new Array<int>();
            longStrip.append(0, 1, 2, 3);
            longStrip.append(4);
            MeshAlg.toIndexedTriList(longStrip, MeshAlg.Primitive.TRIANGLE_STRIP, triangleList);
            CheckArray(triangleList, 0, 1, 2, 2, 1, 3, 2, 3, 4);

            Array<int> longQuadStrip = new Array<int>();
            longQuadStrip.append(0, 1, 2, 3);
            longQuadStrip.append(4, 5);
            MeshAlg.toIndexedTriList(longQuadStrip, MeshAlg.Primitive.QUAD_STRIP, triangleList);
            CheckArray(triangleList, 0, 1, 2, 2, 1, 3, 2, 3, 4, 4, 3, 5);

            bool invalidStripRejected = false;
            try
            {
                Array<int> invalidStrip = new Array<int>();
                invalidStrip.append(0, 1);
                MeshAlg.toIndexedTriList(invalidStrip, MeshAlg.Primitive.TRIANGLE_STRIP, triangleList);
            }
            catch (ArgumentException)
            {
                invalidStripRejected = true;
            }
            Check(invalidStripRejected, "MeshAlg.toIndexedTriList rejects short triangle strip");

            bool invalidQuadStripRejected = false;
            try
            {
                Array<int> invalidQuadStrip = new Array<int>();
                invalidQuadStrip.append(0, 1, 2, 3);
                invalidQuadStrip.append(4);
                MeshAlg.toIndexedTriList(invalidQuadStrip, MeshAlg.Primitive.QUAD_STRIP, triangleList);
            }
            catch (ArgumentException)
            {
                invalidQuadStripRejected = true;
            }
            Check(invalidQuadStripRejected, "MeshAlg.toIndexedTriList rejects odd quad strip");

            bool invalidPrimitiveRejected = false;
            try
            {
                Array<int> invalidPrimitive = new Array<int>();
                invalidPrimitive.append(0, 1, 2);
                MeshAlg.toIndexedTriList(invalidPrimitive, MeshAlg.Primitive.TRIANGLES, triangleList);
            }
            catch (ArgumentException)
            {
                invalidPrimitiveRejected = true;
            }
            Check(invalidPrimitiveRejected, "MeshAlg.toIndexedTriList rejects unsupported primitive");

            Box bounds;
            Sphere sphere;
            MeshAlg.computeBounds(vertices, out bounds, out sphere);
            Check(bounds.contains(new Vector3(0, 0, 0)), "MeshAlg.computeBounds box minimum");
            Check(bounds.contains(new Vector3(1, 1, 0)), "MeshAlg.computeBounds box maximum");
            Check((new Vector3(0, 0, 0) - sphere.center).magnitude() <= sphere.radius + 0.00001f, "MeshAlg.computeBounds sphere minimum");
            Check((new Vector3(1, 1, 0) - sphere.center).magnitude() <= sphere.radius + 0.00001f, "MeshAlg.computeBounds sphere maximum");

            Array<int> subset = new Array<int>();
            subset.append(0, 1);
            MeshAlg.computeBounds(vertices, subset, out bounds, out sphere);
            Check(bounds.center().fuzzyEq(new Vector3(0.5f, 0, 0)), "MeshAlg.computeBounds subset center");
            CheckEqual(1.0, bounds.extent(0), "MeshAlg.computeBounds subset x extent");
            CheckEqual(0.0, bounds.extent(1), "MeshAlg.computeBounds subset y extent");
            CheckEqual(0.0, bounds.extent(2), "MeshAlg.computeBounds subset z extent");
            CheckEqual(4, MeshAlg.countBoundaryEdges(edges), "MeshAlg.countBoundaryEdges");
            CheckEqual(4, MeshAlg.countBrokenEdges(edges), "MeshAlg.countBrokenEdges");

            Array<Vector3> referenceVertices = new Array<Vector3>();
            referenceVertices.append(new Vector3(-3, -4, -5));
            referenceVertices.append(new Vector3(7, 2, 1));
            referenceVertices.append(new Vector3(1, 8, -9));
            MeshAlg.computeBounds(referenceVertices, out bounds, out sphere);
            CheckEqual(2.0, sphere.center.x, "MeshAlg.computeBounds reference sphere x");
            CheckEqual(2.0, sphere.center.y, "MeshAlg.computeBounds reference sphere y");
            CheckEqual(-4.0, sphere.center.z, "MeshAlg.computeBounds reference sphere z");
            CheckEqual(9.2736187, sphere.radius, "MeshAlg.computeBounds reference sphere radius");

            Array<Array<int>> adjacentFaces = new Array<Array<int>>();
            Array<MeshAlg.Face> deprecatedFaces = new Array<MeshAlg.Face>();
            Array<MeshAlg.Edge> deprecatedEdges = new Array<MeshAlg.Edge>();
            MeshAlg.computeAdjacency(geometry.vertexArray, indices, deprecatedFaces, deprecatedEdges, adjacentFaces);
            Check(adjacentFaces[0].contains(0), "MeshAlg.computeAdjacency deprecated faces");
            Check(adjacentFaces[0].contains(1), "MeshAlg.computeAdjacency deprecated faces 2");

            Array<Vector3> deprecatedVertexNormals = new Array<Vector3>();
            Array<Vector3> deprecatedFaceNormals = new Array<Vector3>();
            MeshAlg.computeNormals(geometry.vertexArray, deprecatedFaces, adjacentFaces, deprecatedVertexNormals, deprecatedFaceNormals);
            CheckEqual(4, deprecatedVertexNormals.size(), "MeshAlg.computeNormals deprecated vertex normal count");
            CheckEqual(2, deprecatedFaceNormals.size(), "MeshAlg.computeNormals deprecated face normal count");
            Check(deprecatedVertexNormals[0].fuzzyEq(Vector3.unitZ()), "MeshAlg.computeNormals deprecated vertex normal");

            Check(faces[0].containsVertex(0), "MeshAlg.Face.containsVertex");
            Check(faces[0].containsDirectedEdge(faces[0].edgeIndex[0]), "MeshAlg.Face.containsDirectedEdge");
            Check(meshVertices[0].inFace(0), "MeshAlg.Vertex.inFace");
            Check(meshVertices[0].inEdge(0), "MeshAlg.Vertex.inEdge");

            MeshAlg.Edge edge = new MeshAlg.Edge();
            edge.vertexIndex[0] = 2;
            edge.vertexIndex[1] = 5;
            edge.faceIndex[0] = 3;
            edge.faceIndex[1] = 4;
            MeshAlg.Edge reverse = edge.reverse();
            Check(reverse.vertexIndex[0] == 5 && reverse.vertexIndex[1] == 2, "MeshAlg.Edge.reverse vertices");
            Check(reverse.faceIndex[0] == 4 && reverse.faceIndex[1] == 3, "MeshAlg.Edge.reverse faces");
            Check(edge.containsVertex(2) && edge.containsVertex(5), "MeshAlg.Edge.containsVertex");
            Check(edge.inFace(3) && edge.inFace(4), "MeshAlg.Edge.inFace");
            Check(!edge.broken(), "MeshAlg.Edge.broken");
            edge.faceIndex[1] = MeshAlg.Face.NONE;
            Check(edge.broken(), "MeshAlg.Edge.broken boundary");

            MeshAlg.Geometry source = new MeshAlg.Geometry();
            source.vertexArray.append(new Vector3(1, 2, 3));
            source.normalArray.append(new Vector3(0, 0, 1));
            source.normalArray.append(new Vector3(1, 0, 0));
            MeshAlg.Geometry copied = new MeshAlg.Geometry();
            copied.copyFrom(source);
            CheckEqual(1, copied.vertexArray.size(), "MeshAlg.Geometry.copyFrom vertex count");
            CheckEqual(2, copied.normalArray.size(), "MeshAlg.Geometry.copyFrom normal count");
            Check(copied.normalArray[1].fuzzyEq(Vector3.unitX()), "MeshAlg.Geometry.copyFrom normal value");
            copied.clear();
            CheckEqual(0, copied.vertexArray.size(), "MeshAlg.Geometry.clear vertex count");
            CheckEqual(0, copied.normalArray.size(), "MeshAlg.Geometry.clear normal count");

            bool undefinedHelperRejected = false;
            try
            {
                MeshAlgProbe.callFindEdgeIndex(vertices, edges, 0, 1, 0, 1.0);
            }
            catch (NotSupportedException)
            {
                undefinedHelperRejected = true;
            }
            Check(undefinedHelperRejected, "MeshAlg.findEdgeIndex reports undefined original helper");

            MeshAlg.debugCheckConsistency(faces, edges, meshVertices);
            printf("passed\n");
        }
    }
}
